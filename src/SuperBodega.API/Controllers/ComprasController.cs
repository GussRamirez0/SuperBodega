using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Core.Entities;
using SuperBodega.Infrastructure.Data;

namespace SuperBodega.API.Controllers;

public class DetalleCompraDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CompraDto
{
    public int ProveedorId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public List<DetalleCompraDto> Detalles { get; set; } = new();
}
// este controlador maneja las compras, permite crear una compra con sus detalles, actualizarla, eliminarla y obtenerla por id o por proveedor. Al crear una compra, se actualiza el stock de los productos involucrados.
[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ComprasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var compra = await _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);
        return compra == null ? NotFound() : Ok(compra);
    }

    [HttpGet("proveedor/{proveedorId}")]
    public async Task<IActionResult> GetByProveedor(int proveedorId) =>
        Ok(await _context.Compras
            .Include(c => c.Detalles)
            .Where(c => c.ProveedorId == proveedorId)
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompraDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var compra = new Compra
            {
                ProveedorId = dto.ProveedorId,
                NumeroFactura = dto.NumeroFactura,
                Notas = dto.Notas,
                Fecha = DateTime.UtcNow,
                Estado = "Recibida"
            };

            decimal total = 0;
            var detalles = new List<DetalleCompra>();

            foreach (var item in dto.Detalles)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null) return BadRequest($"Producto {item.ProductoId} no encontrado");
                var subtotal = item.Cantidad * item.PrecioUnitario;
                total += subtotal;
                producto.Stock += item.Cantidad;
                detalles.Add(new DetalleCompra
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = subtotal
                });
            }

            compra.Total = total;
            compra.Detalles = detalles;
            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CompraDto dto)
    {
        var compra = await _context.Compras.FindAsync(id);
        if (compra == null) return NotFound();
        compra.ProveedorId = dto.ProveedorId;
        compra.NumeroFactura = dto.NumeroFactura;
        compra.Notas = dto.Notas;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var compra = await _context.Compras.FindAsync(id);
        if (compra == null) return NotFound();
        _context.Compras.Remove(compra);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
