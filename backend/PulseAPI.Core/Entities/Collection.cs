namespace PulseAPI.Core.Entities;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    
    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ApiCollection> ApiCollections { get; set; } = new List<ApiCollection>();
}



