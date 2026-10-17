namespace Paqueteria.Web.Models;

public sealed class ProductCatalogItem
{
    public Guid Id { get; set; }
    public string SkuCodigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaBaja { get; set; }
    public string Estado { get; set; } = "activo";
}

public sealed class AgencyProduct
{
    public Guid Id { get; set; }
    public int IdAgencia { get; set; }
    public Guid IdProducto { get; set; }
    public string Moneda { get; set; } = "MXN";
    public decimal IncrementoPesoKg { get; set; } = 1m;
    public decimal CoberturaExtendidaPrecio { get; set; }
    public decimal CargoDimensionesExtrasPrecio { get; set; }
    public decimal LadoMaximoCm { get; set; } = 120m;
    public decimal? SumaLadosMaximaCm { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset FechaCreacion { get; set; }
}

public sealed class AgencyProductRateRange
{
    public long Id { get; set; }
    public Guid IdAgenciaProducto { get; set; }
    public decimal PesoInicialKg { get; set; }
    public decimal? PesoFinalKg { get; set; }
    public decimal PrecioBaseRango { get; set; }
    public decimal PrecioKgAdicional { get; set; }
}

public sealed class ShipmentOrder
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int IdAgencia { get; set; }
    public Guid IdProducto { get; set; }
    public Guid IdAgenciaProducto { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public string EstadoOrden { get; set; } = "cotizada";
    public bool CoberturaExtendida { get; set; }
    public decimal CoberturaExtendidaPrecioSnapshot { get; set; }
    public decimal TotalEnvio { get; set; }
    public string Moneda { get; set; } = "MXN";
    public string? CreadaPorUserId { get; set; }
}

public sealed class ShipmentAddress
{
    public long Id { get; set; }
    public Guid IdOrden { get; set; }
    public string TipoContacto { get; set; } = string.Empty;
    public string Pais { get; set; } = "MX";
    public string NombreContacto { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string DireccionLinea1 { get; set; } = string.Empty;
    public string? DireccionLinea2 { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;
    public string Colonia { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Ciudad { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? TaxIdRfc { get; set; }
}

public sealed class ShipmentPackage
{
    public long Id { get; set; }
    public Guid IdOrden { get; set; }
    public short SecuenciaPaquete { get; set; }
    public decimal PesoBasculaKg { get; set; }
    public decimal PesoVolumetricoKg { get; set; }
    public decimal PesoCobrableKg { get; set; }
    public decimal LargoCm { get; set; }
    public decimal AnchoCm { get; set; }
    public decimal AltoCm { get; set; }
    public string DescripcionContenido { get; set; } = string.Empty;
    public bool EsDimensionExtra { get; set; }
    public decimal TarifaBaseSnapshot { get; set; }
    public decimal CargoKgAdicionalSnapshot { get; set; }
    public decimal CargoDimensionExtraSnapshot { get; set; }
    public decimal TotalPaqueteSnapshot { get; set; }
}

public sealed record PackageQuote(
    decimal VolumetricWeightKg,
    decimal BillableWeightKg,
    decimal BasePrice,
    decimal ExtraWeightPrice,
    decimal DimensionalSurcharge,
    decimal Total);

public sealed record ShipmentQuote(
    string Currency,
    decimal ExtendedCoverageSurcharge,
    decimal Total,
    IReadOnlyList<PackageQuote> Packages);