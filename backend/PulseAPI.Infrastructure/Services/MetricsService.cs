using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using PulseAPI.Infrastructure.Data;

namespace PulseAPI.Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(ApplicationDbContext context, ILogger<MetricsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MetricsDto> GetMetricsAsync(string? environment = null, DateTime? startTime = null, DateTime? endTime = null)
    {
        var end = endTime ?? DateTime.UtcNow;
        var start = startTime ?? end.AddHours(-1); // Default to last hour

        var query = _context.HealthChecks
            .Include(h => h.Api)
            .Where(h => h.CheckedAt >= start && h.CheckedAt <= end);

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(h => h.Api.Environment == environment);
        }

        var healthChecks = await query.ToListAsync();

        // Calculate total traffic (TPS)
        var totalChecks = healthChecks.Count;
        var timeSpanSeconds = (end - start).TotalSeconds;
        var totalTrafficTps = timeSpanSeconds > 0 ? totalChecks / timeSpanSeconds : 0;

        // Calculate error rate
        var errorCount = healthChecks.Count(h => !h.IsSuccess);
        var errorRatePercent = totalChecks > 0 ? (errorCount / (double)totalChecks) * 100 : 0;

        // Calculate P99 latency
        var latencies = healthChecks.Where(h => h.IsSuccess).Select(h => h.LatencyMs).OrderBy(l => l).ToList();
        var latencyP99Ms = 0L;
        if (latencies.Any())
        {
            var p99Index = (int)Math.Ceiling(latencies.Count * 0.99) - 1;
            latencyP99Ms = latencies[Math.Max(0, p99Index)];
        }

        // Get API breakdown
        var apiBreakdown = await GetApiMetricsAsync(environment, start, end);

        // Get alert count
        var alertQuery = _context.AlertHistories.Where(a => !a.IsResolved);
        if (!string.IsNullOrEmpty(environment))
        {
            alertQuery = alertQuery.Where(a => 
                (a.Alert.Api != null && a.Alert.Api.Environment == environment) ||
                (a.Alert.Collection != null && a.Alert.Collection.ApiCollections.Any(ac => ac.Api.Environment == environment)));
        }
        var alertCount = await alertQuery.CountAsync();

        // Get alert breakdown
        var alertBreakdown = await _context.AlertHistories
            .Include(a => a.Alert)
            .Where(a => !a.IsResolved)
            .GroupBy(a => a.Alert.Name)
            .Select(g => new AlertBreakdownDto
            {
                AlertName = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return new MetricsDto
        {
            TotalTrafficTps = totalTrafficTps,
            ErrorRatePercent = errorRatePercent,
            TopProxyLatencyP99Ms = latencyP99Ms,
            AlertCount = alertCount,
            ApiBreakdown = apiBreakdown,
            AlertBreakdown = alertBreakdown
        };
    }

    public async Task<List<ApiMetricsDto>> GetApiMetricsAsync(string? environment = null, DateTime? startTime = null, DateTime? endTime = null)
    {
        var end = endTime ?? DateTime.UtcNow;
        var start = startTime ?? end.AddHours(-1);

        var query = _context.HealthChecks
            .Include(h => h.Api)
            .Where(h => h.CheckedAt >= start && h.CheckedAt <= end);

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(h => h.Api.Environment == environment);
        }

        var healthChecks = await query.ToListAsync();
        var timeSpanSeconds = (end - start).TotalSeconds;

        var apiGroups = healthChecks.GroupBy(h => new { h.ApiId, h.Api.Name, h.Api.Environment });

        var result = new List<ApiMetricsDto>();

        foreach (var group in apiGroups)
        {
            var checks = group.ToList();
            var trafficTps = timeSpanSeconds > 0 ? checks.Count / timeSpanSeconds : 0;
            var errorCount = checks.Count(h => !h.IsSuccess);
            var errorRatePercent = checks.Count > 0 ? (errorCount / (double)checks.Count) * 100 : 0;

            var latencies = checks.Where(h => h.IsSuccess).Select(h => h.LatencyMs).OrderBy(l => l).ToList();
            var latencyP99Ms = 0L;
            if (latencies.Any())
            {
                var p99Index = (int)Math.Ceiling(latencies.Count * 0.99) - 1;
                latencyP99Ms = latencies[Math.Max(0, p99Index)];
            }

            result.Add(new ApiMetricsDto
            {
                ApiId = group.Key.ApiId,
                ApiName = group.Key.Name,
                Environment = group.Key.Environment ?? "",
                TrafficTps = trafficTps,
                ErrorRatePercent = errorRatePercent,
                LatencyP99Ms = latencyP99Ms
            });
        }

        return result.OrderByDescending(a => a.TrafficTps).ToList();
    }
}

