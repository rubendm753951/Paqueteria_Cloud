using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Agencias
{
    public class CreateModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;

        public CreateModel(Paqueteria.Web.Data.PaqueteriaDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["IdCorporativo"] = new SelectList(_context.CCorporativos, "IdCorporativo", "IdCorporativo");
        ViewData["IdMoneda"] = new SelectList(_context.CMonedas, "IdMoneda", "IdMoneda");
        ViewData["IdPais"] = new SelectList(_context.CPaises, "IdPais", "IdPais");
        ViewData["IdRecibo"] = new SelectList(_context.CRecibos, "IdRecibo", "IdRecibo");
            return Page();
        }

        [BindProperty]
        public CAgencia CAgencia { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // Las propiedades de navegación no son enviadas por el formulario.
            ModelState.Remove("CAgencia.IdCorporativoNavigation");
            ModelState.Remove("CAgencia.IdPaisNavigation");
            ModelState.Remove("CAgencia.IdMonedaNavigation");
            ModelState.Remove("CAgencia.IdReciboNavigation");

            if (!ModelState.IsValid)
            {
                // Repite aquí las mismas líneas ViewData que tienes en OnGet().
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

            _context.CAgencias.Add(CAgencia);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
