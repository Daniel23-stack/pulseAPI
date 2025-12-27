namespace PulseAPI.Core.Entities;

public class Api
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET"; // GET, POST, PUT, DELETE, etc.
    public string? Headers { get; set; } // JSON string for headers
    public string? Body { get; set; } // Request body for POST/PUT
    public int CheckIntervalSeconds { get; set; } = 60; // Default 60 seconds
    public int TimeoutSeconds { get; set; } = 30; // Default 30 seconds
    public bool IsActive { get; set; } = true;
    public string? Environment { get; set; } // prod, staging, dev, etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    
    // Navigation properties
    public User? CreatedByUser { get; set; }
    public ICollection<HealthCheck> HealthChecks { get; set; } = new List<HealthCheck>();
    public ICollection<ApiCollection> ApiCollections { get; set; } = new List<ApiCollection>();
}

