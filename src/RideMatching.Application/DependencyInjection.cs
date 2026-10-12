using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RideMatching.Application.Interfaces;
using RideMatching.Application.Services;
using RideMatching.Application.Validators;

namespace RideMatching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IRiderService, RiderService>();
        services.AddScoped<IRideService, RideService>();

        services.AddValidatorsFromAssemblyContaining<RegisterDriverRequestValidator>();

        return services;
    }
}
