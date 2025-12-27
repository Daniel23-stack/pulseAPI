using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using PulseAPI.Infrastructure.Data;

namespace PulseAPI.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while performing health checks");
            }

            // Wait 10 seconds before next iteration
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("Worker service stopped");
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        // Get all active APIs
        var activeApis = await dbContext.Apis
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var api in activeApis)
        {
            try
            {
                // Check if it's time to check this API
                var lastCheck = await dbContext.HealthChecks
                    .Where(h => h.ApiId == api.Id)
                    .OrderByDescending(h => h.CheckedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var timeSinceLastCheck = lastCheck == null 
                    ? TimeSpan.MaxValue 
                    : now - lastCheck.CheckedAt;

                var checkInterval = TimeSpan.FromSeconds(api.CheckIntervalSeconds);

                if (timeSinceLastCheck >= checkInterval)
                {
                    _logger.LogInformation("Checking API: {ApiName} ({ApiId})", api.Name, api.Id);

                    // Perform health check
                    var result = await healthCheckService.CheckApiAsync(api, cancellationToken);

                    // Save health check result
                    var healthCheck = new HealthCheck
                    {
                        ApiId = api.Id,
                        CheckedAt = now,
                        IsSuccess = result.IsSuccess,
                        StatusCode = result.StatusCode,
                        LatencyMs = result.LatencyMs,
                        ResponseBody = result.ResponseBody,
                        ErrorMessage = result.ErrorMessage
                    };

                    dbContext.HealthChecks.Add(healthCheck);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // Evaluate alerts
                    await alertService.EvaluateAlertsAsync(api.Id, healthCheck, cancellationToken);

                    _logger.LogInformation(
                        "Health check completed for API {ApiName}: Success={Success}, StatusCode={StatusCode}, Latency={Latency}ms",
                        api.Name, result.IsSuccess, result.StatusCode, result.LatencyMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API {ApiName} ({ApiId})", api.Name, api.Id);
            }
        }

        // Evaluate collection-level alerts
        var collections = await dbContext.Collections
            .Include(c => c.ApiCollections)
            .ThenInclude(ac => ac.Api)
            .Where(c => c.ApiCollections.Any())
            .ToListAsync(cancellationToken);

        foreach (var collection in collections)
        {
            try
            {
                await alertService.EvaluateCollectionAlertsAsync(collection.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alerts for collection {CollectionName} ({CollectionId})", 
                    collection.Name, collection.Id);
            }
        }
    }
}
