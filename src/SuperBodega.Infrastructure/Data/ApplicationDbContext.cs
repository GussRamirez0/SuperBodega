using Microsoft.EntityFrameworkCore;
using SuperBodega.Core.Entities;

namespace SuperBodega.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Producto>(e => {
            e.Property(p => p.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DetalleCompra>(e => {
            e.Property(p => p.PrecioUnitario).HasPrecision(18, 2);
            e.Property(p => p.Subtotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DetalleVenta>(e => {
            e.Property(p => p.PrecioUnitario).HasPrecision(18, 2);
            e.Property(p => p.Subtotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Compra>(e => {
            e.Property(p => p.Total).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Venta>(e => {
            e.Property(p => p.Total).HasPrecision(18, 2);
        });
    }
}
// DbContext principal
// DbContext principal
