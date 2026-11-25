using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class StockDocumentViewModelTests
{
    private const string Drill = "4006381333931";
    private const string Saw = "4820001000016";
    private const string Unknown = "3017620422003";

    private readonly TestStore _store = new();
    private readonly InMemoryStockDocumentRepository _documents;
    private readonly StockDocumentDraftStore _drafts;
    private readonly FakeScanFeedback _feedback = new();
    private readonly FakeToastService _toasts = new();
    private readonly FakeBarcodeScanner _scanner = new() { IsCameraSupported = false };
    private readonly StockChanges _changes = new();

    public StockDocumentViewModelTests()
    {
        _documents = new InMemoryStockDocumentRepository(_store.Operations);
        _drafts = new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets),
            new CapturingLogger<StockDocumentDraftStore>());
    }

    private async Task<StockDocumentViewModel> OpenAsync(
        Role role = Role.Manager, OperationDirection direction = OperationDirection.In)
    {
        if (_store.CurrentUser.CurrentUser is null)
        {
            await _store.SignInAsync(role);
            await _store.AddProductAsync("Drill", Drill);
            await _store.AddProductAsync("Saw", Saw);
        }

        var stock = new StockService(_store.Operations, _store.Products, _store.CurrentUser, _store.Clock,
            new CapturingLogger<StockService>());
        var service = new StockDocumentService(_documents, _store.Products, stock, _store.CurrentUser, _store.Clock,
            _changes, new CapturingLogger<StockDocumentService>());
        var viewModel = new StockDocumentViewModel(_drafts, service, _store.Catalog, _store.CurrentUser, _scanner,
            _feedback, _toasts, _store.Alerts, _store.Guard, _store.Navigation, _changes, _store.Clock,
            new CapturingLogger<StockDocumentViewModel>())
        {
            Direction = direction
        };
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static async Task TypeAsync(StockDocumentViewModel viewModel, string barcode)
    {
        viewModel.ScanText = barcode;
        await viewModel.AddTypedCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task Typing_AddsALineAndTheSameBarcodeAgainIncrementsIt()
    {
        var viewModel = await OpenAsync();

        await TypeAsync(viewModel, Drill);
        await TypeAsync(viewModel, Drill);

        var line = Assert.Single(viewModel.Lines);
        Assert.Equal(("Drill", 2, "on hand 0 → 2"), (line.Name, line.Quantity, line.StockText));
        Assert.Equal("Receive 2 units", viewModel.CommitText);
        Assert.Equal("Draft · 1 line · 2 units", viewModel.SummaryText);
        Assert.Equal(string.Empty, viewModel.ScanText);
        Assert.Equal(2, _feedback.Successes);
        Assert.True(viewModel.CommitCommand.CanExecute(null));
    }

    [Fact]
    public async Task CameraScans_AreDebounced_ButTypedOnesAreNot()
    {
        var viewModel = await OpenAsync();

        await viewModel.ScanDetectedCommand.ExecuteAsync(Drill);
        await viewModel.ScanDetectedCommand.ExecuteAsync(Drill);
        await TypeAsync(viewModel, Drill);

        Assert.Equal(2, viewModel.Lines.Single().Quantity);
    }

    [Fact]
    public async Task NotABarcode_ShowsTheReason()
    {
        var viewModel = await OpenAsync();

        await TypeAsync(viewModel, "hello");

        Assert.Empty(viewModel.Lines);
        Assert.Equal(ScanPayloadParser.NotStockRoomCodeMessage, viewModel.StatusMessage);
        Assert.Equal(1, _feedback.Errors);
    }

    [Fact]
    public async Task UnknownBarcode_BlocksTheCommit_AndTheManagerCanCreateTheProduct()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Unknown);
        var line = viewModel.Lines.Single();

        Assert.True(line.IsUnknown);
        Assert.False(viewModel.CommitCommand.CanExecute(null));
        await viewModel.CreateProductCommand.ExecuteAsync(line);

        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task UnknownBarcode_OthersCannotCreate()
    {
        var viewModel = await OpenAsync(Role.Driver);
        await TypeAsync(viewModel, Unknown);

        await viewModel.CreateProductCommand.ExecuteAsync(viewModel.Lines.Single());

        Assert.False(viewModel.CanCreateProducts);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.NotEqual(string.Empty, viewModel.StatusMessage);
    }

    [Fact]
    public async Task Stepper_AndTypedQuantity_ChangeTheLine()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);

        await viewModel.IncrementCommand.ExecuteAsync(viewModel.Lines.Single());
        await viewModel.IncrementCommand.ExecuteAsync(viewModel.Lines.Single());
        await viewModel.DecrementCommand.ExecuteAsync(viewModel.Lines.Single());
        Assert.Equal(2, viewModel.Lines.Single().Quantity);

        var line = viewModel.Lines.Single();
        line.QuantityText = "12";
        await viewModel.SetQuantityCommand.ExecuteAsync(line);
        Assert.Equal(12, viewModel.Lines.Single().Quantity);

        line = viewModel.Lines.Single();
        line.QuantityText = "x";
        await viewModel.SetQuantityCommand.ExecuteAsync(line);
        Assert.Equal(12, viewModel.Lines.Single().Quantity);
        Assert.Equal("Enter a whole number of units.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Remove_CanBeUndoneFromThePageOrTheToast()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);

        await viewModel.DecrementCommand.ExecuteAsync(viewModel.Lines.Single());
        Assert.Empty(viewModel.Lines);
        Assert.True(viewModel.CanUndo);

        _toasts.Action!();

        Assert.Single(viewModel.Lines);
        Assert.False(viewModel.CanUndo);
    }

    [Fact]
    public async Task Draft_SurvivesReopeningThePage()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        viewModel.Counterparty = "Acme";
        await Eventually.WaitAsync(() => _drafts.LineCount(OperationDirection.In) == 1);

        var reopened = await OpenAsync();

        Assert.Equal(("Drill", "Acme"), (reopened.Lines.Single().Name, reopened.Counterparty));
    }

    [Fact]
    public async Task Discard_AsksFirst()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        _store.Alerts.ConfirmAnswer = false;
        await viewModel.DiscardCommand.ExecuteAsync(null);
        Assert.Single(viewModel.Lines);

        _store.Alerts.ConfirmAnswer = true;
        await viewModel.DiscardCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Lines);
        Assert.Equal(0, _drafts.LineCount(OperationDirection.In));
    }

    [Fact]
    public async Task Commit_SavesTheDocumentClearsTheDraftAndSignalsTheChange()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        await TypeAsync(viewModel, Saw);

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Single(_documents.Documents);
        Assert.Empty(viewModel.Lines);
        Assert.StartsWith("Saved R-", viewModel.StatusMessage);
        Assert.Equal(0, _drafts.LineCount(OperationDirection.In));
        Assert.Equal(1, _changes.Version);
        Assert.Contains("Receive 2 units of 2 products?", _store.Alerts.Confirmations.Single().Message);
    }

    [Fact]
    public async Task Commit_Cancelled_KeepsTheDraft()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        _store.Alerts.ConfirmAnswer = false;

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Empty(_documents.Documents);
        Assert.Single(viewModel.Lines);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Commit_StorageFails_KeepsTheDraftAndSaysSo()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        _documents.FailWith = new IOException("disk full");

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Lines);
        Assert.Equal(StockDocumentService.NotSaved, viewModel.StatusMessage);
    }

    [Fact]
    public async Task Issue_AboveStock_MarksTheLineAndDisablesCommit()
    {
        var viewModel = await OpenAsync(Role.Storekeeper, OperationDirection.Out);

        await TypeAsync(viewModel, Drill);

        var line = viewModel.Lines.Single();
        Assert.True(line.IsInvalid);
        Assert.Equal("Only 0 on hand", line.Problem);
        Assert.False(viewModel.CommitCommand.CanExecute(null));
        Assert.Equal(("Issue goods", "Recipient (optional)"), (viewModel.Title, viewModel.CounterpartyLabel));
    }

    [Fact]
    public async Task CameraDenied_TellsTheUserAndKeepsTypingAvailable()
    {
        _scanner.IsCameraSupported = true;
        _scanner.CameraAccessGranted = false;

        var viewModel = await OpenAsync();

        Assert.False(viewModel.IsCameraActive);
        Assert.Contains("denied", viewModel.StatusMessage);
        viewModel.OnDisappearing();
    }
}
