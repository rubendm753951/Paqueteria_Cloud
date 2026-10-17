using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Agencias;

public sealed class CreateModel(
    PaqueteriaDbContext context,
    AgencyAccessService agencyAccess) : PageModel
{
    [BindProperty]
    public CAgencia CAgencia { get; set; } = new() { Factor = 1m };

    public int? SelectedAgencyId { get; private set; }
    public bool CanCreateAgency { get; private set; }
    public bool CanEditAgencies { get; private set; }
    public bool CanManageAgencies => CanCreateAgency || EditableAgencies.Count > 0;
    public bool IsEditing => CAgencia.IdAgencia > 0;
    public IReadOnlyList<AgencyOption> EditableAgencies { get; private set; } = [];
    public IReadOnlyList<LookupOption> Corporativos { get; private set; } = [];
    public IReadOnlyList<LookupOption> Monedas { get; private set; } = [];
    public IReadOnlyList<LookupOption> Paises { get; private set; } = [];
    public IReadOnlyList<LookupOption> Recibos { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? agencyId)
    {
        if (!await LoadAccessAsync())
        {
            return Forbid();
        }

        if (agencyId.HasValue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!CanEditAgencies || !await agencyAccess.CanEditAgencyAsync(userId, agencyId.Value))
            {
                return NotFound();
            }

            var agency = await context.CAgencias.AsNoTracking()
                .SingleOrDefaultAsync(item => item.IdAgencia == agencyId.Value);
            if (agency is null)
            {
                return NotFound();
            }

            CAgencia = agency;
            SelectedAgencyId = agency.IdAgencia;
        }

        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        CanCreateAgency = await agencyAccess.CanCreateAsync(userId);
        CanEditAgencies = await HasPagePermissionAsync(userId, "can_edit");

        CAgencia? existingAgency = null;
        if (CAgencia.IdAgencia > 0)
        {
            if (!CanEditAgencies || !await agencyAccess.CanEditAgencyAsync(userId, CAgencia.IdAgencia))
            {
                return NotFound();
            }

            existingAgency = await context.CAgencias
                .SingleOrDefaultAsync(item => item.IdAgencia == CAgencia.IdAgencia);
            if (existingAgency is null) return NotFound();
            SelectedAgencyId = existingAgency.IdAgencia;
        }
        else if (!CanCreateAgency)
        {
            return Forbid();
        }

        ModelState.Remove("CAgencia.IdCorporativoNavigation");
        ModelState.Remove("CAgencia.IdPaisNavigation");
        ModelState.Remove("CAgencia.IdMonedaNavigation");
        ModelState.Remove("CAgencia.IdReciboNavigation");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        if (!await context.CCorporativos.AnyAsync(item => item.IdCorporativo == CAgencia.IdCorporativo))
            ModelState.AddModelError("CAgencia.IdCorporativo", "Selecciona un corporativo válido.");
        if (!await context.CMonedas.AnyAsync(item => item.IdMoneda == CAgencia.IdMoneda))
            ModelState.AddModelError("CAgencia.IdMoneda", "Selecciona una moneda válida.");
        if (!await context.CPaises.AnyAsync(item => item.IdPais == CAgencia.IdPais))
            ModelState.AddModelError("CAgencia.IdPais", "Selecciona un país válido.");
        if (!await context.CRecibos.AnyAsync(item => item.IdRecibo == CAgencia.IdRecibo))
            ModelState.AddModelError("CAgencia.IdRecibo", "Selecciona un recibo válido.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        if (existingAgency is null)
        {
            existingAgency = new CAgencia();
            context.CAgencias.Add(existingAgency);
        }

        CopyEditableFields(CAgencia, existingAgency);
        await context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadAccessAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;
        CanCreateAgency = await agencyAccess.CanCreateAsync(userId);
        CanEditAgencies = await HasPagePermissionAsync(userId, "can_edit");
        return CanCreateAgency || CanEditAgencies;
    }

    private Task<bool> HasPagePermissionAsync(string userId, string action) =>
        context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId && permission.PageKey == "Agencias" &&
            (action == "can_edit" ? permission.CanEdit : permission.CanCreate));

    private async Task LoadLookupsAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && CanEditAgencies)
        {
            var editableAgencies = await (
                from grant in context.UserAgencyPermissions.AsNoTracking()
                join agency in context.CAgencias.AsNoTracking() on grant.AgencyId equals agency.IdAgencia
                where grant.UserId == userId && grant.CanEdit
                orderby agency.Nombre
                select new { agency.IdAgencia, agency.Nombre }
            ).ToListAsync();
            EditableAgencies = editableAgencies
                .Select(item => new AgencyOption(item.IdAgencia, item.Nombre ?? $"Agencia {item.IdAgencia}"))
                .ToList();
        }

        var corporativos = await context.CCorporativos.AsNoTracking()
            .OrderBy(item => item.Nombre)
            .Select(item => new { item.IdCorporativo, item.Nombre })
            .ToListAsync();
        Corporativos = corporativos.Select(item => new LookupOption(
            item.IdCorporativo, $"{item.Nombre ?? "Corporativo"} (ID {item.IdCorporativo})")).ToList();

        var monedas = await context.CMonedas.AsNoTracking()
            .OrderBy(item => item.Nombre)
            .Select(item => new { item.IdMoneda, item.Nombre, item.Abreviatura })
            .ToListAsync();
        Monedas = monedas.Select(item => new LookupOption(
            item.IdMoneda, $"{item.Nombre ?? "Moneda"} · {item.Abreviatura} (ID {item.IdMoneda})")).ToList();

        var paises = await context.CPaises.AsNoTracking()
            .OrderBy(item => item.Nombre)
            .Select(item => new { item.IdPais, item.Nombre, item.CountryCode })
            .ToListAsync();
        Paises = paises.Select(item => new LookupOption(
            item.IdPais, $"{item.Nombre}{(string.IsNullOrWhiteSpace(item.CountryCode) ? string.Empty : $" · {item.CountryCode}")} (ID {item.IdPais})")).ToList();

        var recibos = await context.CRecibos.AsNoTracking()
            .OrderBy(item => item.Descripcion)
            .Select(item => new { item.IdRecibo, item.Descripcion })
            .ToListAsync();
        Recibos = recibos.Select(item => new LookupOption(
            item.IdRecibo, $"{item.Descripcion ?? "Recibo"} (ID {item.IdRecibo})")).ToList();
    }

    private static void CopyEditableFields(CAgencia source, CAgencia target)
    {
        target.IdCorporativo = source.IdCorporativo;
        target.IdPais = source.IdPais;
        target.Nombre = source.Nombre?.Trim();
        target.Direccion = source.Direccion?.Trim();
        target.EstadoProvincia = source.EstadoProvincia?.Trim();
        target.Ciudad = source.Ciudad?.Trim();
        target.Telefono = source.Telefono?.Trim();
        target.FechaAlta = source.FechaAlta;
        target.FechaTermino = source.FechaTermino;
        target.IdMoneda = source.IdMoneda;
        target.LimiteCredito = source.LimiteCredito;
        target.Nit = source.Nit?.Trim();
        target.RequiereAsignacion = source.RequiereAsignacion;
        target.IdRecibo = source.IdRecibo;
        target.Factor = source.Factor;
        target.EsquemaPorFactor = source.EsquemaPorFactor;
        target.CostoAdicional = source.CostoAdicional;
        target.GuiaEstafeta = source.GuiaEstafeta;
        target.Noexterior = source.Noexterior?.Trim();
        target.Colonia = source.Colonia?.Trim();
        target.Municipio = source.Municipio?.Trim();
        target.CodigoPostal = source.CodigoPostal?.Trim();
        target.Email = source.Email?.Trim();
        target.Rfc = source.Rfc?.Trim();
        target.TipoDePago = source.TipoDePago?.Trim();
        target.PeRemitente = source.PeRemitente;
        target.FedexRemitente = source.FedexRemitente;
    }

    public sealed record LookupOption(int Id, string Label);
    public sealed record AgencyOption(int Id, string Name);
}
