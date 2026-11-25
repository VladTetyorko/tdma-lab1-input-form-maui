using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// What a list of operations is narrowed to
public sealed record OperationFilter(
    OperationDirection? Direction = null,
    bool MineOnly = false,
    Guid? ProductId = null,
    DateTime? SinceUtc = null,
    int Count = 100);
