using System;
using System.Collections.Generic;
using System.Linq;
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

        public EditModel(Paqueteria.Web.Data.PaqueteriaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CAgencia CAgencia { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cagencia =  await _context.CAgencias.FirstOrDefaultAsync(m => m.IdAgencia == id);
            if (cagencia == null)
            {
                return NotFound();
            }
            CAgencia = cagencia;
           ViewData["IdCorporativo"] = new SelectList(_context.CCorporativos, "IdCorporativo", "IdCorporativo");
           ViewData["IdMoneda"] = new SelectList(_context.CMonedas, "IdMoneda", "IdMoneda");
           ViewData["IdPais"] = new SelectList(_context.CPaises, "IdPais", "IdPais");
           ViewData["IdRecibo"] = new SelectList(_context.CRecibos, "IdRecibo", "IdRecibo");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
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
