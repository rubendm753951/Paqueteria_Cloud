using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;

namespace Paqueteria.Web.Pages.Rastreo;

[AllowAnonymous]
public sealed class ConfirmarModel(PaqueteriaDbContext context) : PageModel
{
    public bool IsValid { get; private set; }
    public bool AlreadyConfirmed { get; private set; }
    public string? TrackingNumber { get; private set; }

    public async Task OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var subscription = await context.TrackingSubscriptions.SingleOrDefaultAsync(item => item.ConfirmationToken == token);
        if (subscription is null) return;
        TrackingNumber = subscription.TrackingNumber;
        if (subscription.Status == "confirmed")
        {
            IsValid = true;
            AlreadyConfirmed = true;
            return;
        }
        if (subscription.Status == "unsubscribed") return;
        subscription.Status = "confirmed";
        subscription.ConfirmedAt = DateTimeOffset.UtcNow;
        subscription.UnsubscribedAt = null;
        await context.SaveChangesAsync();
        IsValid = true;
    }
}
