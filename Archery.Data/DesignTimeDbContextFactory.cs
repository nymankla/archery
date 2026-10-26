using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Archery.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArcheryDbContext>
{
    public ArcheryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseNpgsql("Host=localhost;Database=archery_design;Username=postgres;Password=postgres")
            .Options;
        return new ArcheryDbContext(options);
    }
}
