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
    public class DetailsModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;
        private readonly AgencyAccessService _agencyAccess;

        public DetailsModel(Paqueteria.Web.Data.PaqueteriaDbContext context, AgencyAccessService agencyAccess)
        {
            _context = context;
            _agencyAccess = agencyAccess;
        }

        public CAgencia CAgencia { get; set; } = default!;

        public bool CanEditAgency { get; private set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (!await _agencyAccess.CanViewPageAsync(userId))
            {
                return Forbid();
            }

            if (!await _agencyAccess.CanViewAgencyAsync(userId, id.Value))
            {
                return NotFound();
            }

            var cagencia = await _context.CAgencias
                .Include(agency => agency.IdCorporativoNavigation)
                .Include(agency => agency.IdMonedaNavigation)
                .Include(agency => agency.IdPaisNavigation)
                .Include(agency => agency.IdReciboNavigation)
                .FirstOrDefaultAsync(m => m.IdAgencia == id);

            if (cagencia is not null)
            {
                CAgencia = cagencia;
                CanEditAgency = await _agencyAccess.CanEditAgencyAsync(userId, id.Value);

                return Page();
            }

            return NotFound();
        }
    }
}
