using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Interfaces;
using RideMatching.Domain.Repositories;
using RideMatching.Infrastructure.BackgroundServices;
using RideMatching.Infrastructure.Persistence;
using RideMatching.Infrastructure.Persistence.Repositories;
using RideMatching.Infrastructure.Services;
using StackExchange.Redis;

namespace RideMatching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbPath = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ridematching.db";

        services.AddDbContext<RideMatchingDbContext>(options =>
        {
            options.UseSqlite(dbPath);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RideMatchingDbContext>());
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IRiderRepository, RiderRepository>();
        services.AddScoped<IRideRepository, RideRepository>();

        // Redis Connection (optional / resilient)
        var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        try
        {
            var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { redisConn },
                AbortOnConnectFail = false,
                ConnectTimeout = 1500,
                SyncTimeout = 1500
            });
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        }
        catch
        {
            // Will fallback to in-memory location store gracefully
        }

        services.AddSingleton<IDriverLocationStore, RedisDriverLocationStore>();
        services.AddSingleton<IRideMatchingChannel, RideMatchingChannel>();
        services.AddHostedService<RideMatchingBackgroundWorker>();

        return services;
    }
}
