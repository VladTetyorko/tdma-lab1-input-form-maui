using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

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
    private readonly FakeSheetService _sheets = new();

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
        var search = new QuickSearchViewModel(_store.Catalog, _store.CharacteristicService, _store.CurrentUser,
            _store.Guard, _store.Navigation, _store.Keyboard, _store.Logger<QuickSearchViewModel>());
        var viewModel = new StockDocumentViewModel(_drafts, service, _store.Catalog, _store.CurrentUser, _scanner,
            _feedback, _toasts, _store.Confirm, _sheets, _store.Guard, _store.Navigation, _changes, _store.Clock, search,
            _store.Logger<StockDocumentViewModel>())
        {
            Direction = direction,
            ReturnDelay = TimeSpan.Zero
        };
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static async Task TypeAsync(StockDocumentViewModel viewModel, string barcode)
    {
        viewModel.QuickSearch.Query = barcode;
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
        Assert.Equal("Review 2 units", viewModel.CommitText);
        Assert.Equal("Draft · 1 line · 2 units", viewModel.SummaryText);
        Assert.Equal(string.Empty, viewModel.QuickSearch.Query);
        Assert.Equal(2, _feedback.Successes);
        Assert.True(viewModel.CommitCommand.CanExecute(null));
    }

    [Fact]
    public async Task Commit_ReturnsToWhereTheTaskWasStarted()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task Back_KeepsTheDraftAndSaysSo()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);

        await viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Contains(_toasts.Messages, message => message.Contains("Draft kept · 1 line"));
        Assert.Equal(1, _drafts.LineCount(OperationDirection.In));
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

        Assert.Equal(Routes.NewProduct, Assert.Single(_store.Navigation.RequestedRoutes));
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
        Assert.Equal((string.Empty, false), (viewModel.StatusMessage, viewModel.HasSummary));

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
        _store.Confirm.Answer = false;
        await viewModel.DiscardCommand.ExecuteAsync(null);
        Assert.Single(viewModel.Lines);

        _store.Confirm.Answer = true;
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
        Assert.StartsWith("Received 2 units · R-", viewModel.SuccessText);
        Assert.Equal(string.Empty, viewModel.StatusMessage);
        Assert.Equal(0, _drafts.LineCount(OperationDirection.In));
        Assert.Equal(1, _changes.Version);
        var review = Assert.IsType<DocumentReviewViewModel>(Assert.Single(_sheets.Shown));
        Assert.Equal("Receive 2 units", review.PrimaryText);
        Assert.Equal(["Saw", "Drill"], review.Lines.Select(l => l.Name)); // the order of the page: newest first

        await TypeAsync(viewModel, Drill);
        Assert.Equal(string.Empty, viewModel.SuccessText);
    }

    [Fact]
    public async Task Commit_Cancelled_KeepsTheDraft()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        _sheets.OnShow = sheet =>
        {
            ((DocumentReviewViewModel)sheet).Note = "Waybill 3";
            return FakeSheetService.DismissAsync(sheet);
        };

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Empty(_documents.Documents);
        Assert.Single(viewModel.Lines);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("Waybill 3", viewModel.Note);
    }

    [Fact]
    public async Task Review_OffersTheLastThreeSuppliersAndSavesTheChosenOne()
    {
        var viewModel = await OpenAsync();
        foreach (var name in new[] { "One", "Two", "Three", "Four", "Two" })
        {
            await TypeAsync(viewModel, Drill);
            viewModel.Counterparty = name;
            await viewModel.CommitCommand.ExecuteAsync(null);
        }

        await TypeAsync(viewModel, Drill);
        IReadOnlyList<string> offered = [];
        _sheets.OnShow = async sheet =>
        {
            var review = (DocumentReviewViewModel)sheet;
            offered = [.. review.Names.Select(chip => chip.Name)];
            review.PickNameCommand.Execute(review.Names[1]);
            await FakeSheetService.AcceptAsync(sheet);
        };
        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Equal(["Two", "Four", "Three"], offered);
        Assert.Equal("Four", _documents.Documents[^1].Counterparty);
        Assert.DoesNotContain(_store.Logs, log => log.Message.Contains("Four"));
    }

    [Fact]
    public async Task Menu_DiscardsAfterAQuestion()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        _sheets.OnShow = sheet =>
        {
            var menu = (ActionSheetViewModel)sheet;
            menu.ChooseCommand.Execute(menu.Actions.Single());
            return Task.CompletedTask;
        };

        await viewModel.OpenMenuCommand.ExecuteAsync(null);

        Assert.Single(_store.Confirm.Asked);
        Assert.Empty(viewModel.Lines);
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
        Assert.Equal(("Out of stock", "on hand 0 → 0, short by 1"), (line.Problem, line.StockText));
        Assert.Equal("Drill −1", viewModel.StatusMessage);
        Assert.Equal(("Review 1 unit", "Draft · 1 line · 1 unit"), (viewModel.CommitText, viewModel.SummaryText));
        Assert.False(viewModel.CommitCommand.CanExecute(null));
        Assert.Equal("Issue goods", viewModel.Title);
    }

    [Fact]
    public async Task CameraDenied_TellsTheUserAndKeepsTypingAvailable()
    {
        _scanner.IsCameraSupported = true;
        _scanner.CameraAccessGranted = false;

        var viewModel = await OpenAsync();

        Assert.False(viewModel.IsCameraActive);
        Assert.Equal(CameraState.Denied, viewModel.CameraState);
        Assert.Equal((CameraMessages.Denied, CameraMessages.AllowAction),
            (viewModel.CameraMessage, viewModel.CameraActionText));

        _scanner.CameraAccessGranted = true;
        await viewModel.RetryCameraCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCameraActive);
        Assert.Equal(string.Empty, viewModel.CameraMessage);
        Assert.Equal(2, _scanner.AccessRequests);
    }

    [Fact]
    public async Task NoCamera_SaysSoAndAsksNothing()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(CameraState.Unsupported, viewModel.CameraState);
        Assert.Equal(CameraMessages.Unsupported, viewModel.CameraMessage);
        Assert.Equal(0, _scanner.AccessRequests);
    }

    [Fact]
    public async Task CameraThatCannotStart_FallsBackToTypingAndCanBeRetried()
    {
        _scanner.IsCameraSupported = true;
        var viewModel = await OpenAsync();

        _scanner.ReportStartFailure();

        Assert.False(viewModel.IsCameraActive);
        Assert.Equal((CameraMessages.Failed, CameraMessages.RetryAction),
            (viewModel.CameraMessage, viewModel.CameraActionText));

        await viewModel.RetryCameraCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsCameraActive);

        // A failure after the page was left changes nothing
        viewModel.OnDisappearing();
        _scanner.ReportStartFailure();
        Assert.Equal(CameraState.Off, viewModel.CameraState);
    }

    [Fact]
    public async Task Camera_MakesRoomWhileTypingAndIsReleasedWhenLeaving()
    {
        _scanner.IsCameraSupported = true;
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsCameraActive);

        viewModel.QuickSearch.Query = "dri";
        Assert.False(viewModel.IsCameraActive);
        viewModel.QuickSearch.CloseCommand.Execute(null);
        viewModel.IsTyping = true;
        Assert.False(viewModel.IsCameraActive);
        viewModel.IsTyping = false;
        Assert.True(viewModel.IsCameraActive);

        viewModel.OnDisappearing();
        Assert.False(viewModel.IsCameraActive);
    }

    [Fact]
    public async Task PickingFromTheSearch_AddsOneAndAsksForTheQuantity()
    {
        var viewModel = await OpenAsync();
        viewModel.QuickSearch.Query = "dri";

        viewModel.QuickSearch.PickCommand.Execute(viewModel.QuickSearch.Suggestions.Single());
        await Eventually.WaitAsync(() => viewModel.Lines.Count == 1);

        Assert.Equal(("Drill", 1, true), (viewModel.Lines[0].Name, viewModel.Lines[0].Quantity, viewModel.Lines[0].WantsFocus));
        Assert.Equal("Drill +1", viewModel.StatusMessage);
    }

    [Fact]
    public async Task TheNextScan_TakesTheTypedNumberOver_AndARepeatScanAddsOne()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        viewModel.Lines[0].QuantityText = "12";
        Assert.Equal("Review 12 units", viewModel.CommitText);

        await TypeAsync(viewModel, Saw);
        Assert.Equal([("Saw", 1, true), ("Drill", 12, false)], viewModel.Lines.Select(l => (l.Name, l.Quantity, l.WantsFocus)));

        await TypeAsync(viewModel, Drill);
        Assert.Equal(("Drill", 13), (viewModel.Lines[0].Name, viewModel.Lines[0].Quantity));
    }

    [Fact]
    public async Task ABoxOfARebuiltLine_ChangesNothing()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        var old = viewModel.Lines[0];
        old.QuantityText = "12";
        await TypeAsync(viewModel, Drill);

        await viewModel.SetQuantityCommand.ExecuteAsync(old);

        Assert.Equal(13, viewModel.Lines[0].Quantity);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("two")]
    [InlineData("")]
    public async Task AnUnusableNumber_LeavesTheLineAsItWas(string typed)
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        viewModel.Lines[0].QuantityText = typed;
        _sheets.OnShow = FakeSheetService.DismissAsync;

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Lines[0].Quantity);
        Assert.Equal("Receive 1 unit", Assert.IsType<DocumentReviewViewModel>(_sheets.Shown.Single()).PrimaryText);
    }

    [Fact]
    public async Task Review_CountsANumberTypedWithoutEnter()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        viewModel.Lines[0].QuantityText = "24";

        await viewModel.CommitCommand.ExecuteAsync(null);

        Assert.StartsWith("Received 24 units", viewModel.SuccessText);
    }

    [Fact]
    public async Task EnterInTheSearchBox_AddsAKeyboardScannedBarcode()
    {
        var viewModel = await OpenAsync();
        viewModel.QuickSearch.Query = Saw;

        viewModel.QuickSearch.SubmitCommand.Execute(null);
        await Eventually.WaitAsync(() => viewModel.Lines.Count == 1);

        Assert.Equal("Saw", viewModel.Lines[0].Name);
        Assert.Equal(string.Empty, viewModel.QuickSearch.Query);
    }

    [Fact]
    public async Task CommitHint_SaysWhyTheButtonIsDisabled()
    {
        var viewModel = await OpenAsync(Role.Storekeeper, OperationDirection.Out);
        Assert.Equal("Add a product first", viewModel.CommitHint);

        await TypeAsync(viewModel, Drill);
        Assert.Equal("Fix the red lines first", viewModel.CommitHint);

        var receive = await OpenAsync(direction: OperationDirection.In);
        await TypeAsync(receive, Drill);
        Assert.Equal(string.Empty, receive.CommitHint);
    }

    [Fact]
    public async Task Driver_OnIssue_IsRefusedAndSentHome()
    {
        var viewModel = await OpenAsync(Role.Driver, OperationDirection.Out);

        Assert.Equal(Routes.Home, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(PermissionGuard.AlertTitle, Assert.Single(_store.Alerts.Shown).Title);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
        Assert.Empty(viewModel.Lines);
    }

    [Fact]
    public async Task SavedNote_IsInTheReviewAgain()
    {
        var viewModel = await OpenAsync();
        await TypeAsync(viewModel, Drill);
        viewModel.Note = "Waybill 7";
        await Eventually.WaitAsync(() => _store.AppState.GetDraft<StockDocumentDraftStore.StoredDocuments>() is not null);
        _sheets.OnShow = FakeSheetService.DismissAsync;

        var reopened = await OpenAsync();
        await reopened.CommitCommand.ExecuteAsync(null);

        var review = Assert.IsType<DocumentReviewViewModel>(Assert.Single(_sheets.Shown));
        Assert.True(review.IsNoteOpen);
        Assert.Equal("Waybill 7", review.Note);
    }
}
