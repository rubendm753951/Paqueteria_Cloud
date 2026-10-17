namespace Paqueteria.Web.Data;

public sealed class UserPagePermission
{
    public string UserId { get; set; } = string.Empty;

    public string PageKey { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}