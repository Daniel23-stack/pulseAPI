namespace PulseAPI.Core.Entities;

public class AlertHistory
{
    public int Id { get; set; }
    public int AlertId { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    
    // Navigation properties
    public Alert Alert { get; set; } = null!;
}



