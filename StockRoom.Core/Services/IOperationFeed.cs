using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Reads operations for the lists of the app. It is where "the Driver sees only his own" is enforced: whatever the
// caller asks for, such a role gets only what it recorded itself
public interface IOperationFeed
{
    // Newest first
    Task<IReadOnlyList<OperationEntry>> GetAsync(OperationFilter filter);
}
