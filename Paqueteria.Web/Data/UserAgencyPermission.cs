namespace Paqueteria.Web.Data;

public sealed class UserAgencyPermission
{
    public string UserId { get; set; } = string.Empty;

    public int AgencyId { get; set; }

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}