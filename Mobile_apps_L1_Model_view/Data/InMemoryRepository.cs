using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

// Shared storage for the in-memory repositories (replaced by SQLite in Lab 4)
public abstract class InMemoryRepository<T> where T : Entity
{
    private readonly List<T> _items = [];
    private readonly object _gate = new();

    protected Task<IReadOnlyList<T>> QueryAsync(Func<T, bool>? filter = null)
    {
        lock (_gate)
        {
            IReadOnlyList<T> result = (filter is null ? _items : _items.Where(filter)).ToList();
            return Task.FromResult(result);
        }
    }

    protected Task<T?> FindAsync(Func<T, bool> predicate)
    {
        lock (_gate)
        {
            return Task.FromResult(_items.FirstOrDefault(predicate));
        }
    }

    protected Task InsertAsync(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            _items.Add(item);
        }

        return Task.CompletedTask;
    }
}
