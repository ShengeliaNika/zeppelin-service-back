using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zeppelin.Infrastructure;

// Used only by `dotnet ef migrations add/update` so the design-time tooling
// doesn't need to spin up the full app host (auth, DI, etc.) just to build a
// DbContext for generating a migration.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ZeppelinDbContext>
{
    public ZeppelinDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ZEPPELIN_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=zeppelin;Username=zeppelin;Password=zeppelin_dev";

        var optionsBuilder = new DbContextOptionsBuilder<ZeppelinDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ZeppelinDbContext(optionsBuilder.Options);
    }
}
