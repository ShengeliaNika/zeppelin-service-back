using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zeppelin.Entities.Identity;
using Zeppelin.Auditing;
using Zeppelin.Auth;
using Zeppelin.Services;
using Zeppelin.Storage;

namespace Zeppelin;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ZeppelinDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ZeppelinDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<SchedulingService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<InventoryReportingService>();
        services.AddScoped<RecallReminderService>();

        services.Configure<LocalDiskFileStorageOptions>(configuration.GetSection(LocalDiskFileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

        return services;
    }
}
