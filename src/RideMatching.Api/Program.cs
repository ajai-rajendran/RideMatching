using Microsoft.AspNetCore.SignalR;
using RideMatching.Api.Hubs;
using RideMatching.Api.Middleware;
using RideMatching.Api.Services;
using RideMatching.Application;
using RideMatching.Application.Interfaces;
using RideMatching.Infrastructure;
using RideMatching.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Ride Matching API",
        Version = "v1",
        Description = "Real-Time Ride Matching API with SignalR, Redis GEO & EF Core SQLite"
    });
});

// Real-Time SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Layer registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR notification dispatcher
builder.Services.AddScoped<IRideNotificationService, SignalRRideNotificationService>();

// CORS configuration for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ride Matching API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();
app.MapHub<RideHub>("/hubs/ride");

// Initialize Database and Seed default test data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RideMatchingDbContext>();
    var locationStore = scope.ServiceProvider.GetRequiredService<IDriverLocationStore>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await DbInitializer.InitializeAsync(dbContext, locationStore, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing/seeding the database.");
    }
}

app.Run();

public partial class Program { }
