using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Infrastructure.Contexts;
using GameStore.Infrastructure.Identity; // <-- NEW
using GameStore.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;     // <-- NEW
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Temporarily comment out the configuration read
        // var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Hardcode it just for this migration step
        var connectionString = "Server=127.0.0.1;Database=GameStore;Uid=root;Pwd=kerberos1;";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, serverVersion)
                   // Best practice for performance: Trace EF Core queries in Aspire
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors());

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        // --- NEW: Identity Configuration ---
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // --- NEW: Hybrid Cache ---
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB max cache size per entry
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
        // -------------------------

        services.AddScoped<IIdentityService, IdentityService>();
        // -----------------------------------

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}