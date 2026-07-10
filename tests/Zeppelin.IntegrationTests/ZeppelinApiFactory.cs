using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Zeppelin;

namespace Zeppelin.IntegrationTests;

public class ZeppelinApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("zeppelin_test")
        .WithUsername("zeppelin")
        .WithPassword("zeppelin_test")
        .Build();

    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "IntegrationTest123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "ZeppelinApiTests",
                ["Jwt:Audience"] = "ZeppelinClientTests",
                ["Jwt:SigningKey"] = Convert.ToBase64String("integration-test-signing-key-integration-test-signing-key"u8.ToArray()),
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations via a standalone connection *before* touching
        // Services/CreateClient - either of those boots the app host, which
        // runs Program.cs's own startup seeding and would fail against a
        // schema-less database.
        var optionsBuilder = new DbContextOptionsBuilder<ZeppelinDbContext>();
        optionsBuilder.UseNpgsql(_postgres.GetConnectionString());
        await using var migrationContext = new ZeppelinDbContext(optionsBuilder.Options);
        await migrationContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
    }
}
