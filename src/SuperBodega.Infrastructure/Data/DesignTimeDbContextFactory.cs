using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SuperBodega.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1434;Database=SuperBodegaDB;User Id=sa;Password=SuperBodega2024!;TrustServerCertificate=True;");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
// Factory design time
// Factory design time
