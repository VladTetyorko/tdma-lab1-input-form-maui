namespace Mobile_apps_L1_Model_view.Models;

public abstract class Entity
{
    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    // UUID v7 is time-ordered, so new rows land at the end of an index
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
