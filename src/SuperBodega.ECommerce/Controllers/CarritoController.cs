using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperBodega.Core.Entities;
using SuperBodega.Infrastructure.Data;
using SuperBodega.Infrastructure.Messaging;

namespace SuperBodega.ECommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarritoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RabbitMQService _rabbitMQ;

    public CarritoController(ApplicationDbContext context, RabbitMQService rabbitMQ)
    {
        _context = context;
        _rabbitMQ = rabbitMQ;
    }

    [HttpPost("realizar-compra")]
    public async Task<IActionResult> RealizarCompra([FromBody] CompraRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        // Proceso de compra completo del carrito
        {
            var cliente = await _context.Clientes.FindAsync(request.ClienteId);
            if (cliente == null) return BadRequest("Cliente no encontrado");

            var venta = new Venta
            {
                ClienteId = request.ClienteId,
                Fecha = DateTime.UtcNow,
                Estado = "Pendiente",
                DireccionEntrega = request.DireccionEntrega,
                NumeroOrden = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Notas = request.Notas
            };

            decimal total = 0;
            var detalles = new List<DetalleVenta>();

            foreach (var item in request.Items)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null) return BadRequest($"Producto {item.ProductoId} no encontrado");
                if (producto.Stock < item.Cantidad)
                    return BadRequest($"Stock insuficiente para {producto.Nombre}. Stock: {producto.Stock}");

                var subtotal = item.Cantidad * producto.Precio;
                total += subtotal;
                producto.Stock -= item.Cantidad;

                detalles.Add(new DetalleVenta
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = subtotal
                });
            }
// Validacion de stock antes de procesar compra
            venta.Total = total;
            venta.Detalles = detalles;

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _rabbitMQ.PublicarNotificacion(new NotificacionPedido
            {
                VentaId = venta.Id,
                ClienteEmail = cliente.Email,
                ClienteNombre = $"{cliente.Nombre} {cliente.Apellido}",
                Estado = "Pendiente",
                Total = total
            });

            return Ok(new {
                Mensaje = "Compra realizada exitosamente",
                NumeroOrden = venta.NumeroOrden,
                VentaId = venta.Id,
                Total = total,
                Estado = venta.Estado
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }
}

public class CompraRequest
{
    public int ClienteId { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public List<ItemCarrito> Items { get; set; } = new();
}

public class ItemCarrito
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
