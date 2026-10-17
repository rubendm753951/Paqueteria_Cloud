using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;
using Paqueteria.Web.Services;

namespace Paqueteria.Web.Pages.Envios;

public sealed class IndexModel(PaqueteriaDbContext context, IEmailNotificationService emailNotificationService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Tracking { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int? AgencyId { get; set; }
    [BindProperty(SupportsGet = true)] public long? DriverId { get; set; }

    [BindProperty] public Guid ShipmentId { get; set; }
    [BindProperty] public long? SelectedDriverId { get; set; }

    public IReadOnlyList<ShipmentRow> Shipments { get; private set; } = [];
    public IReadOnlyList<AgencyOption> Agencies { get; private set; } = [];
    public IReadOnlyList<DriverOption> Drivers { get; private set; } = [];
    public int Total { get; private set; }
    public int InTransit { get; private set; }
    public int Delivered { get; private set; }
    public int Incidents { get; private set; }

    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        var agencyIds = await GetAuthorizedAgencyIdsAsync(userId, canEdit: false);
        if (!await HasPagePermissionAsync(userId, canEdit: false) || agencyIds.Count == 0) return Forbid();

        await LoadAsync(userId, agencyIds);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        var agencyIds = await GetAuthorizedAgencyIdsAsync(userId, canEdit: true);
        if (!await HasPagePermissionAsync(userId, canEdit: true)) return Forbid();

        var order = await context.ShipmentOrders.SingleOrDefaultAsync(item =>
            item.Id == ShipmentId && agencyIds.Contains(item.IdAgencia));
        if (order is null) return NotFound();
        if (order.EstadoOrden is "entregada" or "cancelada" or "devuelta")
        {
            TempData["StatusMessage"] = "El envío ya no puede ser asignado.";
            return RedirectToPage(new { Tracking, Status, AgencyId, DriverId });
        }

        DeliveryDriver? driver = null;
        if (SelectedDriverId.HasValue)
        {
            driver = await context.DeliveryDrivers.SingleOrDefaultAsync(item =>
                item.Id == SelectedDriverId.Value && item.AgencyId == order.IdAgencia && item.Status == "activo");
            if (driver is null)
            {
                ModelState.AddModelError(string.Empty, "El conductor no pertenece a la agencia o está inactivo.");
                await LoadAsync(userId, agencyIds);
                return Page();
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var previous = order.EstadoOrden;
        order.AssignedDriverId = driver?.Id;
        order.EstadoOrden = driver is null ? "confirmada" : "asignada";
        context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
        {
            AgencyId = order.IdAgencia,
            ShipmentOrderId = order.Id,
            EventCode = driver is null ? "DRIVER_UNASSIGNED" : "DRIVER_ASSIGNED",
            PreviousStatus = previous,
            NewStatus = order.EstadoOrden,
            Description = driver is null ? "Conductor desasignado" : $"Conductor asignado: {driver.FirstName} {driver.LastName}",
            CreatedByUserId = userId,
            EventAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        await emailNotificationService.SendTrackingUpdateAsync(order.TrackingNumber,
            driver is null ? "En preparación" : "Preparando entrega",
            driver is null ? "El envío volvió a estar pendiente de asignación." : "El envío fue asignado para su entrega.");
        StatusMessage = driver is null ? "Conductor desasignado." : "Conductor asignado correctamente.";
        return RedirectToPage(new { Tracking, Status, AgencyId, DriverId });
    }

    private async Task LoadAsync(string userId, IReadOnlyList<int> agencyIds)
    {
        Agencies = await context.CAgencias.AsNoTracking()
            .Where(a => agencyIds.Contains(a.IdAgencia))
            .OrderBy(a => a.Nombre)
            .Select(a => new AgencyOption(a.IdAgencia, a.Nombre ?? $"Agencia {a.IdAgencia}"))
            .ToListAsync();

        Drivers = await context.DeliveryDrivers.AsNoTracking()
            .Where(d => agencyIds.Contains(d.AgencyId) && d.Status == "activo")
            .OrderBy(d => d.FirstName).ThenBy(d => d.LastName)
            .Select(d => new DriverOption(d.Id, d.AgencyId, d.FirstName + " " + d.LastName))
            .ToListAsync();

        var query = context.ShipmentOrders.AsNoTracking()
            .Where(o => agencyIds.Contains(o.IdAgencia));
        if (!string.IsNullOrWhiteSpace(Tracking)) query = query.Where(o => o.TrackingNumber.Contains(Tracking.Trim()));
        if (!string.IsNullOrWhiteSpace(Status)) query = query.Where(o => o.EstadoOrden == Status);
        if (AgencyId.HasValue) query = query.Where(o => o.IdAgencia == AgencyId.Value);
        if (DriverId.HasValue) query = query.Where(o => o.AssignedDriverId == DriverId.Value);

        Total = await query.CountAsync();
        InTransit = await query.CountAsync(o => o.EstadoOrden == "en_transito" || o.EstadoOrden == "asignada");
        Delivered = await query.CountAsync(o => o.EstadoOrden == "entregada");
        Incidents = await query.CountAsync(o => o.EstadoOrden == "intento_fallido" || o.EstadoOrden == "en_retencion");

        Shipments = await (
            from order in query
            join agency in context.CAgencias.AsNoTracking() on order.IdAgencia equals agency.IdAgencia
            join driver in context.DeliveryDrivers.AsNoTracking() on order.AssignedDriverId equals driver.Id into driverGroup
            from driver in driverGroup.DefaultIfEmpty()
            orderby order.FechaCreacion descending
            select new ShipmentRow(
                order.Id, order.TrackingNumber, agency.Nombre ?? $"Agencia {order.IdAgencia}",
                order.IdAgencia, order.EstadoOrden, order.FechaCreacion, order.AssignedDriverId,
                driver == null ? null : driver.FirstName + " " + driver.LastName))
            .Take(100)
            .ToListAsync();
    }

    private async Task<List<int>> GetAuthorizedAgencyIdsAsync(string userId, bool canEdit) =>
        await context.UserAgencyPermissions.AsNoTracking()
            .Where(p => p.UserId == userId && p.CanView && (!canEdit || p.CanEdit))
            .Select(p => p.AgencyId)
            .ToListAsync();

    private Task<bool> HasPagePermissionAsync(string userId, bool canEdit) =>
        context.UserPagePermissions.AnyAsync(p => p.UserId == userId && p.PageKey == "Envios" &&
            p.CanView && (!canEdit || p.CanEdit));

    public sealed record AgencyOption(int Id, string Name);
    public sealed record DriverOption(long Id, int AgencyId, string Name);
    public sealed record ShipmentRow(Guid Id, string Tracking, string AgencyName, int AgencyId, string Status,
        DateTimeOffset CreatedAt, long? DriverId, string? DriverName);
}
