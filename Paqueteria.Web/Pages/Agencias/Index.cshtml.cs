using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Agencias
{
    public class IndexModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;
        private readonly AgencyAccessService _agencyAccess;

        public IndexModel(Paqueteria.Web.Data.PaqueteriaDbContext context, AgencyAccessService agencyAccess)
        {
            _context = context;
            _agencyAccess = agencyAccess;
        }

        public IList<CAgencia> CAgencia { get;set; } = default!;

        public bool CanCreateAgency { get; private set; }

        public bool CanManageProductAssignments { get; private set; }

        public HashSet<int> EditableAgencyIds { get; private set; } = new();

        public HashSet<int> DeletableAgencyIds { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (!await _agencyAccess.CanViewPageAsync(userId))
            {
                return Forbid();
            }

            CanCreateAgency = await _agencyAccess.CanCreateAsync(userId);
            CanManageProductAssignments = await _context.UserPagePermissions.AnyAsync(permission =>
                permission.UserId == userId && permission.PageKey == "Productos" &&
                permission.CanView && permission.CanEdit);
            var canEditPage = await _context.UserPagePermissions.AnyAsync(permission =>
                permission.UserId == userId && permission.PageKey == "Agencias" && permission.CanEdit);
            var canDeletePage = await _context.UserPagePermissions.AnyAsync(permission =>
                permission.UserId == userId && permission.PageKey == "Agencias" && permission.CanDelete);

            if (canEditPage)
            {
                EditableAgencyIds = await _context.UserAgencyPermissions
                    .Where(permission => permission.UserId == userId && permission.CanEdit)
                    .Select(permission => permission.AgencyId)
                    .ToHashSetAsync();
            }

            if (canDeletePage)
            {
                DeletableAgencyIds = await _context.UserAgencyPermissions
                    .Where(permission => permission.UserId == userId && permission.CanDelete)
                    .Select(permission => permission.AgencyId)
                    .ToHashSetAsync();
            }

            CAgencia = await _context.CAgencias
                .Where(agency => _context.UserAgencyPermissions.Any(permission =>
                    permission.UserId == userId &&
                    permission.AgencyId == agency.IdAgencia &&
                    permission.CanView))
                .Include(c => c.IdCorporativoNavigation)
                .Include(c => c.IdMonedaNavigation)
                .Include(c => c.IdPaisNavigation)
                .Include(c => c.IdReciboNavigation).ToListAsync();

            return Page();
        }
    }
}
