using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductCatalogTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly IProductCatalog _catalog;

    public ProductCatalogTests()
    {
        _catalog = _store.Catalog;
    }

    [Fact]
    public async Task GetSummariesAsync_ReturnsQuantityAndValuesForEveryProduct()
    {
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        var saw = await _store.AddProductAsync("Saw", "4820001000016");
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(drill, color, "Blue");
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 10 });
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.Out, Quantity = 4 });

        var summaries = await _catalog.GetSummariesAsync();

        Assert.Equal(2, summaries.Count);
        var drillSummary = summaries.Single(s => s.Id == drill.Id);
        Assert.Equal("Drill", drillSummary.Name);
        Assert.Equal(ValidBarcode, drillSummary.Barcode);
        Assert.Equal(6, drillSummary.Quantity);
        Assert.Equal("Blue", drillSummary.Values[color.Id]);
        var sawSummary = summaries.Single(s => s.Id == saw.Id);
        Assert.Equal(0, sawSummary.Quantity);
        Assert.Empty(sawSummary.Values);
    }

    [Fact]
    public async Task GetSummaryAsync_UnknownProduct_ReturnsNull()
    {
        Assert.Null(await _catalog.GetSummaryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSummaryAsync_KnownProduct_ReturnsQuantityOnHand()
    {
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 3 });

        var summary = await _catalog.GetSummaryAsync(drill.Id);

        Assert.NotNull(summary);
        Assert.Equal(drill.Id, summary.Id);
        Assert.Equal(3, summary.Quantity);
    }

    [Fact]
    public async Task GetDetailsAsync_UnknownProduct_ReturnsNull()
    {
        Assert.Null(await _catalog.GetDetailsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDetailsAsync_ListsEveryCharacteristicWithEmptyValueWhenAbsent()
    {
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        var weight = await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        var color = await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SetValueAsync(drill, weight, "1.6");

        var details = await _catalog.GetDetailsAsync(drill.Id);

        Assert.NotNull(details);
        Assert.Equal(2, details.Characteristics.Count);
        var weightItem = details.Characteristics.Single(c => c.CharacteristicId == weight.Id);
        Assert.Equal("Weight (kg)", weightItem.Name);
        Assert.Equal(CharacteristicValueType.Number, weightItem.ValueType);
        Assert.Equal("1.6", weightItem.Value);
        var colorItem = details.Characteristics.Single(c => c.CharacteristicId == color.Id);
        Assert.Equal(string.Empty, colorItem.Value);
    }

    [Fact]
    public async Task GetDetailsAsync_SummaryMatchesTheListRow()
    {
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 2 });

        var details = await _catalog.GetDetailsAsync(drill.Id);

        Assert.NotNull(details);
        Assert.Equal(drill.Id, details.Summary.Id);
        Assert.Equal(2, details.Summary.Quantity);
    }

    [Fact]
    public async Task GetDetailsAsync_HistoryIsNewestFirst()
    {
        var manager = await _store.SignInAsync(Role.Manager, "Olena Kovalenko");
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        var start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        await _store.Operations.AddAsync(Movement(drill, OperationDirection.In, 5, manager, start));
        await _store.Operations.AddAsync(Movement(drill, OperationDirection.Out, 2, manager, start.AddDays(2)));
        await _store.Operations.AddAsync(Movement(drill, OperationDirection.In, 1, manager, start.AddDays(1)));

        var details = await _catalog.GetDetailsAsync(drill.Id);

        Assert.NotNull(details);
        Assert.Equal(new[] { 2, 1, 5 }, details.History.Select(h => h.Quantity));
        Assert.Equal(new[] { start.AddDays(2), start.AddDays(1), start }, details.History.Select(h => h.At));
    }

    [Fact]
    public async Task GetDetailsAsync_OperationsAtSameInstant_AreOrderedByIdDescending()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        var at = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var first = Movement(drill, OperationDirection.In, 1, manager, at);
        var second = Movement(drill, OperationDirection.In, 2, manager, at);
        await _store.Operations.AddAsync(first);
        await _store.Operations.AddAsync(second);

        var details = await _catalog.GetDetailsAsync(drill.Id);

        Assert.NotNull(details);
        var expected = (first.Id.CompareTo(second.Id) > 0 ? new[] { 1, 2 } : new[] { 2, 1 });
        Assert.Equal(expected, details.History.Select(h => h.Quantity));
    }

    [Fact]
    public async Task GetDetailsAsync_HistoryNamesTheUserOrUnknown()
    {
        var olena = await _store.SignInAsync(Role.Manager, "Olena Kovalenko");
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        await _store.Operations.AddAsync(Movement(drill, OperationDirection.In, 1, olena, _store.Clock.UtcNow));
        await _store.Operations.AddAsync(Movement(drill, OperationDirection.In, 1, new User { FullName = "Gone" }, _store.Clock.UtcNow.AddMinutes(1)));

        var details = await _catalog.GetDetailsAsync(drill.Id);

        Assert.NotNull(details);
        Assert.Equal(new[] { "Unknown", "Olena Kovalenko" }, details.History.Select(h => h.UserName));
    }

    [Fact]
    public async Task GetDetailsAsync_HistoryKeepsCounterpartyAndReason()
    {
        var manager = await _store.SignInAsync(Role.Manager);
        var drill = await _store.AddProductAsync("Drill", ValidBarcode);
        await _store.Operations.AddAsync(new Operation
        {
            ProductId = drill.Id,
            Direction = OperationDirection.In,
            Quantity = 4,
            Counterparty = "Acme Ltd",
            Reason = "Initial delivery",
            CreatedByUserId = manager.Id
        });

        var line = Assert.Single((await _catalog.GetDetailsAsync(drill.Id))!.History);

        Assert.Equal(OperationDirection.In, line.Direction);
        Assert.Equal(4, line.Quantity);
        Assert.Equal("Acme Ltd · Initial delivery", line.Details);
    }

    private static Operation Movement(
        Product product, OperationDirection direction, int quantity, User user, DateTime at) =>
        new()
        {
            ProductId = product.Id,
            Direction = direction,
            Quantity = quantity,
            CreatedByUserId = user.Id,
            CreatedAt = at
        };
}
