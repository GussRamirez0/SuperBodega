namespace SuperBodega.Core.Entities;

public class Venta
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string DireccionEntrega { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}
// Entidad Venta
