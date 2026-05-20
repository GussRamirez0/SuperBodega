using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Infrastructure.Data;

namespace SuperBodega.ECommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CatalogoController(ApplicationDbContext context)
    {
        _context = context;
    }
// Catalogo publico con paginacion
    [HttpGet]
    public async Task<IActionResult> GetProductos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 10,
        [FromQuery] int? categoriaId = null,
        [FromQuery] string? busqueda = null)
    {
        var query = _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.Stock > 0)
            .AsQueryable();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (!string.IsNullOrEmpty(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion.Contains(busqueda));

        var total = await query.CountAsync();
        var productos = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(p => new {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.ImagenUrl,
                Categoria = p.Categoria.Nombre
            })

            .ToListAsync();

        return Ok(new {
            Total = total,
            Pagina = pagina,
            Tamano = tamano,
            TotalPaginas = (int)Math.Ceiling((double)total / tamano),
            Productos = productos
        });
    }
// Filtros por categoria y busqueda
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProducto(int id)
    {
        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.Id == id)
            .Select(p => new {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.ImagenUrl,
                Categoria = p.Categoria.Nombre
            })
            .FirstOrDefaultAsync();

        return producto == null ? NotFound() : Ok(producto);
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias() =>
        Ok(await _context.Categorias.Where(c => c.Activo).ToListAsync());
}
