using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace SuperBodega.Infrastructure.Messaging;
// Servicio encargado de administrar conexion RabbitMQ
public class RabbitMQService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "notificaciones-pedidos";

    public RabbitMQService()
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
    }

    public void PublicarNotificacion(NotificacionPedido notificacion)
    {
        var mensaje = JsonSerializer.Serialize(notificacion);
        var body = Encoding.UTF8.GetBytes(mensaje);
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        _channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: properties, body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
// Publica mensajes en la cola de notificaciones
public class NotificacionPedido
{
    public int VentaId { get; set; }
    public string ClienteEmail { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
