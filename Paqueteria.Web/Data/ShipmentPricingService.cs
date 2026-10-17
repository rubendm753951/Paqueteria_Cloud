using Paqueteria.Web.Models;

namespace Paqueteria.Web.Data;

public sealed class ShipmentPricingService
{
    private const decimal VolumetricDivisor = 5000m;

    public ShipmentQuote Quote(
        AgencyProduct service,
        IReadOnlyCollection<AgencyProductRateRange> ranges,
        IReadOnlyCollection<PackageDimensions> packages,
        bool extendedCoverage)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(packages);

        if (!service.Activo || packages.Count is < 1 or > 10)
        {
            throw new InvalidOperationException("El servicio no está disponible o la orden debe tener de 1 a 10 paquetes.");
        }

        if (service.IncrementoPesoKg <= 0)
        {
            throw new InvalidOperationException("El incremento tarifario de peso no es válido.");
        }

        var packageQuotes = packages.Select(package => QuotePackage(service, ranges, package)).ToArray();
        var coverageCharge = extendedCoverage ? service.CoberturaExtendidaPrecio : 0m;
        var total = RoundCurrency(packageQuotes.Sum(package => package.Total) + coverageCharge);

        return new ShipmentQuote(service.Moneda.Trim(), coverageCharge, total, packageQuotes);
    }

    private static PackageQuote QuotePackage(
        AgencyProduct service,
        IReadOnlyCollection<AgencyProductRateRange> ranges,
        PackageDimensions package)
    {
        if (package.WeightKg <= 0 || package.LengthCm <= 0 || package.WidthCm <= 0 || package.HeightCm <= 0)
        {
            throw new InvalidOperationException("El peso y las tres dimensiones deben ser mayores que cero.");
        }

        var volumetric = package.LengthCm * package.WidthCm * package.HeightCm / VolumetricDivisor;
        var rawBillable = Math.Max(package.WeightKg, volumetric);
        var billable = decimal.Ceiling(rawBillable / service.IncrementoPesoKg) * service.IncrementoPesoKg;
        var range = ranges.SingleOrDefault(rate =>
            billable >= rate.PesoInicialKg &&
            (rate.PesoFinalKg is null || billable < rate.PesoFinalKg.Value));

        if (range is null)
        {
            throw new InvalidOperationException($"No hay tarifa configurada para {billable:0.###} kg.");
        }

        var extraWeight = RoundCurrency(Math.Max(0m, billable - range.PesoInicialKg) * range.PrecioKgAdicional);
        var largestSide = Math.Max(package.LengthCm, Math.Max(package.WidthCm, package.HeightCm));
        var totalSideSum = package.LengthCm + package.WidthCm + package.HeightCm;
        var isOversized = largestSide > service.LadoMaximoCm ||
            (service.SumaLadosMaximaCm.HasValue && totalSideSum > service.SumaLadosMaximaCm.Value);
        var dimensionalSurcharge = isOversized ? service.CargoDimensionesExtrasPrecio : 0m;
        var basePrice = RoundCurrency(range.PrecioBaseRango);
        var total = RoundCurrency(basePrice + extraWeight + dimensionalSurcharge);

        return new PackageQuote(
            decimal.Round(volumetric, 3, MidpointRounding.AwayFromZero),
            billable,
            basePrice,
            extraWeight,
            dimensionalSurcharge,
            total);
    }

    private static decimal RoundCurrency(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}

public sealed record PackageDimensions(
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm);