namespace Mobile_apps_L1_Model_view.Models;

public abstract class OwnedEntity : Entity
{
    public Guid CreatedByUserId { get; init; }
}
