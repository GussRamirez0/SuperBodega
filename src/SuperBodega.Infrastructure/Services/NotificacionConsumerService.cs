using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SuperBodega.Infrastructure.Messaging;
using System.Text;
using System.Text.Json;

namespace SuperBodega.Infrastructure.Services;
// Servicio consumidor de mensajes RabbitMQ
public class NotificacionConsumerService : BackgroundService
{
    private readonly ILogger<NotificacionConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "notificaciones-pedidos";

    public NotificacionConsumerService(ILogger<NotificacionConsumerService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "admin123"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _logger.LogInformation("Consumidor de notificaciones iniciado");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo conectar a RabbitMQ: {mensaje}", ex.Message);
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
            var notificacion = JsonSerializer.Deserialize<NotificacionPedido>(mensaje);

            if (notificacion != null)
            {
                try
                {
                    // Manejo de scopes para procesamiento por mensaje
                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                    await emailService.EnviarNotificacionPedidoAsync(
                        notificacion.ClienteEmail,
                        notificacion.ClienteNombre,
                        notificacion.VentaId,
                        notificacion.Estado,
                        notificacion.Total);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Notificacion enviada a {email} para pedido #{id}", notificacion.ClienteEmail, notificacion.VentaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error enviando notificacion: {error}", ex.Message);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
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
