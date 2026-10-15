using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteCharacteristicRepository(IDatabase database, SyncJournal journal)
    : SqliteRepository<Characteristic, CharacteristicRow>(database, journal), ICharacteristicRepository
{
    public Task<IReadOnlyList<Characteristic>> GetAllAsync() => QueryAsync();

    public Task AddAsync(Characteristic characteristic) => InsertAsync(characteristic);

    protected override ValueTask<CharacteristicRow> ToRowAsync(Characteristic characteristic) =>
        ValueTask.FromResult(new CharacteristicRow
        {
            Id = RowFormat.Text(characteristic.Id),
            Name = characteristic.Name,
            ValueType = RowFormat.Text(characteristic.ValueType),
            CreatedByUserId = RowFormat.Text(characteristic.CreatedByUserId),
            CreatedAt = RowFormat.Text(characteristic.CreatedAt),
            UpdatedAt = RowFormat.Text(characteristic.UpdatedAt)
        });

    protected override ValueTask<Characteristic> ToEntityAsync(CharacteristicRow row) =>
        ValueTask.FromResult(new Characteristic
        {
            Id = RowFormat.ToGuid(row.Id),
            Name = row.Name,
            ValueType = RowFormat.ToEnum<CharacteristicValueType>(row.ValueType),
            CreatedByUserId = RowFormat.ToGuid(row.CreatedByUserId),
            CreatedAt = RowFormat.ToTime(row.CreatedAt),
            UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
        });
}
