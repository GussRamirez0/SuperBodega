using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SuperBodega.Core.Entities;
using SuperBodega.Infrastructure.Data;
using SuperBodega.Infrastructure.Messaging;
using System.Text;
using System.Text.Json;

namespace SuperBodega.Infrastructure.Services;
// Worker encargado de procesar ventas asincronas
public class VentaAsyncWorker : BackgroundService
{
    private readonly ILogger<VentaAsyncWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "ventas-async";

    public VentaAsyncWorker(ILogger<VentaAsyncWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost", UserName = "admin", Password = "admin123" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _logger.LogInformation("VentaAsyncWorker iniciado");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo conectar a RabbitMQ: {msg}", ex.Message);
        }
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return Task.CompletedTask;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var mensaje = Encoding.UTF8.GetString(body);

            try
            {
                var request = JsonSerializer.Deserialize<VentaAsyncRequest>(mensaje);
                if (request == null) return;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var rabbitMQ = scope.ServiceProvider.GetRequiredService<RabbitMQService>();

                using var transaction = await context.Database.BeginTransactionAsync();

                var cliente = await context.Clientes.FindAsync(request.ClienteId);
                if (cliente == null) { _channel.BasicNack(ea.DeliveryTag, false, false); return; }

                var venta = new Venta
                {
                    ClienteId = request.ClienteId,
                    DireccionEntrega = request.DireccionEntrega,
                    Notas = request.Notas,
                    Fecha = DateTime.UtcNow,
                    Estado = "Pendiente",
                    NumeroOrden = $"ASYNC-{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                decimal total = 0;
                var detalles = new List<DetalleVenta>();

                foreach (var item in request.Items)
                {
                    var producto = await context.Productos.FindAsync(item.ProductoId);
                    if (producto == null || producto.Stock < item.Cantidad) continue;
                    var subtotal = item.Cantidad * producto.Precio;
                    total += subtotal;
                    producto.Stock -= item.Cantidad;
                    detalles.Add(new DetalleVenta { ProductoId = item.ProductoId, Cantidad = item.Cantidad, PrecioUnitario = producto.Precio, Subtotal = subtotal });
                }

                venta.Total = total;
                venta.Detalles = detalles;
                context.Ventas.Add(venta);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                rabbitMQ.PublicarNotificacion(new NotificacionPedido
                {
                    VentaId = venta.Id,
                    ClienteEmail = cliente.Email,
                    ClienteNombre = $"{cliente.Nombre} {cliente.Apellido}",
                    Estado = "Pendiente",
                    Total = total
                });

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Venta async procesada: {orden}", venta.NumeroOrden);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error procesando venta async: {error}", ex.Message);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class VentaAsyncRequest
{
    public int ClienteId { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public List<DetalleVentaItemAsync> Items { get; set; } = new();
}

public class DetalleVentaItemAsync
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
