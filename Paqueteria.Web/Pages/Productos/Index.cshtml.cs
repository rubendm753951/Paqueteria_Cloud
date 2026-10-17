using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Pages.Productos;

public sealed class IndexModel(PaqueteriaDbContext context) : PageModel
{
    private const int PageSize = 20;

    [BindProperty]
    public ProductInput NewProduct { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<ProductCatalogItem> Products { get; private set; } = [];
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDeactivate { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        if (!await HasPermissionAsync("can_view")) return Forbid();
        CanCreate = await HasPermissionAsync("can_create");
        CanEdit = await HasPermissionAsync("can_edit");
        CanDeactivate = await HasPermissionAsync("can_delete");
        await LoadProductsAsync(page);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!await HasPermissionAsync("can_create")) return Forbid();
        if (!ModelState.IsValid || await context.Products.AnyAsync(item => item.SkuCodigo == NewProduct.SkuCodigo.Trim()))
        {
            if (ModelState.IsValid)
                ModelState.AddModelError("NewProduct.SkuCodigo", "Este SKU ya existe.");
            CanCreate = true;
            CanEdit = await HasPermissionAsync("can_edit");
            CanDeactivate = await HasPermissionAsync("can_delete");
            await LoadProductsAsync(1);
            return Page();
        }

        context.Products.Add(new ProductCatalogItem
        {
            Id = Guid.NewGuid(),
            SkuCodigo = NewProduct.SkuCodigo.Trim(),
            Nombre = NewProduct.Nombre.Trim(),
            Descripcion = NewProduct.Descripcion?.Trim(),
            FechaCreacion = DateTimeOffset.UtcNow,
            Estado = "activo"
        });
        await context.SaveChangesAsync();
        return RedirectToPage("./Index", new { page = 1, search = Search });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        if (!await HasPermissionAsync("can_delete")) return Forbid();
        var product = await context.Products.SingleOrDefaultAsync(item => item.Id == id);
        if (product is null) return NotFound();
        product.Estado = "inactivo";
        product.FechaBaja = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        StatusMessage = "Producto dado de baja; se conservó su historial.";
        return RedirectToPage("./Index", new { page = 1, search = Search });
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string skuCodigo, string nombre, string? descripcion)
    {
        if (!await HasPermissionAsync("can_edit")) return Forbid();
        skuCodigo = skuCodigo.Trim();
        nombre = nombre.Trim();
        if (string.IsNullOrWhiteSpace(skuCodigo) || skuCodigo.Length > 50 ||
            string.IsNullOrWhiteSpace(nombre) || nombre.Length > 160 ||
            (descripcion?.Length ?? 0) > 2000)
        {
            StatusMessage = "Revisa SKU, nombre y descripción antes de guardar.";
            return RedirectToPage("./Index", new { page = CurrentPage, search = Search });
        }

        if (await context.Products.AnyAsync(item => item.Id != id && item.SkuCodigo == skuCodigo))
        {
            StatusMessage = "El SKU ya pertenece a otro producto.";
            return RedirectToPage("./Index", new { page = CurrentPage, search = Search });
        }

        var product = await context.Products.SingleOrDefaultAsync(item => item.Id == id);
        if (product is null) return NotFound();
        product.SkuCodigo = skuCodigo;
        product.Nombre = nombre;
        product.Descripcion = descripcion?.Trim();
        await context.SaveChangesAsync();
        StatusMessage = "Producto actualizado.";
        return RedirectToPage("./Index", new { page = CurrentPage, search = Search });
    }

    private async Task LoadProductsAsync(int page)
    {
        var query = context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(item => EF.Functions.ILike(item.SkuCodigo, $"%{search}%") ||
                EF.Functions.ILike(item.Nombre, $"%{search}%"));
        }

        var count = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        CurrentPage = Math.Clamp(page, 1, TotalPages);
        Products = await query.OrderBy(item => item.Nombre)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    private Task<bool> HasPermissionAsync(string action)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Task.FromResult(false);

        var permissions = context.UserPagePermissions
            .Where(permission => permission.UserId == userId && permission.PageKey == "Productos");

        return action switch
        {
            "can_view" => permissions.AnyAsync(permission => permission.CanView),
            "can_create" => permissions.AnyAsync(permission => permission.CanCreate),
                "can_edit" => permissions.AnyAsync(permission => permission.CanEdit),
            "can_delete" => permissions.AnyAsync(permission => permission.CanDelete),
            _ => Task.FromResult(false)
        };
    }

    public sealed class ProductInput
    {
        [Required, StringLength(50)] public string SkuCodigo { get; set; } = string.Empty;
        [Required, StringLength(160)] public string Nombre { get; set; } = string.Empty;
        [StringLength(2000)] public string? Descripcion { get; set; }
    }
}