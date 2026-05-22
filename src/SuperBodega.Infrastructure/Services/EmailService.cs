using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace SuperBodega.Infrastructure.Services;

public class EmailService
{
    private readonly string _email;
    private readonly string _password;

    public EmailService(IConfiguration configuration)
    {
        _email = configuration["Gmail:Email"] ?? "";
        _password = configuration["Gmail:Password"] ?? "";
    }
// Servicio de envio de emails mediante Gmail SMTP
    public async Task EnviarNotificacionPedidoAsync(string destinatario, string nombre, int ventaId, string estado, decimal total)
    {
        var asunto = estado switch
        {
            "Pendiente"  => $"Pedido #{ventaId} recibido - SuperBodega",
            "Despachado" => $"Pedido #{ventaId} despachado - SuperBodega",
            "Entregado"  => $"Pedido #{ventaId} entregado - SuperBodega",
            _            => $"Actualizacion de tu pedido #{ventaId} - SuperBodega"
        };

        var cuerpo = estado switch
        {
            "Pendiente"  => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido recibido correctamente.</p><p>Total: <strong>Q{total:F2}</strong></p><p>Gracias por comprar en SuperBodega!</p>",
            "Despachado" => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido despachado y esta en camino.</p><p>Pronto lo recibiras!</p>",
            "Entregado"  => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido entregado exitosamente.</p><p>Gracias por tu compra en SuperBodega!</p>",
            _            => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> tiene un nuevo estado: <strong>{estado}</strong></p>"
        };
// Notificaciones por estado del pedido
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress("SuperBodega", _email));
        mensaje.To.Add(new MailboxAddress(nombre, destinatario));
        mensaje.Subject = asunto;

        var body = new BodyBuilder { HtmlBody = cuerpo };
        mensaje.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_email, _password);
        await client.SendAsync(mensaje);
        await client.DisconnectAsync(true);
    }
}