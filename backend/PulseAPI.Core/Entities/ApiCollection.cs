namespace PulseAPI.Core.Entities;

public class ApiCollection
{
    public int Id { get; set; }
    public int ApiId { get; set; }
    public int CollectionId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Api Api { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
}



