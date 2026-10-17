using Microsoft.EntityFrameworkCore;

namespace Paqueteria.Web.Data;

public sealed class AgencyAccessService(PaqueteriaDbContext context)
{
    private const string AgenciesPage = "Agencias";

    public Task<bool> CanViewPageAsync(string userId) =>
        context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.PageKey == AgenciesPage &&
            permission.CanView);

    public Task<bool> CanCreateAsync(string userId) =>
        context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.PageKey == AgenciesPage &&
            permission.CanCreate);

    public async Task<bool> CanViewAgencyAsync(string userId, int agencyId) =>
        await CanViewPageAsync(userId) &&
        await context.UserAgencyPermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.AgencyId == agencyId &&
            permission.CanView);

    public async Task<bool> CanEditAgencyAsync(string userId, int agencyId) =>
        await context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.PageKey == AgenciesPage &&
            permission.CanEdit) &&
        await context.UserAgencyPermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.AgencyId == agencyId &&
            permission.CanEdit);

    public async Task<bool> CanDeleteAgencyAsync(string userId, int agencyId) =>
        await context.UserPagePermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.PageKey == AgenciesPage &&
            permission.CanDelete) &&
        await context.UserAgencyPermissions.AnyAsync(permission =>
            permission.UserId == userId &&
            permission.AgencyId == agencyId &&
            permission.CanDelete);
}