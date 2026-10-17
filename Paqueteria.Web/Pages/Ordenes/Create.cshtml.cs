using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Ordenes;

public sealed class CreateModel(
    PaqueteriaDbContext context,
    AgencyAccessService agencyAccess,
    ShipmentPricingService pricingService,
    PendingShipmentQuoteStore pendingQuotes) : PageModel
{
    [BindProperty]
    public OrderInput Input { get; set; } = new();

    public IReadOnlyList<AgencyOption> Agencies { get; private set; } = [];

    [BindProperty]
    public Guid? QuoteToken { get; set; }

    public QuoteReview? QuotePreview { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? CreatedTrackingNumber { get; set; }

    [TempData]
    public string? CreatedTotalText { get; set; }

    [TempData]
    public string? CreatedCurrency { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await HasAgencyPageAccessAsync())
        {
            return Forbid();
        }

        await LoadAgenciesAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetProductsAsync(int agencyId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) ||
            !await CanCreateOrdersAsync(userId) ||
            !await agencyAccess.CanViewPageAsync(userId) ||
            !await agencyAccess.CanViewAgencyAsync(userId, agencyId))
        {
            return Forbid();
        }

        var products = await (
            from assignment in context.AgencyProducts.AsNoTracking()
            join product in context.Products.AsNoTracking() on assignment.IdProducto equals product.Id
            where assignment.IdAgencia == agencyId && assignment.Activo && product.Estado == "activo" &&
                product.FechaBaja == null && context.AgencyProductRateRanges.Any(rate => rate.IdAgenciaProducto == assignment.Id)
            orderby product.Nombre
            select new
            {
                id = product.Id,
                label = product.Nombre,
                sku = product.SkuCodigo,
                currency = assignment.Moneda,
                weightIncrementKg = assignment.IncrementoPesoKg,
                extendedCoveragePrice = assignment.CoberturaExtendidaPrecio,
                maxSideCm = assignment.LadoMaximoCm,
                maxSideSumCm = assignment.SumaLadosMaximaCm
            }).ToListAsync();

        return new JsonResult(products);
    }

    public async Task<IActionResult> OnPostQuoteAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (!await CanCreateOrdersAsync(userId) ||
            !await agencyAccess.CanViewPageAsync(userId) ||
            !await agencyAccess.CanViewAgencyAsync(userId, Input.AgencyId))
        {
            return Forbid();
        }

        if (Input.Packages.Count is < 1 or > 10)
        {
            ModelState.AddModelError("Input.Packages", "La orden debe contener entre 1 y 10 paquetes.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAgenciesAsync();
            return Page();
        }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            var product = await context.Products.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == Input.ProductId && item.Estado == "activo" && item.FechaBaja == null);
            if (product is null)
            {
                ModelState.AddModelError("Input.ProductId", "El producto seleccionado ya no está disponible.");
                await LoadAgenciesAsync();
                return Page();
            }

            var agencyProduct = await context.AgencyProducts.AsNoTracking().SingleOrDefaultAsync(assignment =>
                assignment.IdAgencia == Input.AgencyId && assignment.IdProducto == Input.ProductId && assignment.Activo &&
                context.Products.Any(product => product.Id == assignment.IdProducto &&
                    product.Estado == "activo" && product.FechaBaja == null));
            if (agencyProduct is null)
            {
                return NotFound();
            }

            var ranges = await context.AgencyProductRateRanges
                .AsNoTracking()
                .Where(rate => rate.IdAgenciaProducto == agencyProduct.Id)
                .OrderBy(rate => rate.PesoInicialKg)
                .ToListAsync();

            var quote = pricingService.Quote(
                agencyProduct,
                ranges,
                Input.Packages.Select(package => new PackageDimensions(
                    package.WeightKg!.Value,
                    package.LengthCm!.Value,
                    package.WidthCm!.Value,
                    package.HeightCm!.Value)).ToArray(),
                Input.ExtendedCoverage);
            await transaction.CommitAsync();

            var pending = new PendingShipmentQuote(
                Guid.NewGuid(),
                userId,
                Input.AgencyId,
                Input.ProductId!.Value,
                agencyProduct.Id,
                product.Nombre,
                agencyProduct,
                ToDraft(Input.Sender),
                ToDraft(Input.Recipient),
                Input.Packages.Select(ToDraft).ToArray(),
                Input.ExtendedCoverage,
                quote,
                PendingShipmentQuoteStore.GetExpiration());

            pendingQuotes.Add(pending);
            QuoteToken = pending.Token;
            QuotePreview = new QuoteReview(
                pending.Token,
                pending.ProductName,
                quote.Currency,
                quote.ExtendedCoverageSurcharge,
                quote.Total,
                quote.Packages,
                pending.ExpiresAtUtc);
            await LoadAgenciesAsync();
            return Page();
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAgenciesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (QuoteToken is not Guid token || !pendingQuotes.TryTake(token, userId, out var pending))
        {
            ModelState.AddModelError(string.Empty, "La cotización expiró o ya fue procesada. Vuelve a cotizar la orden.");
            await LoadAgenciesAsync();
            return Page();
        }

        Input = RestoreInput(pending);
        StatusMessage = "Cotización rechazada. Puedes modificar los datos del envío.";
        await LoadAgenciesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (QuoteToken is not Guid token || !pendingQuotes.TryClaim(token, userId, out var pending))
        {
            ModelState.AddModelError(string.Empty, "La cotización expiró o ya fue procesada. Vuelve a cotizar la orden.");
            await LoadAgenciesAsync();
            return Page();
        }

        if (!await CanCreateOrdersAsync(userId) ||
            !await agencyAccess.CanViewPageAsync(userId) ||
            !await agencyAccess.CanViewAgencyAsync(userId, pending.AgencyId))
        {
            pendingQuotes.Release(token);
            return Forbid();
        }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            var assignmentIsActive = await context.AgencyProducts.AnyAsync(assignment =>
                assignment.Id == pending.AgencyProductId &&
                assignment.IdAgencia == pending.AgencyId &&
                assignment.IdProducto == pending.ProductId &&
                assignment.Activo &&
                context.Products.Any(product => product.Id == assignment.IdProducto &&
                    product.Estado == "activo" && product.FechaBaja == null));

            if (!assignmentIsActive)
            {
                await transaction.RollbackAsync();
                pendingQuotes.Release(token);
                ModelState.AddModelError(string.Empty, "El producto o la tarifa dejó de estar disponible. Vuelve al formulario y cotiza de nuevo.");
                await LoadAgenciesAsync();
                return Page();
            }

            var orderId = Guid.NewGuid();
            var trackingNumber = $"SXP{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..34].ToUpperInvariant();
            var order = new ShipmentOrder
            {
                Id = orderId,
                TrackingNumber = trackingNumber,
                IdAgencia = pending.AgencyId,
                IdProducto = pending.ProductId,
                IdAgenciaProducto = pending.AgencyProductId,
                FechaCreacion = DateTimeOffset.UtcNow,
                EstadoOrden = "cotizada",
                CoberturaExtendida = pending.ExtendedCoverage,
                CoberturaExtendidaPrecioSnapshot = pending.Quote.ExtendedCoverageSurcharge,
                TotalEnvio = pending.Quote.Total,
                Moneda = pending.Quote.Currency,
                CreadaPorUserId = userId
            };

            context.ShipmentOrders.Add(order);
            context.ShipmentAddresses.Add(CreateAddress(orderId, "remitente", pending.Sender));
            context.ShipmentAddresses.Add(CreateAddress(orderId, "destinatario", pending.Recipient));

            for (var index = 0; index < pending.Packages.Count; index++)
            {
                var package = pending.Packages[index];
                var packageQuote = pending.Quote.Packages[index];
                context.ShipmentPackages.Add(new ShipmentPackage
                {
                    IdOrden = orderId,
                    SecuenciaPaquete = checked((short)(index + 1)),
                    PesoBasculaKg = package.WeightKg,
                    PesoVolumetricoKg = packageQuote.VolumetricWeightKg,
                    PesoCobrableKg = packageQuote.BillableWeightKg,
                    LargoCm = package.LengthCm,
                    AnchoCm = package.WidthCm,
                    AltoCm = package.HeightCm,
                    DescripcionContenido = package.Description,
                    EsDimensionExtra = packageQuote.DimensionalSurcharge > 0,
                    TarifaBaseSnapshot = packageQuote.BasePrice,
                    CargoKgAdicionalSnapshot = packageQuote.ExtraWeightPrice,
                    CargoDimensionExtraSnapshot = packageQuote.DimensionalSurcharge,
                    TotalPaqueteSnapshot = packageQuote.Total
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            pendingQuotes.Consume(token);

            CreatedTrackingNumber = trackingNumber;
            CreatedTotalText = pending.Quote.Total.ToString("N2");
            CreatedCurrency = pending.Quote.Currency;
            StatusMessage = "Cotización aceptada y orden creada.";
            return RedirectToPage("./Create");
        }
        catch
        {
            pendingQuotes.Release(token);
            throw;
        }
    }

    private async Task<bool> HasAgencyPageAccessAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) &&
            await CanCreateOrdersAsync(userId) &&
            await agencyAccess.CanViewPageAsync(userId);
    }

    private Task<bool> CanCreateOrdersAsync(string userId) =>
        context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId && permission.PageKey == "Ordenes" &&
            permission.CanView && permission.CanCreate);

    private async Task LoadAgenciesAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Agencies = [];
            return;
        }

        Agencies = await (
            from grant in context.UserAgencyPermissions.AsNoTracking()
            join agency in context.CAgencias.AsNoTracking() on grant.AgencyId equals agency.IdAgencia
            where grant.UserId == userId && grant.CanView
            orderby agency.Nombre
            select new AgencyOption(agency.IdAgencia, agency.Nombre ?? $"Agencia {agency.IdAgencia}")
        ).ToListAsync();
    }

    private static AddressInput RestoreAddress(ShipmentAddressDraft draft) => new()
    {
        Country = draft.Country,
        ContactName = draft.ContactName,
        Company = draft.Company,
        AddressLine1 = draft.AddressLine1,
        AddressLine2 = draft.AddressLine2,
        PostalCode = draft.PostalCode,
        Neighborhood = draft.Neighborhood,
        Municipality = draft.Municipality,
        State = draft.State,
        City = draft.City,
        Phone = draft.Phone,
        Email = draft.Email,
        TaxId = draft.TaxId
    };

    private static ShipmentAddressDraft ToDraft(AddressInput input) => new(
        input.Country.Trim().ToUpperInvariant(),
        input.ContactName.Trim(),
        input.Company?.Trim(),
        input.AddressLine1.Trim(),
        input.AddressLine2?.Trim(),
        input.PostalCode.Trim(),
        input.Neighborhood.Trim(),
        input.Municipality.Trim(),
        input.State.Trim(),
        input.City?.Trim(),
        input.Phone.Trim(),
        input.Email?.Trim(),
        input.TaxId?.Trim());

    private static ShipmentPackageDraft ToDraft(PackageInput input) => new(
        input.WeightKg!.Value,
        input.LengthCm!.Value,
        input.WidthCm!.Value,
        input.HeightCm!.Value,
        input.Description.Trim());

    private static OrderInput RestoreInput(PendingShipmentQuote pending) => new()
    {
        AgencyId = pending.AgencyId,
        ProductId = pending.ProductId,
        ExtendedCoverage = pending.ExtendedCoverage,
        Sender = RestoreAddress(pending.Sender),
        Recipient = RestoreAddress(pending.Recipient),
        Packages = pending.Packages.Select(package => new PackageInput
        {
            WeightKg = package.WeightKg,
            LengthCm = package.LengthCm,
            WidthCm = package.WidthCm,
            HeightCm = package.HeightCm,
            Description = package.Description
        }).ToList()
    };

    private static ShipmentAddress CreateAddress(Guid orderId, string type, AddressInput input) => new()
    {
        IdOrden = orderId,
        TipoContacto = type,
        Pais = input.Country.Trim().ToUpperInvariant(),
        NombreContacto = input.ContactName.Trim(),
        Empresa = input.Company?.Trim(),
        DireccionLinea1 = input.AddressLine1.Trim(),
        DireccionLinea2 = input.AddressLine2?.Trim(),
        CodigoPostal = input.PostalCode.Trim(),
        Colonia = input.Neighborhood.Trim(),
        Municipio = input.Municipality.Trim(),
        Estado = input.State.Trim(),
        Ciudad = input.City?.Trim(),
        Telefono = input.Phone.Trim(),
        Email = input.Email?.Trim(),
        TaxIdRfc = input.TaxId?.Trim()
    };

    private static ShipmentAddress CreateAddress(Guid orderId, string type, ShipmentAddressDraft draft) => new()
    {
        IdOrden = orderId,
        TipoContacto = type,
        Pais = draft.Country,
        NombreContacto = draft.ContactName,
        Empresa = draft.Company,
        DireccionLinea1 = draft.AddressLine1,
        DireccionLinea2 = draft.AddressLine2,
        CodigoPostal = draft.PostalCode,
        Colonia = draft.Neighborhood,
        Municipio = draft.Municipality,
        Estado = draft.State,
        Ciudad = draft.City,
        Telefono = draft.Phone,
        Email = draft.Email,
        TaxIdRfc = draft.TaxId
    };

    public sealed record AgencyOption(int Id, string Name);

    public sealed record QuoteReview(
        Guid Token,
        string ProductName,
        string Currency,
        decimal ExtendedCoverageSurcharge,
        decimal Total,
        IReadOnlyList<PackageQuote> Packages,
        DateTimeOffset ExpiresAtUtc);

    public sealed class OrderInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una agencia.")]
        public int AgencyId { get; set; }

        [Required(ErrorMessage = "Selecciona un producto.")]
        public Guid? ProductId { get; set; }

        public bool ExtendedCoverage { get; set; }

        public AddressInput Sender { get; set; } = new();

        public AddressInput Recipient { get; set; } = new();

        public List<PackageInput> Packages { get; set; } = [new()];
    }

    public sealed class AddressInput
    {
        [Required, StringLength(160)] public string ContactName { get; set; } = string.Empty;
        [StringLength(160)] public string? Company { get; set; }
        [Required, StringLength(200)] public string AddressLine1 { get; set; } = string.Empty;
        [StringLength(200)] public string? AddressLine2 { get; set; }
        [Required, StringLength(12)] public string PostalCode { get; set; } = string.Empty;
        [Required, StringLength(120)] public string Neighborhood { get; set; } = string.Empty;
        [Required, StringLength(120)] public string Municipality { get; set; } = string.Empty;
        [Required, StringLength(120)] public string State { get; set; } = string.Empty;
        [StringLength(120)] public string? City { get; set; }
        [Required, StringLength(30)] public string Phone { get; set; } = string.Empty;
        [EmailAddress, StringLength(254)] public string? Email { get; set; }
        [StringLength(20)] public string? TaxId { get; set; }
        [Required, RegularExpression("^[A-Za-z]{2}$")] public string Country { get; set; } = "MX";
    }

    public sealed class PackageInput
    {
        [Required, Range(typeof(decimal), "0.001", "999999", ErrorMessage = "El peso debe ser mayor que cero.")]
        public decimal? WeightKg { get; set; }

        [Required, Range(typeof(decimal), "0.01", "999999")] public decimal? LengthCm { get; set; }
        [Required, Range(typeof(decimal), "0.01", "999999")] public decimal? WidthCm { get; set; }
        [Required, Range(typeof(decimal), "0.01", "999999")] public decimal? HeightCm { get; set; }
        [Required, StringLength(240)] public string Description { get; set; } = string.Empty;
    }
}