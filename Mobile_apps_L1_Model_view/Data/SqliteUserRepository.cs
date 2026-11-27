using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteUserRepository(IDatabase database, SyncJournal journal, ILogger<SqliteUserRepository> logger)
    : SqliteRepository<User, UserRow>(database, journal, logger), IUserRepository
{
    public Task<IReadOnlyList<User>> GetAllAsync() => QueryAsync();

    public Task<User?> GetByIdAsync(Guid id) => FindAsync("WHERE Id = ?", RowFormat.Text(id));

    // Login is unique ignoring case; null when there is no such user
    public Task<User?> GetByLoginAsync(string login) => FindAsync("WHERE Login = ? COLLATE NOCASE", login);

    public Task AddAsync(User user) => InsertAsync(user);

    protected override ValueTask<UserRow> ToRowAsync(User user) => ValueTask.FromResult(new UserRow
    {
        Id = RowFormat.Text(user.Id),
        FullName = user.FullName,
        Role = RowFormat.Text(user.Role),
        Login = NullIfEmpty(user.Login),
        PasswordHash = NullIfEmpty(user.PasswordHash),
        CreatedAt = RowFormat.Text(user.CreatedAt),
        UpdatedAt = RowFormat.Text(user.UpdatedAt)
    });

    protected override ValueTask<User> ToEntityAsync(UserRow row) => ValueTask.FromResult(new User
    {
        Id = RowFormat.ToGuid(row.Id),
        FullName = row.FullName,
        Role = RowFormat.ToEnum<Role>(row.Role),
        Login = row.Login ?? string.Empty,
        PasswordHash = row.PasswordHash ?? string.Empty,
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    });

    // Two users without a login must not collide in the unique index, so "no login" is stored as NULL
    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
