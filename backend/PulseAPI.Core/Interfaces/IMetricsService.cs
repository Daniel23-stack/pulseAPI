namespace PulseAPI.Core.Interfaces;

public interface IMetricsService
{
    Task<MetricsDto> GetMetricsAsync(string? environment = null, DateTime? startTime = null, DateTime? endTime = null);
    Task<List<ApiMetricsDto>> GetApiMetricsAsync(string? environment = null, DateTime? startTime = null, DateTime? endTime = null);
}

public class MetricsDto
{
    public double TotalTrafficTps { get; set; }
    public double ErrorRatePercent { get; set; }
    public long TopProxyLatencyP99Ms { get; set; }
    public int AlertCount { get; set; }
    public List<ApiMetricsDto> ApiBreakdown { get; set; } = new();
    public List<AlertBreakdownDto> AlertBreakdown { get; set; } = new();
}

public class ApiMetricsDto
{
    public int ApiId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public double TrafficTps { get; set; }
    public double ErrorRatePercent { get; set; }
    public long LatencyP99Ms { get; set; }
}

public class AlertBreakdownDto
{
    public string AlertName { get; set; } = string.Empty;
    public int Count { get; set; }
}



