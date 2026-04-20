using Resend;

namespace SuperBodega.Infrastructure.Services;

public class EmailService
{
    private readonly IResend _resend;

    public EmailService(IResend resend)
    {
        _resend = resend;
    }

    public async Task EnviarNotificacionPedidoAsync(string destinatario, string nombre, int ventaId, string estado, decimal total)
    {
        var asunto = estado switch
        {
            "Pendiente"  => $"Pedido #{ventaId} recibido",
            "Despachado" => $"Pedido #{ventaId} despachado",
            "Entregado"  => $"Pedido #{ventaId} entregado",
            _            => $"Actualizacion de tu pedido #{ventaId}"
        };

        var cuerpo = estado switch
        {
            "Pendiente"  => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido recibido correctamente.</p><p>Total: <strong>Q{total:F2}</strong></p>",
            "Despachado" => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido despachado y esta en camino.</p>",
            "Entregado"  => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> ha sido entregado exitosamente.</p><p>Gracias por tu compra!</p>",
            _            => $"<h2>Hola {nombre}</h2><p>Tu pedido <strong>#{ventaId}</strong> tiene un nuevo estado: {estado}</p>"
        };

        var mensaje = new EmailMessage
        {
            From = "SuperBodega <onboarding@resend.dev>",
            To = { destinatario },
            Subject = asunto,
            HtmlBody = cuerpo
        };

        await _resend.EmailSendAsync(mensaje);
    }
}
