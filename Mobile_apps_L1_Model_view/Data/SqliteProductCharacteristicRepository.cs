using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteProductCharacteristicRepository(IDatabase database, SyncJournal journal)
    : SqliteRepository<ProductCharacteristic, ProductCharacteristicRow>(database, journal),
        IProductCharacteristicRepository
{
    public Task<IReadOnlyList<ProductCharacteristic>> GetAllAsync() => QueryAsync();

    public Task<IReadOnlyList<ProductCharacteristic>> GetByProductAsync(Guid productId) =>
        QueryAsync("WHERE ProductId = ?", RowFormat.Text(productId));

    public Task AddAsync(ProductCharacteristic value) => InsertAsync(value);

    Task IProductCharacteristicRepository.UpdateAsync(ProductCharacteristic value) => UpdateAsync(value);

    Task IProductCharacteristicRepository.DeleteAsync(Guid id) => DeleteAsync(id);

    protected override ValueTask<ProductCharacteristicRow> ToRowAsync(ProductCharacteristic value) =>
        ValueTask.FromResult(new ProductCharacteristicRow
        {
            Id = RowFormat.Text(value.Id),
            ProductId = RowFormat.Text(value.ProductId),
            CharacteristicId = RowFormat.Text(value.CharacteristicId),
            Value = value.Value,
            CreatedByUserId = RowFormat.Text(value.CreatedByUserId),
            CreatedAt = RowFormat.Text(value.CreatedAt),
            UpdatedAt = RowFormat.Text(value.UpdatedAt)
        });

    protected override ValueTask<ProductCharacteristic> ToEntityAsync(ProductCharacteristicRow row) =>
        ValueTask.FromResult(new ProductCharacteristic
        {
            Id = RowFormat.ToGuid(row.Id),
            ProductId = RowFormat.ToGuid(row.ProductId),
            CharacteristicId = RowFormat.ToGuid(row.CharacteristicId),
            Value = row.Value,
            CreatedByUserId = RowFormat.ToGuid(row.CreatedByUserId),
            CreatedAt = RowFormat.ToTime(row.CreatedAt),
            UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
        });
}
