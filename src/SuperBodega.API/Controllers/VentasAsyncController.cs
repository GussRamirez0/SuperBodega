using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using SuperBodega.Infrastructure.Services;
using System.Text;
using System.Text.Json;

namespace SuperBodega.API.Controllers;
// Endpoint asincrono de ventas
[ApiController]
[Route("api/async/[controller]")]
public class VentasAsyncController : ControllerBase
{
    [HttpPost]
    public IActionResult CrearVentaAsync([FromBody] VentaAsyncRequest request)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "admin123"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "ventas-async", durable: true, exclusive: false, autoDelete: false);

            var mensaje = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(mensaje);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(exchange: "", routingKey: "ventas-async", basicProperties: properties, body: body);

            return Accepted(new {
                Mensaje = "Venta recibida y en proceso",
                Estado = "En cola",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
