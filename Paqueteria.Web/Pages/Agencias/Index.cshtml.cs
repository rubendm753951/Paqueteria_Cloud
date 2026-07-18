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
    public class IndexModel : PageModel
    {
        private readonly Paqueteria.Web.Data.PaqueteriaDbContext _context;

        public IndexModel(Paqueteria.Web.Data.PaqueteriaDbContext context)
        {
            _context = context;
        }

        public IList<CAgencia> CAgencia { get;set; } = default!;

        public async Task OnGetAsync()
        {
            CAgencia = await _context.CAgencias
                .Include(c => c.IdCorporativoNavigation)
                .Include(c => c.IdMonedaNavigation)
                .Include(c => c.IdPaisNavigation)
                .Include(c => c.IdReciboNavigation).ToListAsync();
        }
    }
}
