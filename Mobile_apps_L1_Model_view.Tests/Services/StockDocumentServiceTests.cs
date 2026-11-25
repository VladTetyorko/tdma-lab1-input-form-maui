using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Moq;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class StockDocumentServiceTests
{
    private const string Drill = "4006381333931";
    private const string Saw = "4820001000016";

    private readonly TestStore _store = new();
    private readonly InMemoryStockDocumentRepository _documents;
    private readonly StockChanges _changes = new();
    private readonly CapturingLogger<StockDocumentService> _logger = new();

    public StockDocumentServiceTests()
    {
        _documents = new InMemoryStockDocumentRepository(_store.Operations);
    }

    private StockDocumentService Service(IStockDocumentRepository? repository = null) => new(
        repository ?? _documents, _store.Products, new StockService(_store.Operations, _store.Products, _store.CurrentUser,
            _store.Clock, new CapturingLogger<StockService>()), _store.CurrentUser, _store.Clock, _changes, _logger);

    private static StockDocumentDraft Draft(OperationDirection direction, params (string Barcode, int Quantity)[] lines)
    {
        var draft = new StockDocumentDraft(direction) { Counterparty = "Acme Foods" };
        foreach (var (barcode, quantity) in lines)
        {
            draft.Add(barcode, quantity);
        }

        return draft;
    }

    [Fact]
    public async Task Commit_SavesOneDocumentWithOneOperationPerProductAndLogsOnce()
    {
        await _store.SignInAsync(Role.Driver);
        await _store.AddProductAsync("Drill", Drill);
        await _store.AddProductAsync("Saw", Saw);

        var result = await Service().CommitAsync(Draft(OperationDirection.In, (Drill, 3), (Saw, 2)));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentNumberGenerator.Prefix(OperationDirection.In, _store.Clock.UtcNow) + "0001", result.Value!.Number);
        var operations = await _store.Operations.GetAllAsync();
        Assert.Equal(2, operations.Count);
        Assert.All(operations, o => Assert.Equal((result.Value.Id, (string?)null), (o.DocumentId!.Value, o.Counterparty)));
        Assert.Equal(1, _changes.Version);
        var line = Assert.Single(_logger.Records, r => r.Level == LogLevel.Information);
        Assert.DoesNotContain("Acme", line.Message);
    }

    [Fact]
    public async Task Commit_SecondDocumentOfTheDay_TakesTheNextNumber()
    {
        await _store.SignInAsync(Role.Manager);
        await _store.AddProductAsync("Drill", Drill);
        await Service().CommitAsync(Draft(OperationDirection.In, (Drill, 3)));

        var second = await Service().CommitAsync(Draft(OperationDirection.In, (Drill, 1)));

        Assert.EndsWith("-0002", second.Value!.Number);
    }

    [Fact]
    public async Task Commit_DriverIssue_IsRefusedAndNothingIsWritten()
    {
        await _store.SignInAsync(Role.Driver);
        var repository = new Mock<IStockDocumentRepository>();

        var result = await Service(repository.Object).CommitAsync(Draft(OperationDirection.Out, (Drill, 1)));

        Assert.False(result.IsSuccess);
        repository.Verify(r => r.AddAsync(It.IsAny<StockDocument>(), It.IsAny<IReadOnlyList<Operation>>()), Times.Never);
        Assert.Contains(_logger.Records, r => r.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData(OperationDirection.Out, 1, "Only 0")]
    [InlineData(OperationDirection.In, 0, "No product")]
    public async Task Commit_AboveStockOrUnknownBarcode_IsRefused(OperationDirection direction, int createDrill, string message)
    {
        await _store.SignInAsync(Role.Storekeeper);
        if (createDrill == 1)
        {
            await _store.AddProductAsync("Drill", Drill);
        }

        var result = await Service().CommitAsync(Draft(direction, (Drill, 5)));

        Assert.Contains(message, result.Error);
        Assert.Empty(_documents.Documents);
        Assert.Equal(0, _changes.Version);
    }

    [Fact]
    public async Task Commit_EmptyDraft_IsRefused()
    {
        await _store.SignInAsync(Role.Manager);

        Assert.False((await Service().CommitAsync(Draft(OperationDirection.In))).IsSuccess);
    }

    [Fact]
    public async Task Commit_NobodySignedIn_IsRefused()
    {
        Assert.False((await Service().CommitAsync(Draft(OperationDirection.In, (Drill, 1)))).IsSuccess);
    }

    [Fact]
    public async Task Commit_StorageFails_ReportsNothingRecordedAndLogsAnError()
    {
        await _store.SignInAsync(Role.Manager);
        await _store.AddProductAsync("Drill", Drill);
        _documents.FailWith = new IOException("disk full");

        var result = await Service().CommitAsync(Draft(OperationDirection.In, (Drill, 1)));

        Assert.Equal(StockDocumentService.NotSaved, result.Error);
        Assert.Contains(_logger.Records, r => r.Level == LogLevel.Error && r.Exception is IOException);
        Assert.Equal(0, _changes.Version);
    }
}
