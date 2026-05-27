namespace SuperBodega.Core.Entities;

public class DetalleCompra
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
}
// Entidad DetalleCompra
