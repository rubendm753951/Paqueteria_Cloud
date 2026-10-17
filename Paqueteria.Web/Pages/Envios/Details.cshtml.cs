using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;
using Paqueteria.Web.Services;

namespace Paqueteria.Web.Pages.Envios;

public sealed class DetailsModel(PaqueteriaDbContext context, IEmailNotificationService emailNotificationService) : PageModel
{
    public Guid Id { get; private set; }
    public ShipmentHeader? Shipment { get; private set; }
    public IReadOnlyList<TrackingRow> Timeline { get; private set; } = [];
    public IReadOnlyList<AttemptRow> Attempts { get; private set; } = [];
    public ProofRow? Proof { get; private set; }
    public IReadOnlyList<DriverOption> Drivers { get; private set; } = [];

    [BindProperty] public AttemptInput Attempt { get; set; } = new();
    [BindProperty] public PodInput Pod { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await LoadAsync(userId, id, canEdit: false)) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAttemptAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await HasPagePermissionAsync(userId, canEdit: true)) return Forbid();
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(Attempt.ResultCode))
            ModelState.AddModelError("Attempt.ResultCode", "Selecciona el motivo del intento.");
        if (Attempt.NextAction is not ("reintentar" or "retener" or "devolver"))
            ModelState.AddModelError("Attempt.NextAction", "La próxima acción no es válida.");
        var agencyIds = await GetAgencyIdsAsync(userId, canEdit: true);
        var order = await context.ShipmentOrders.SingleOrDefaultAsync(o => o.Id == id && agencyIds.Contains(o.IdAgencia));
        if (order is null) return NotFound();
        if (order.EstadoOrden is "entregada" or "cancelada" or "devuelta")
        {
            ModelState.AddModelError(string.Empty, "No se puede registrar un intento para un envío cerrado.");
        }
        if (!ModelState.IsValid)
        {
            await LoadAsync(userId, id, canEdit: true);
            return Page();
        }

        var driverId = Attempt.DriverId ?? order.AssignedDriverId;
        if (driverId.HasValue && !await context.DeliveryDrivers.AnyAsync(d => d.Id == driverId && d.AgencyId == order.IdAgencia && d.Status == "activo"))
        {
            ModelState.AddModelError("Attempt.DriverId", "El conductor seleccionado no pertenece a la agencia.");
            await LoadAsync(userId, id, canEdit: true);
            return Page();
        }

        var attemptNumber = await context.DeliveryAttempts.CountAsync(a => a.AgencyId == order.IdAgencia && a.ShipmentOrderId == id) + 1;
        var newStatus = Attempt.NextAction switch
        {
            "retener" => "en_retencion",
            "devolver" => "devuelta",
            _ => "intento_fallido"
        };
        await using var transaction = await context.Database.BeginTransactionAsync();
        var previous = order.EstadoOrden;
        context.DeliveryAttempts.Add(new DeliveryAttempt
        {
            AgencyId = order.IdAgencia, ShipmentOrderId = id, DriverId = driverId,
            AttemptNumber = attemptNumber, AttemptedAt = DateTimeOffset.UtcNow,
            ResultCode = Attempt.ResultCode, Notes = Attempt.Notes?.Trim(), NextAction = Attempt.NextAction
        });
        order.EstadoOrden = newStatus;
        context.ShipmentTrackingEvents.Add(CreateEvent(order, previous, newStatus, "DELIVERY_ATTEMPTED", Attempt.Notes, userId));
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        await emailNotificationService.SendTrackingUpdateAsync(order.TrackingNumber, "Intento de entrega", "Se registró una actualización en el proceso de entrega.");
        StatusMessage = "Intento de entrega registrado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, long? driverId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await HasPagePermissionAsync(userId, canEdit: true)) return Forbid();
        var agencyIds = await GetAgencyIdsAsync(userId, canEdit: true);
        var order = await context.ShipmentOrders.SingleOrDefaultAsync(o => o.Id == id && agencyIds.Contains(o.IdAgencia));
        if (order is null) return NotFound();
        if (order.EstadoOrden is "entregada" or "cancelada" or "devuelta") return BadRequest("El envío ya está cerrado.");
        DeliveryDriver? driver = null;
        if (driverId.HasValue)
        {
            driver = await context.DeliveryDrivers.SingleOrDefaultAsync(d => d.Id == driverId.Value && d.AgencyId == order.IdAgencia && d.Status == "activo");
            if (driver is null) return BadRequest("El conductor no pertenece a la agencia.");
        }
        var previous = order.EstadoOrden;
        order.AssignedDriverId = driver?.Id;
        order.EstadoOrden = driver is null ? "confirmada" : "asignada";
        context.ShipmentTrackingEvents.Add(CreateEvent(order, previous, order.EstadoOrden,
            driver is null ? "DRIVER_UNASSIGNED" : "DRIVER_ASSIGNED",
            driver is null ? "Conductor desasignado" : $"Conductor asignado: {driver.FirstName} {driver.LastName}", userId));
        await emailNotificationService.SendTrackingUpdateAsync(order.TrackingNumber,
            driver is null ? "En preparación" : "Preparando entrega",
            driver is null ? "El envío volvió a estar pendiente de asignación." : "El envío fue asignado para su entrega.");
        await context.SaveChangesAsync();
        StatusMessage = "Asignación actualizada.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostStartDeliveryAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await HasPagePermissionAsync(userId, canEdit: true)) return Forbid();

        var agencyIds = await GetAgencyIdsAsync(userId, canEdit: true);
        var order = await context.ShipmentOrders.SingleOrDefaultAsync(o =>
            o.Id == id && agencyIds.Contains(o.IdAgencia));
        if (order is null) return NotFound();
        if (!order.AssignedDriverId.HasValue)
        {
            TempData["StatusMessage"] = "Asigna un conductor antes de iniciar la entrega.";
            return RedirectToPage(new { id });
        }
        if (order.EstadoOrden is not ("asignada" or "confirmada"))
        {
            TempData["StatusMessage"] = "El envío no está disponible para iniciar la entrega.";
            return RedirectToPage(new { id });
        }

        var previous = order.EstadoOrden;
        order.EstadoOrden = "en_transito";
        await emailNotificationService.SendTrackingUpdateAsync(order.TrackingNumber, "En tránsito", "El envío salió a ruta de entrega.");
        context.ShipmentTrackingEvents.Add(CreateEvent(
            order, previous, "en_transito", "OUT_FOR_DELIVERY",
            "El conductor inició la ruta de entrega", userId));
        await context.SaveChangesAsync();
        StatusMessage = "Entrega iniciada. El envío está en ruta.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeliverAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await HasPagePermissionAsync(userId, canEdit: true)) return Forbid();
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(Pod.ReceivedByName))
            ModelState.AddModelError("Pod.ReceivedByName", "Indica quién recibió el envío.");
        var agencyIds = await GetAgencyIdsAsync(userId, canEdit: true);
        var order = await context.ShipmentOrders.SingleOrDefaultAsync(o => o.Id == id && agencyIds.Contains(o.IdAgencia));
        if (order is null) return NotFound();
        if (order.EstadoOrden is "entregada" or "cancelada" or "devuelta") ModelState.AddModelError(string.Empty, "El envío ya está cerrado.");
        if (!ModelState.IsValid)
        {
            await LoadAsync(userId, id, canEdit: true);
            return Page();
        }

        var driverId = Pod.DriverId ?? order.AssignedDriverId;
        if (driverId.HasValue && !await context.DeliveryDrivers.AnyAsync(d => d.Id == driverId && d.AgencyId == order.IdAgencia && d.Status == "activo"))
        {
            ModelState.AddModelError("Pod.DriverId", "El conductor seleccionado no pertenece a la agencia.");
            await LoadAsync(userId, id, canEdit: true);
            return Page();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var previous = order.EstadoOrden;
        context.ProofsOfDelivery.Add(new ProofOfDelivery
        {
            AgencyId = order.IdAgencia, ShipmentOrderId = id, DriverId = driverId,
            ReceivedByName = Pod.ReceivedByName.Trim(), Relationship = Pod.Relationship?.Trim(),
            DeliveryNotes = Pod.DeliveryNotes?.Trim(), DeliveredAt = DateTimeOffset.UtcNow
        });
        order.EstadoOrden = "entregada";
        await emailNotificationService.SendTrackingUpdateAsync(order.TrackingNumber, "Entregado", "El envío fue entregado correctamente.");
        order.DeliveredAt = DateTimeOffset.UtcNow;
        context.ShipmentTrackingEvents.Add(CreateEvent(order, previous, "entregada", "DELIVERED", Pod.DeliveryNotes, userId));
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        StatusMessage = "Entrega confirmada y POD registrado.";
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(string userId, Guid id, bool canEdit)
    {
        var agencyIds = await GetAgencyIdsAsync(userId, canEdit);
        if (!await HasPagePermissionAsync(userId, canEdit: false)) return false;
        var order = await context.ShipmentOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && agencyIds.Contains(o.IdAgencia));
        if (order is null) return false;
        Id = id;
        var agencyName = await context.CAgencias.AsNoTracking().Where(a => a.IdAgencia == order.IdAgencia).Select(a => a.Nombre).SingleOrDefaultAsync();
        var driverName = order.AssignedDriverId.HasValue ? await context.DeliveryDrivers.AsNoTracking().Where(d => d.Id == order.AssignedDriverId).Select(d => d.FirstName + " " + d.LastName).SingleOrDefaultAsync() : null;
        Shipment = new ShipmentHeader(order.Id, order.TrackingNumber, agencyName ?? $"Agencia {order.IdAgencia}", order.IdAgencia, order.EstadoOrden, order.FechaCreacion, driverName);
        Timeline = await context.ShipmentTrackingEvents.AsNoTracking().Where(e => e.AgencyId == order.IdAgencia && e.ShipmentOrderId == id).OrderByDescending(e => e.EventAt).Select(e => new TrackingRow(e.EventCode, e.PreviousStatus, e.NewStatus, e.EventAt, e.Description)).ToListAsync();
        Attempts = await context.DeliveryAttempts.AsNoTracking().Where(a => a.AgencyId == order.IdAgencia && a.ShipmentOrderId == id).OrderByDescending(a => a.AttemptedAt).Select(a => new AttemptRow(a.AttemptNumber, a.ResultCode, a.NextAction, a.AttemptedAt, a.Notes)).ToListAsync();
        Proof = await context.ProofsOfDelivery.AsNoTracking().Where(p => p.AgencyId == order.IdAgencia && p.ShipmentOrderId == id).Select(p => new ProofRow(p.ReceivedByName, p.Relationship, p.DeliveredAt, p.DeliveryNotes)).SingleOrDefaultAsync();
        Drivers = await context.DeliveryDrivers.AsNoTracking().Where(d => d.AgencyId == order.IdAgencia && d.Status == "activo").OrderBy(d => d.FirstName).Select(d => new DriverOption(d.Id, d.FirstName + " " + d.LastName)).ToListAsync();
        return true;
    }

    private ShipmentTrackingEvent CreateEvent(ShipmentOrder order, string previous, string next, string code, string? description, string userId) => new()
    {
        AgencyId = order.IdAgencia, ShipmentOrderId = order.Id, EventCode = code,
        PreviousStatus = previous, NewStatus = next, EventAt = DateTimeOffset.UtcNow,
        Description = description?.Trim(), CreatedByUserId = userId
    };

    private Task<List<int>> GetAgencyIdsAsync(string userId, bool canEdit) => context.UserAgencyPermissions.AsNoTracking()
        .Where(p => p.UserId == userId && p.CanView && (!canEdit || p.CanEdit)).Select(p => p.AgencyId).ToListAsync();

    private Task<bool> HasPagePermissionAsync(string userId, bool canEdit) => context.UserPagePermissions.AnyAsync(p => p.UserId == userId && p.PageKey == "Envios" && p.CanView && (!canEdit || p.CanEdit));

    public sealed record ShipmentHeader(Guid Id, string Tracking, string AgencyName, int AgencyId, string Status, DateTimeOffset CreatedAt, string? DriverName);
    public sealed record TrackingRow(string EventCode, string? PreviousStatus, string NewStatus, DateTimeOffset EventAt, string? Description);
    public sealed record AttemptRow(int Number, string ResultCode, string NextAction, DateTimeOffset AttemptedAt, string? Notes);
    public sealed record ProofRow(string ReceivedBy, string? Relationship, DateTimeOffset DeliveredAt, string? Notes);
    public sealed record DriverOption(long Id, string Name);

    public sealed class AttemptInput
    {
        [Required] public string ResultCode { get; set; } = "cliente_ausente";
        public long? DriverId { get; set; }
        [StringLength(1000)] public string? Notes { get; set; }
        [Required] public string NextAction { get; set; } = "reintentar";
    }

    public sealed class PodInput
    {
        [Required, StringLength(160)] public string ReceivedByName { get; set; } = string.Empty;
        public long? DriverId { get; set; }
        [StringLength(80)] public string? Relationship { get; set; }
        [StringLength(1000)] public string? DeliveryNotes { get; set; }
    }
}
