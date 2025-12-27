using PulseAPI.Core.Entities;

namespace PulseAPI.Core.Interfaces;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckApiAsync(Api api, CancellationToken cancellationToken = default);
}

public class HealthCheckResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
}



