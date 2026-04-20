using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Core.Entities;
using SuperBodega.Infrastructure.Data;

namespace SuperBodega.API.Controllers;

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
    public async Task<IActionResult> Create(Compra compra)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            compra.Fecha = DateTime.UtcNow;
            compra.Total = compra.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

            foreach (var detalle in compra.Detalles)
            {
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                if (producto == null) return BadRequest($"Producto {detalle.ProductoId} no encontrado");
                producto.Stock += detalle.Cantidad;
            }

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
    public async Task<IActionResult> Update(int id, Compra compra)
    {
        if (id != compra.Id) return BadRequest();
        _context.Entry(compra).State = EntityState.Modified;
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
