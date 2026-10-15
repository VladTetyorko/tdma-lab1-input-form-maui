using Mobile_apps_L1_Model_view.Data.Rows;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Data;

public sealed class SqliteProductRepository(IDatabase database, SyncJournal journal)
    : SqliteRepository<Product, ProductRow>(database, journal), IProductRepository
{
    public Task<IReadOnlyList<Product>> GetAllAsync() => QueryAsync();

    public Task<Product?> GetByIdAsync(Guid id) => FindAsync("WHERE Id = ?", RowFormat.Text(id));

    public Task<Product?> GetByBarcodeAsync(string barcode) => FindAsync("WHERE Barcode = ?", barcode);

    public Task AddAsync(Product product) => InsertAsync(product);

    Task IProductRepository.UpdateAsync(Product product) => UpdateAsync(product);

    // The product and its values go in one transaction, each with its own queue entry
    public new Task DeleteAsync(Guid id) =>
        InTransactionAsync(transaction =>
        {
            var key = RowFormat.Text(id);
            var values = transaction.Query<ProductCharacteristicRow>(
                "SELECT * FROM ProductCharacteristics WHERE ProductId = ?", key);
            foreach (var value in values)
            {
                DeleteRow(transaction, value);
            }

            if (transaction.Find<ProductRow>(key) is { } product)
            {
                DeleteRow(transaction, product);
            }
        });

    protected override ValueTask<ProductRow> ToRowAsync(Product product) => ValueTask.FromResult(new ProductRow
    {
        Id = RowFormat.Text(product.Id),
        Name = product.Name,
        Barcode = product.Barcode,
        CreatedByUserId = RowFormat.Text(product.CreatedByUserId),
        CreatedAt = RowFormat.Text(product.CreatedAt),
        UpdatedAt = RowFormat.Text(product.UpdatedAt)
    });

    protected override ValueTask<Product> ToEntityAsync(ProductRow row) => ValueTask.FromResult(new Product
    {
        Id = RowFormat.ToGuid(row.Id),
        Name = row.Name,
        Barcode = row.Barcode,
        CreatedByUserId = RowFormat.ToGuid(row.CreatedByUserId),
        CreatedAt = RowFormat.ToTime(row.CreatedAt),
        UpdatedAt = RowFormat.ToTime(row.UpdatedAt)
    });
}
