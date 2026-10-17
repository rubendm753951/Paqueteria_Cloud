using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;

namespace Paqueteria.Web.Pages.Rastreo;

[AllowAnonymous]
public sealed class CancelarModel(PaqueteriaDbContext context) : PageModel
{
    public bool IsValid { get; private set; }
    public string? TrackingNumber { get; private set; }

    public async Task OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var subscription = await context.TrackingSubscriptions.SingleOrDefaultAsync(item => item.ConfirmationToken == token);
        if (subscription is null) return;
        TrackingNumber = subscription.TrackingNumber;
        subscription.Status = "unsubscribed";
        subscription.UnsubscribedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        IsValid = true;
    }
}
