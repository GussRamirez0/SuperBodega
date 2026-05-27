using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Infrastructure.Data;

namespace SuperBodega.API.Controllers;


// Este controlador maneja los reportes de ventas, permitiendo obtener resúmenes de ventas por periodo,
// por producto, por cliente y por proveedor.
// Estos reportes son útiles para el análisis de negocio y la toma de decisiones.
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("ventas/periodo")]
    public async Task<IActionResult> VentasPorPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fin)
    {
        var ventas = await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
            .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
            .ToListAsync();

        return Ok(new {
            Periodo = new { Inicio = inicio, Fin = fin },
            TotalVentas = ventas.Count,
            MontoTotal = ventas.Sum(v => v.Total),
            Ventas = ventas
        });
    }

    [HttpGet("ventas/producto")]
    public async Task<IActionResult> VentasPorProducto()
    {
        var resultado = await _context.DetallesVenta
            .Include(d => d.Producto)
            .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
            .Select(g => new {
                ProductoId = g.Key.ProductoId,
                Producto = g.Key.Nombre,
                CantidadVendida = g.Sum(d => d.Cantidad),
                MontoTotal = g.Sum(d => d.Subtotal)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .ToListAsync();

        return Ok(resultado);
    }

    [HttpGet("ventas/cliente")]
    public async Task<IActionResult> VentasPorCliente()
    {
        var resultado = await _context.Ventas
            .Include(v => v.Cliente)
            .GroupBy(v => new { v.ClienteId, v.Cliente.Nombre, v.Cliente.Apellido })
            .Select(g => new {
                ClienteId = g.Key.ClienteId,
                Cliente = g.Key.Nombre + " " + g.Key.Apellido,
                TotalOrdenes = g.Count(),
                MontoTotal = g.Sum(v => v.Total)
            })
            .OrderByDescending(x => x.MontoTotal)
            .ToListAsync();

        return Ok(resultado);
    }

    [HttpGet("ventas/proveedor")]
    public async Task<IActionResult> VentasPorProveedor()
    {
        var resultado = await _context.Compras
            .Include(c => c.Proveedor)
            .GroupBy(c => new { c.ProveedorId, c.Proveedor.Nombre })
            .Select(g => new {
                ProveedorId = g.Key.ProveedorId,
                Proveedor = g.Key.Nombre,
                TotalCompras = g.Count(),
                MontoTotal = g.Sum(c => c.Total)
            })
            .OrderByDescending(x => x.MontoTotal)
            .ToListAsync();

        return Ok(resultado);
    }
}
