namespace Paqueteria.Web.Models;

public sealed record ShipmentAddressDraft(
    string Country,
    string ContactName,
    string? Company,
    string AddressLine1,
    string? AddressLine2,
    string PostalCode,
    string Neighborhood,
    string Municipality,
    string State,
    string? City,
    string Phone,
    string? Email,
    string? TaxId);

public sealed record ShipmentPackageDraft(
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    string Description);

public sealed record PendingShipmentQuote(
    Guid Token,
    string UserId,
    int AgencyId,
    Guid ProductId,
    Guid AgencyProductId,
    string ProductName,
    AgencyProduct AgencyProductSnapshot,
    ShipmentAddressDraft Sender,
    ShipmentAddressDraft Recipient,
    IReadOnlyList<ShipmentPackageDraft> Packages,
    bool ExtendedCoverage,
    ShipmentQuote Quote,
    DateTimeOffset ExpiresAtUtc);