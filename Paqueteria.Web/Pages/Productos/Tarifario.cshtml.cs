using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Productos;

public sealed class TarifarioModel(PaqueteriaDbContext context) : PageModel
{
    [BindProperty]
    public TariffInput Input { get; set; } = new();

    public IReadOnlyList<ProductOption> Products { get; private set; } = [];
    public IReadOnlyList<AgencyOption> Agencies { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await HasEditPermissionAsync()) return Forbid();
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await HasEditPermissionAsync()) return Forbid();
        await LoadOptionsAsync();

        Input.SelectedAgencyIds = Input.SelectedAgencyIds.Distinct().ToList();
        if (Input.SelectedAgencyIds.Count == 0)
            ModelState.AddModelError("Input.SelectedAgencyIds", "Selecciona al menos una agencia.");
        if (Input.Ranges.Count == 0)
            ModelState.AddModelError("Input.Ranges", "Agrega al menos un rango de peso.");

        ValidateRateCoverage();
        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var allowedAgencyIds = await context.UserAgencyPermissions
            .Where(grant => grant.UserId == userId && grant.CanEdit && Input.SelectedAgencyIds.Contains(grant.AgencyId))
            .Select(grant => grant.AgencyId)
            .ToListAsync();
        if (allowedAgencyIds.Count != Input.SelectedAgencyIds.Count)
            return Forbid();

        var product = await context.Products.SingleOrDefaultAsync(item =>
            item.Id == Input.ProductId && item.Estado == "activo" && item.FechaBaja == null);
        if (product is null) return NotFound();

        var agencies = await context.CAgencias
            .Where(agency => Input.SelectedAgencyIds.Contains(agency.IdAgencia))
            .Select(agency => new { agency.IdAgencia, Currency = agency.IdMonedaNavigation.Abreviatura })
            .ToDictionaryAsync(agency => agency.IdAgencia, agency => agency.Currency);

        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var agencyId in Input.SelectedAgencyIds)
        {
            var assignment = await context.AgencyProducts.SingleOrDefaultAsync(item =>
                item.IdAgencia == agencyId && item.IdProducto == product.Id);

            if (assignment is null)
            {
                assignment = new AgencyProduct
                {
                    Id = Guid.NewGuid(),
                    IdAgencia = agencyId,
                    IdProducto = product.Id,
                    FechaCreacion = DateTimeOffset.UtcNow
                };
                context.AgencyProducts.Add(assignment);
            }

            assignment.Moneda = agencies[agencyId];
            assignment.IncrementoPesoKg = Input.WeightIncrementKg;
            assignment.CoberturaExtendidaPrecio = Input.ExtendedCoveragePrice;
            assignment.CargoDimensionesExtrasPrecio = Input.OversizePrice;
            assignment.LadoMaximoCm = Input.MaxSideCm;
            assignment.SumaLadosMaximaCm = Input.MaxSideSumCm;
            assignment.Activo = true;

            await context.SaveChangesAsync();
            await context.AgencyProductRateRanges
                .Where(rate => rate.IdAgenciaProducto == assignment.Id)
                .ExecuteDeleteAsync();

            foreach (var range in Input.Ranges.OrderBy(item => item.StartKg))
            {
                context.AgencyProductRateRanges.Add(new AgencyProductRateRange
                {
                    IdAgenciaProducto = assignment.Id,
                    PesoInicialKg = range.StartKg,
                    PesoFinalKg = range.EndKg,
                    PrecioBaseRango = range.BasePrice,
                    PrecioKgAdicional = range.AdditionalKgPrice
                });
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        SuccessMessage = $"Tarifario aplicado a {Input.SelectedAgencyIds.Count} agencia(s).";
        return RedirectToPage("./Tarifario");
    }

    private async Task LoadOptionsAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Products = [];
            Agencies = [];
            return;
        }

        Products = await context.Products.AsNoTracking()
            .Where(item => item.Estado == "activo" && item.FechaBaja == null)
            .OrderBy(item => item.Nombre)
            .Select(item => new ProductOption(item.Id, item.SkuCodigo, item.Nombre))
            .ToListAsync();

        Agencies = await (
            from grant in context.UserAgencyPermissions.AsNoTracking()
            join agency in context.CAgencias.AsNoTracking() on grant.AgencyId equals agency.IdAgencia
            where grant.UserId == userId && grant.CanEdit
            orderby agency.Nombre
            select new AgencyOption(agency.IdAgencia, agency.Nombre ?? $"Agencia {agency.IdAgencia}")
        ).ToListAsync();
    }

    private void ValidateRateCoverage()
    {
        var ranges = Input.Ranges.OrderBy(item => item.StartKg).ToArray();
        if (ranges.Length == 0) return;
        if (ranges[0].StartKg != 0)
            ModelState.AddModelError("Input.Ranges", "El primer rango debe comenzar en 0 kg.");

        for (var index = 0; index < ranges.Length; index++)
        {
            var current = ranges[index];
            var isLast = index == ranges.Length - 1;
            if (isLast && current.EndKg.HasValue)
                ModelState.AddModelError("Input.Ranges", "El último rango debe dejar el peso final vacío para cubrir pesos mayores.");
            if (!isLast && !current.EndKg.HasValue)
                ModelState.AddModelError("Input.Ranges", "Solo el último rango puede tener peso final abierto.");
            if (!isLast && current.EndKg.HasValue && current.EndKg.Value != ranges[index + 1].StartKg)
                ModelState.AddModelError("Input.Ranges", "Los rangos deben ser contiguos, sin traslapes ni huecos.");
        }
    }

    private Task<bool> HasEditPermissionAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId)
            ? context.UserPagePermissions.AnyAsync(permission => permission.UserId == userId &&
                permission.PageKey == "Productos" && permission.CanView && permission.CanEdit)
            : Task.FromResult(false);
    }

    public sealed record ProductOption(Guid Id, string Sku, string Name);
    public sealed record AgencyOption(int Id, string Name);

    public sealed class TariffInput
    {
        [Required] public Guid? ProductId { get; set; }
        public List<int> SelectedAgencyIds { get; set; } = [];
        [Range(typeof(decimal), "0.001", "1000")] public decimal WeightIncrementKg { get; set; } = 1m;
        [Range(typeof(decimal), "0", "999999999")] public decimal ExtendedCoveragePrice { get; set; }
        [Range(typeof(decimal), "0", "999999999")] public decimal OversizePrice { get; set; }
        [Range(typeof(decimal), "1", "999999")] public decimal MaxSideCm { get; set; } = 120m;
        [Range(typeof(decimal), "1", "999999")] public decimal? MaxSideSumCm { get; set; } = 250m;
        public List<RateInput> Ranges { get; set; } = [new()];
    }

    public sealed class RateInput
    {
        [Range(typeof(decimal), "0", "999999")] public decimal StartKg { get; set; }
        [Range(typeof(decimal), "0.001", "999999")] public decimal? EndKg { get; set; }
        [Range(typeof(decimal), "0", "999999999")] public decimal BasePrice { get; set; }
        [Range(typeof(decimal), "0", "999999999")] public decimal AdditionalKgPrice { get; set; }
    }
}