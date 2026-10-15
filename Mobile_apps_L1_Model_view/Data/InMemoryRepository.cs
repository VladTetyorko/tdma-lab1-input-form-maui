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

    protected Task ReplaceAsync(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            var index = _items.FindIndex(i => i.Id == item.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"{typeof(T).Name} {item.Id} does not exist.");
            }

            _items[index] = item;
        }

        return Task.CompletedTask;
    }

    // Removing a row that is already gone is not an error
    protected Task RemoveAsync(Guid id)
    {
        lock (_gate)
        {
            _items.RemoveAll(i => i.Id == id);
        }

        return Task.CompletedTask;
    }
}
