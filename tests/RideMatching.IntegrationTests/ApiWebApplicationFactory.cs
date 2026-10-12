using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Interfaces;
using RideMatching.Infrastructure.Persistence;
using RideMatching.Infrastructure.Services;
using StackExchange.Redis;

namespace RideMatching.IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RideMatchingDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RideMatchingDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            // Remove existing Redis registration if present
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null) services.Remove(redisDescriptor);

            // Create open SQLite in-memory connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<RideMatchingDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Ensure in-memory location store is used for tests
            services.AddSingleton<IDriverLocationStore, RedisDriverLocationStore>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RideMatchingDbContext>();
            var locationStore = scope.ServiceProvider.GetRequiredService<IDriverLocationStore>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApiWebApplicationFactory>>();
            DbInitializer.InitializeAsync(db, locationStore, logger).GetAwaiter().GetResult();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
