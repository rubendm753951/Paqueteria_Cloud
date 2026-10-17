using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Conductores;

public sealed class IndexModel(PaqueteriaDbContext context) : PageModel
{
    public IReadOnlyList<DriverRow> Drivers { get; private set; } = [];
    public IReadOnlyList<AgencyOption> Agencies { get; private set; } = [];
    [BindProperty] public DriverInput Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await CanAccessAsync(userId, false)) return Forbid();
        await LoadAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await CanAccessAsync(userId, true)) return Forbid();
        var agencyIds = await AgencyIdsAsync(userId, true);
        if (!agencyIds.Contains(Input.AgencyId)) return Forbid();
        if (!ModelState.IsValid) { await LoadAsync(userId); return Page(); }
        var exists = await context.DeliveryDrivers.AnyAsync(d => d.AgencyId == Input.AgencyId && d.EmployeeCode == Input.EmployeeCode.Trim());
        if (exists) { ModelState.AddModelError("Input.EmployeeCode", "Ya existe un conductor con ese código en la agencia."); await LoadAsync(userId); return Page(); }
        context.DeliveryDrivers.Add(new DeliveryDriver { AgencyId = Input.AgencyId, EmployeeCode = Input.EmployeeCode.Trim(), FirstName = Input.FirstName.Trim(), LastName = Input.LastName.Trim(), Phone = Input.Phone.Trim(), LicenseNumber = Input.LicenseNumber?.Trim(), Status = "activo" });
        await context.SaveChangesAsync();
        StatusMessage = "Conductor creado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        if (!await CanAccessAsync(userId, true)) return Forbid();
        var agencyIds = await AgencyIdsAsync(userId, true);
        var driver = await context.DeliveryDrivers.SingleOrDefaultAsync(d => d.Id == id && agencyIds.Contains(d.AgencyId));
        if (driver is null) return NotFound();
        driver.Status = driver.Status == "activo" ? "inactivo" : "activo";
        await context.SaveChangesAsync();
        StatusMessage = "Estado del conductor actualizado.";
        return RedirectToPage();
    }

    private async Task LoadAsync(string userId)
    {
        var ids = await AgencyIdsAsync(userId, false);
        Agencies = await context.CAgencias.AsNoTracking().Where(a => ids.Contains(a.IdAgencia)).OrderBy(a => a.Nombre).Select(a => new AgencyOption(a.IdAgencia, a.Nombre ?? $"Agencia {a.IdAgencia}")).ToListAsync();
        Drivers = await (from d in context.DeliveryDrivers.AsNoTracking() join a in context.CAgencias.AsNoTracking() on d.AgencyId equals a.IdAgencia where ids.Contains(d.AgencyId) orderby a.Nombre, d.FirstName select new DriverRow(d.Id, d.AgencyId, a.Nombre ?? $"Agencia {d.AgencyId}", d.EmployeeCode, d.FirstName + " " + d.LastName, d.Phone, d.Status)).ToListAsync();
    }
    private Task<List<int>> AgencyIdsAsync(string userId, bool canEdit) => context.UserAgencyPermissions.AsNoTracking().Where(p => p.UserId == userId && p.CanView && (!canEdit || p.CanEdit)).Select(p => p.AgencyId).ToListAsync();
    private Task<bool> CanAccessAsync(string userId, bool canEdit) => context.UserPagePermissions.AnyAsync(p => p.UserId == userId && p.PageKey == "Envios" && p.CanView && (!canEdit || p.CanEdit));

    public sealed record DriverRow(long Id, int AgencyId, string AgencyName, string EmployeeCode, string Name, string Phone, string Status);
    public sealed record AgencyOption(int Id, string Name);
    public sealed class DriverInput
    {
        [Range(1, int.MaxValue)] public int AgencyId { get; set; }
        [Required, StringLength(50)] public string EmployeeCode { get; set; } = string.Empty;
        [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
        [Required, StringLength(30)] public string Phone { get; set; } = string.Empty;
        [StringLength(80)] public string? LicenseNumber { get; set; }
    }
}
