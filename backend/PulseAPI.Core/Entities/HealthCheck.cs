namespace PulseAPI.Core.Entities;

public class HealthCheck
{
    public int Id { get; set; }
    public int ApiId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public Api Api { get; set; } = null!;
}



