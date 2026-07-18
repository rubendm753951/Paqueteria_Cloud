using System;
using System.Collections.Generic;
using System.Linq;
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

        public DeleteModel(Paqueteria.Web.Data.PaqueteriaDbContext context)
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
