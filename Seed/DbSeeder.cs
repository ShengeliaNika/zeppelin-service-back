using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zeppelin.Common;
using Zeppelin.Entities.Identity;
using Zeppelin.Entities.Scheduling;
using Zeppelin;

namespace Zeppelin.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        await SeedBootstrapAdminAsync(userManager, configuration, logger);

        var db = services.GetRequiredService<ZeppelinDbContext>();
        await SeedSchedulingDefaultsAsync(db);
    }

    private static async Task SeedBootstrapAdminAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed:AdminEmail / Seed:AdminPassword not configured - skipping bootstrap admin creation.");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create bootstrap admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        logger.LogInformation("Seeded bootstrap admin user {Email}", adminEmail);
    }

    private static async Task SeedSchedulingDefaultsAsync(ZeppelinDbContext db)
    {
        if (!db.AppointmentTypes.Any())
        {
            db.AppointmentTypes.AddRange(
                new AppointmentType { Id = Guid.NewGuid(), Name = "Checkup", DefaultDurationMinutes = 30, Color = "#4A90D9", RecallIntervalMonths = 6 },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Cleaning", DefaultDurationMinutes = 45, Color = "#50C878", RecallIntervalMonths = 6 },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Filling", DefaultDurationMinutes = 60, Color = "#E8A33D" },
                new AppointmentType { Id = Guid.NewGuid(), Name = "Extraction", DefaultDurationMinutes = 45, Color = "#D9534F" });
        }

        if (!db.Chairs.Any())
        {
            db.Chairs.AddRange(
                new Chair { Id = Guid.NewGuid(), Name = "Chair 1" },
                new Chair { Id = Guid.NewGuid(), Name = "Chair 2" });
        }

        await db.SaveChangesAsync();
    }
}
