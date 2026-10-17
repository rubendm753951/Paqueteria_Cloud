using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;

namespace Paqueteria.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly PaqueteriaDbContext _context;

        public IndexModel(PaqueteriaDbContext context)
        {
            _context = context;
        }

        public string DisplayName => User.Identity?.Name ?? "there";

        public int AgencyCount { get; private set; }

        public bool CanViewAgencies { get; private set; }

        public bool CanCreateOrders { get; private set; }

        public bool CanViewProducts { get; private set; }

        public int AvailableModuleCount => (CanViewAgencies ? 1 : 0) +
            (CanCreateOrders ? 1 : 0) + (CanViewProducts ? 1 : 0);

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            CanViewAgencies = await _context.UserPagePermissions
                .AnyAsync(permission => permission.UserId == userId &&
                    permission.PageKey == "Agencias" && permission.CanView);

            CanCreateOrders = await _context.UserPagePermissions
                .AnyAsync(permission => permission.UserId == userId &&
                    permission.PageKey == "Ordenes" && permission.CanView && permission.CanCreate);

            CanViewProducts = await _context.UserPagePermissions
                .AnyAsync(permission => permission.UserId == userId &&
                    permission.PageKey == "Productos" && permission.CanView);

            if (CanViewAgencies)
            {
                AgencyCount = await _context.UserAgencyPermissions
                    .CountAsync(permission => permission.UserId == userId && permission.CanView);
            }

            return Page();
        }
    }
}
