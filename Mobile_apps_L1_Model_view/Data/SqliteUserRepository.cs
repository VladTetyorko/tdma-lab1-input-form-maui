using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteUserRepository(IDatabase database, SyncJournal journal)
    : SqliteRepository<User, UserRow>(database, journal), IUserRepository
{
    public Task<IReadOnlyList<User>> GetAllAsync() => QueryAsync();

    public Task<User?> GetByIdAsync(Guid id) => FindAsync("WHERE Id = ?", RowFormat.Text(id));

    public Task AddAsync(User user) => InsertAsync(user);

    protected override ValueTask<UserRow> ToRowAsync(User user) => ValueTask.FromResult(new UserRow
    {
        Id = RowFormat.Text(user.Id),
        FullName = user.FullName,
        Role = RowFormat.Text(user.Role),
        CreatedAt = RowFormat.Text(user.CreatedAt),
        UpdatedAt = RowFormat.Text(user.UpdatedAt)
    });

    protected override ValueTask<User> ToEntityAsync(UserRow row) => ValueTask.FromResult(new User
    {
        Id = RowFormat.ToGuid(row.Id),
        FullName = row.FullName,
        Role = RowFormat.ToEnum<Role>(row.Role),
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    });
}
