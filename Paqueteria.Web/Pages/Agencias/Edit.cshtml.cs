using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Agencias
{
    public class EditModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;
        private readonly AgencyAccessService _agencyAccess;

        public EditModel(Paqueteria.Web.Data.PaqueteriaDbContext context, AgencyAccessService agencyAccess)
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

            if (!await _agencyAccess.CanEditAgencyAsync(userId, id.Value))
            {
                return NotFound();
            }

            return RedirectToPage("./Create", new { agencyId = id.Value });
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (!await _agencyAccess.CanEditAgencyAsync(userId, CAgencia.IdAgencia))
            {
                return NotFound();
            }

            ModelState.Remove("CAgencia.IdCorporativoNavigation");
            ModelState.Remove("CAgencia.IdPaisNavigation");
            ModelState.Remove("CAgencia.IdMonedaNavigation");
            ModelState.Remove("CAgencia.IdReciboNavigation");

            if (!ModelState.IsValid)
            {
                ViewData["IdCorporativo"] = new SelectList(
                    _context.CCorporativos,
                    "IdCorporativo",
                    "Nombre",
                    CAgencia.IdCorporativo);

                ViewData["IdPais"] = new SelectList(
                    _context.CPaises,
                    "IdPais",
                    "Nombre",
                    CAgencia.IdPais);

                ViewData["IdMoneda"] = new SelectList(
                    _context.CMonedas,
                    "IdMoneda",
                    "Nombre",
                    CAgencia.IdMoneda);

                ViewData["IdRecibo"] = new SelectList(
                    _context.CRecibos,
                    "IdRecibo",
                    "Descripcion",
                    CAgencia.IdRecibo);

                return Page();
            }

            _context.Attach(CAgencia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CAgenciaExists(CAgencia.IdAgencia))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToPage("./Index");
        }   

        private bool CAgenciaExists(int id)
        {
            return _context.CAgencias.Any(e => e.IdAgencia == id);
        }
    }
}
