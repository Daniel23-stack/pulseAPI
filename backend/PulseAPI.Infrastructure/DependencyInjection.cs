using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using PulseAPI.Infrastructure.Data;
using PulseAPI.Infrastructure.Repositories;
using PulseAPI.Infrastructure.Services;

namespace PulseAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Services
        services.AddHttpClient<IHealthCheckService, HealthCheckService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IMetricsService, MetricsService>();

        return services;
    }
}

