namespace PulseAPI.Core.Entities;

public class Alert
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AlertType Type { get; set; }
    public AlertCondition Condition { get; set; }
    public double Threshold { get; set; }
    public int? ApiId { get; set; } // Null for collection-level alerts
    public int? CollectionId { get; set; } // Null for API-level alerts
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    
    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public Api? Api { get; set; }
    public Collection? Collection { get; set; }
    public ICollection<AlertHistory> AlertHistories { get; set; } = new List<AlertHistory>();
}

public enum AlertType
{
    ErrorRate,
    Latency,
    Uptime,
    StatusCode
}

public enum AlertCondition
{
    GreaterThan,
    LessThan,
    Equals,
    NotEquals
}



