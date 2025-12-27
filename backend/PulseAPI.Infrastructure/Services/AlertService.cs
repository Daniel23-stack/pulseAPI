using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using PulseAPI.Infrastructure.Data;

namespace PulseAPI.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlertService> _logger;

    public AlertService(ApplicationDbContext context, ILogger<AlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EvaluateAlertsAsync(int apiId, HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.Alerts
            .Where(a => a.IsActive && a.ApiId == apiId)
            .ToListAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            await EvaluateAlertAsync(alert, apiId, healthCheck, cancellationToken);
        }
    }

    public async Task EvaluateCollectionAlertsAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.Alerts
            .Include(a => a.Collection)
            .ThenInclude(c => c!.ApiCollections)
            .ThenInclude(ac => ac.Api)
            .ThenInclude(a => a.HealthChecks)
            .Where(a => a.IsActive && a.CollectionId == collectionId)
            .ToListAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            if (alert.Collection == null) continue;

            var apiIds = alert.Collection.ApiCollections.Select(ac => ac.ApiId).ToList();
            var recentChecks = await _context.HealthChecks
                .Where(h => apiIds.Contains(h.ApiId) && h.CheckedAt >= DateTime.UtcNow.AddHours(-1))
                .ToListAsync(cancellationToken);

            await EvaluateCollectionAlertAsync(alert, recentChecks, cancellationToken);
        }
    }

    private async Task EvaluateAlertAsync(Alert alert, int apiId, HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        bool shouldFire = false;
        string message = string.Empty;

        switch (alert.Type)
        {
            case AlertType.StatusCode:
                shouldFire = EvaluateCondition(healthCheck.StatusCode, alert.Threshold, alert.Condition);
                if (shouldFire)
                {
                    message = $"API {apiId} returned status code {healthCheck.StatusCode}";
                }
                break;

            case AlertType.Latency:
                shouldFire = EvaluateCondition(healthCheck.LatencyMs, alert.Threshold, alert.Condition);
                if (shouldFire)
                {
                    message = $"API {apiId} latency {healthCheck.LatencyMs}ms exceeds threshold";
                }
                break;

            case AlertType.ErrorRate:
                // For error rate, we need to check recent checks
                var recentChecks = await _context.HealthChecks
                    .Where(h => h.ApiId == apiId && h.CheckedAt >= DateTime.UtcNow.AddHours(-1))
                    .ToListAsync(cancellationToken);
                
                if (recentChecks.Any())
                {
                    var errorRate = (recentChecks.Count(h => !h.IsSuccess) / (double)recentChecks.Count) * 100;
                    shouldFire = EvaluateCondition(errorRate, alert.Threshold, alert.Condition);
                    if (shouldFire)
                    {
                        message = $"API {apiId} error rate {errorRate:F2}% exceeds threshold";
                    }
                }
                break;
        }

        if (shouldFire)
        {
            // Check if alert was already fired recently (within last 5 minutes)
            var recentAlert = await _context.AlertHistories
                .Where(h => h.AlertId == alert.Id && !h.IsResolved && h.FiredAt >= DateTime.UtcNow.AddMinutes(-5))
                .FirstOrDefaultAsync(cancellationToken);

            if (recentAlert == null)
            {
                var alertHistory = new AlertHistory
                {
                    AlertId = alert.Id,
                    FiredAt = DateTime.UtcNow,
                    Message = message
                };

                await _context.AlertHistories.AddAsync(alertHistory, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Alert fired: {AlertName} - {Message}", alert.Name, message);
            }
        }
    }

    private async Task EvaluateCollectionAlertAsync(Alert alert, List<HealthCheck> healthChecks, CancellationToken cancellationToken)
    {
        if (!healthChecks.Any()) return;

        bool shouldFire = false;
        string message = string.Empty;

        switch (alert.Type)
        {
            case AlertType.ErrorRate:
                var errorRate = (healthChecks.Count(h => !h.IsSuccess) / (double)healthChecks.Count) * 100;
                shouldFire = EvaluateCondition(errorRate, alert.Threshold, alert.Condition);
                if (shouldFire)
                {
                    message = $"Collection {alert.CollectionId} error rate {errorRate:F2}% exceeds threshold";
                }
                break;

            case AlertType.Latency:
                var avgLatency = healthChecks.Where(h => h.IsSuccess).Select(h => h.LatencyMs).DefaultIfEmpty(0).Average();
                shouldFire = EvaluateCondition(avgLatency, alert.Threshold, alert.Condition);
                if (shouldFire)
                {
                    message = $"Collection {alert.CollectionId} average latency {avgLatency:F2}ms exceeds threshold";
                }
                break;
        }

        if (shouldFire)
        {
            var recentAlert = await _context.AlertHistories
                .Where(h => h.AlertId == alert.Id && !h.IsResolved && h.FiredAt >= DateTime.UtcNow.AddMinutes(-5))
                .FirstOrDefaultAsync(cancellationToken);

            if (recentAlert == null)
            {
                var alertHistory = new AlertHistory
                {
                    AlertId = alert.Id,
                    FiredAt = DateTime.UtcNow,
                    Message = message
                };

                await _context.AlertHistories.AddAsync(alertHistory, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Collection alert fired: {AlertName} - {Message}", alert.Name, message);
            }
        }
    }

    private bool EvaluateCondition(double value, double threshold, AlertCondition condition)
    {
        return condition switch
        {
            AlertCondition.GreaterThan => value > threshold,
            AlertCondition.LessThan => value < threshold,
            AlertCondition.Equals => Math.Abs(value - threshold) < 0.01,
            AlertCondition.NotEquals => Math.Abs(value - threshold) >= 0.01,
            _ => false
        };
    }
}

