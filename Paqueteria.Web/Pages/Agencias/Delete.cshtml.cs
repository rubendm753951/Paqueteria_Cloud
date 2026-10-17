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
    public class DeleteModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;
        private readonly AgencyAccessService _agencyAccess;

        public DeleteModel(Paqueteria.Web.Data.PaqueteriaDbContext context, AgencyAccessService agencyAccess)
        {
            _context = context;
            _agencyAccess = agencyAccess;
        }

        [BindProperty]
        public CAgencia CAgencia { get; set; } = default!;

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

            if (!await _agencyAccess.CanDeleteAgencyAsync(userId, id.Value))
            {
                return NotFound();
            }

            var cagencia = await _context.CAgencias.FirstOrDefaultAsync(m => m.IdAgencia == id);

            if (cagencia is not null)
            {
                CAgencia = cagencia;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            if (!await _agencyAccess.CanDeleteAgencyAsync(userId, id.Value))
            {
                return NotFound();
            }

            var cagencia = await _context.CAgencias.FindAsync(id);
            if (cagencia != null)
            {
                CAgencia = cagencia;
                _context.CAgencias.Remove(CAgencia);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
