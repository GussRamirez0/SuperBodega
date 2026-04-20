using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Core.Entities;
using SuperBodega.Infrastructure.Data;

namespace SuperBodega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VentasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == id);
        return venta == null ? NotFound() : Ok(venta);
    }

    [HttpGet("cliente/{clienteId}")]
    public async Task<IActionResult> GetByCliente(int clienteId) =>
        Ok(await _context.Ventas
            .Include(v => v.Detalles)
            .Where(v => v.ClienteId == clienteId)
            .ToListAsync());

    [HttpGet("periodo")]
    public async Task<IActionResult> GetByPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fin) =>
        Ok(await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(Venta venta)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            venta.Fecha = DateTime.UtcNow;
            venta.Estado = "Pendiente";
            venta.Total = venta.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

            foreach (var detalle in venta.Detalles)
            {
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                if (producto == null) return BadRequest($"Producto {detalle.ProductoId} no encontrado");
                if (producto.Stock < detalle.Cantidad)
                    return BadRequest($"Stock insuficiente para {producto.Nombre}. Stock actual: {producto.Stock}");
                producto.Stock -= detalle.Cantidad;
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = venta.Id }, venta);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var estadosValidos = new[] { "Pendiente", "Despachado", "Entregado", "Cancelado" };
        if (!estadosValidos.Contains(nuevoEstado))
            return BadRequest($"Estado no valido. Estados permitidos: {string.Join(", ", estadosValidos)}");

        var venta = await _context.Ventas.FindAsync(id);
        if (venta == null) return NotFound();

        venta.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return Ok(venta);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var venta = await _context.Ventas.FindAsync(id);
        if (venta == null) return NotFound();
        _context.Ventas.Remove(venta);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
