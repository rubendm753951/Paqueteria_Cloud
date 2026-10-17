using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;
using Paqueteria.Web.Services;

namespace Paqueteria.Web.Pages.Rastreo;

[AllowAnonymous]
public sealed class IndexModel(PaqueteriaDbContext context, IEmailNotificationService emailNotificationService) : PageModel
{
    [BindProperty] public SearchInput Search { get; set; } = new();
    [BindProperty] public SubscriptionInput Subscription { get; set; } = new();
    public PublicShipment? Shipment { get; private set; }
    public string? SearchMessage { get; private set; }
    [TempData] public string? StatusMessage { get; set; }

    public async Task OnGetAsync(string? tracking)
    {
        if (!string.IsNullOrWhiteSpace(tracking))
        {
            Search.TrackingNumber = tracking;
            await LoadShipmentAsync(tracking);
        }
    }

    public IActionResult OnPostSearch()
    {
        var tracking = NormalizeTracking(Search.TrackingNumber);
        if (string.IsNullOrEmpty(tracking))
        {
            ModelState.AddModelError("Search.TrackingNumber", "Introduce un número de tracking.");
            return Page();
        }
        return RedirectToPage(new { tracking });
    }

    public async Task<IActionResult> OnPostSubscribeAsync()
    {
        var tracking = NormalizeTracking(Subscription.TrackingNumber);
        Subscription.TrackingNumber = tracking;
        if (!ModelState.IsValid)
        {
            await LoadShipmentAsync(tracking);
            return Page();
        }

        var shipmentExists = await context.ShipmentOrders.AsNoTracking().AnyAsync(order => order.TrackingNumber == tracking);
        if (!shipmentExists)
        {
            SearchMessage = "No encontramos un envío con ese número de tracking.";
            return Page();
        }

        var email = Subscription.Email.Trim().ToLowerInvariant();
        var existing = await context.TrackingSubscriptions.SingleOrDefaultAsync(item => item.TrackingNumber == tracking && item.Email == email);
        if (existing is null)
        {
            existing = new TrackingSubscription
            {
                TrackingNumber = tracking,
                Email = email,
                ConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.TrackingSubscriptions.Add(existing);
            await context.SaveChangesAsync();
        }
        else if (existing.Status == "unsubscribed")
        {
            existing.Status = "pending";
            existing.UnsubscribedAt = null;
            existing.ConfirmedAt = null;
            await context.SaveChangesAsync();
        }

        if (existing.Status == "confirmed")
            StatusMessage = "Este correo ya está suscrito a las actualizaciones del envío.";
        else if (await emailNotificationService.SendSubscriptionConfirmationAsync(existing))
            StatusMessage = "Revisa tu correo y confirma la suscripción para recibir actualizaciones.";
        else
            StatusMessage = "Solicitud registrada, pero el correo de confirmación no pudo enviarse. Intenta nuevamente más tarde.";

        return RedirectToPage(new { tracking });
    }

    private async Task LoadShipmentAsync(string? tracking)
    {
        var normalized = NormalizeTracking(tracking);
        if (string.IsNullOrEmpty(normalized)) return;
        var order = await context.ShipmentOrders.AsNoTracking().SingleOrDefaultAsync(item => item.TrackingNumber == normalized);
        if (order is null)
        {
            SearchMessage = "No encontramos un envío con ese número de tracking.";
            return;
        }

        var destination = await context.ShipmentAddresses.AsNoTracking()
            .Where(address => address.IdOrden == order.Id && address.TipoContacto == "destinatario")
            .Select(address => new { address.Ciudad, address.Municipio, address.Estado })
            .SingleOrDefaultAsync();
        var events = await context.ShipmentTrackingEvents.AsNoTracking()
            .Where(item => item.AgencyId == order.IdAgencia && item.ShipmentOrderId == order.Id)
            .OrderByDescending(item => item.EventAt)
            .Select(item => new PublicEvent(item.EventCode, item.NewStatus, item.EventAt))
            .ToListAsync();

        Shipment = new PublicShipment(order.TrackingNumber, PublicStatus(order.EstadoOrden), destination?.Ciudad ?? destination?.Municipio, destination?.Estado, events.Select(MapEvent).ToList());
    }

    private static PublicEventRow MapEvent(PublicEvent item) => new(PublicEventTitle(item.EventCode, item.NewStatus), item.EventAt, PublicEventDescription(item.EventCode, item.NewStatus));
    private static string NormalizeTracking(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static string PublicStatus(string status) => status switch
    {
        "entregada" => "Entregado", "en_transito" => "En tránsito", "asignada" => "Preparando entrega",
        "intento_fallido" => "Intento de entrega", "en_retencion" => "En revisión", "cancelada" => "Cancelado",
        "devuelta" => "Devuelto al origen", "confirmada" => "En preparación", _ => "Procesando"
    };
    private static string PublicEventTitle(string eventCode, string status) => eventCode switch
    {
        "DELIVERED" => "Entrega completada", "DELIVERY_ATTEMPTED" => "Intento de entrega", "DRIVER_ASSIGNED" => "Preparando entrega",
        "DRIVER_UNASSIGNED" => "En preparación", "SHIPMENT_CREATED" => "Envío registrado", _ => PublicStatus(status)
    };
    private static string PublicEventDescription(string eventCode, string status) => eventCode switch
    {
        "DELIVERED" => "El envío fue entregado.", "DELIVERY_ATTEMPTED" => "Se registró una actualización en el proceso de entrega.",
        "DRIVER_ASSIGNED" => "El envío está siendo preparado para su entrega.", "SHIPMENT_CREATED" => "Recibimos la información del envío.", _ => PublicStatus(status)
    };

    public sealed class SearchInput { [StringLength(40)] public string? TrackingNumber { get; set; } }
    public sealed class SubscriptionInput
    {
        [Required, StringLength(40)] public string TrackingNumber { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    }
    public sealed record PublicShipment(string TrackingNumber, string Status, string? City, string? State, IReadOnlyList<PublicEventRow> Events);
    public sealed record PublicEventRow(string Title, DateTimeOffset EventAt, string Description);
    private sealed record PublicEvent(string EventCode, string NewStatus, DateTimeOffset EventAt);
}
