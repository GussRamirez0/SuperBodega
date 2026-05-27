namespace SuperBodega.Core.Entities;

public class Compra
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Recibida";
    public string Notas { get; set; } = string.Empty;

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
}
// Entidad Compra
