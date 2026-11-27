using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class StockCountViewModelTests
{
    private const string Drill = "4006381333931";
    private const string Saw = "4820001000016";
    private const string Tape = "3017620422003";

    private readonly TestStore _store = new();
    private readonly FakeScanFeedback _feedback = new();
    private readonly StockChanges _changes = new();

    private StockDocumentService Documents => new(_store.Documents, _store.Products, _store.Stock, _store.CurrentUser,
        _store.Clock, _changes, _store.Logger<StockDocumentService>());

    private StockCountService Count => new(_store.Catalog, Documents, _store.CurrentUser, _store.Clock,
        _store.Logger<StockCountService>());

    private async Task<StockCountViewModel> OpenAsync(Role role = Role.Manager)
    {
        if (_store.CurrentUser.CurrentUser is null)
        {
            var user = await _store.SignInAsync(role);
            foreach (var (name, code, quantity) in new[] { ("Drill", Drill, 10), ("Saw", Saw, 5), ("Tape", Tape, 3) })
            {
                var product = await _store.AddProductAsync(name, code);
                await _store.Operations.AddAsync(new Operation
                {
                    ProductId = product.Id, Direction = OperationDirection.In, Quantity = quantity, CreatedByUserId = user.Id
                });
            }
        }

        var viewModel = new StockCountViewModel(_store.Catalog, Count, _store.AppState, _store.Guard, _store.Navigation,
            _store.Scanner, _feedback, _store.Sheets, _store.Confirm, _store.Toasts, _store.Clock, _store.QuickSearch,
            _store.Logger<StockCountViewModel>());
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static CountRow Row(StockCountViewModel viewModel, string barcode) =>
        viewModel.Rows.Single(row => row.Barcode == barcode);

    [Fact]
    public async Task Counting_ShowsTheDifferenceAndTheProgress()
    {
        var viewModel = await OpenAsync();
        Assert.Equal(("0 of 3 counted", "Count a product first"), (viewModel.Progress, viewModel.ReviewHint));
        Assert.False(viewModel.ReviewCommand.CanExecute(null));

        Row(viewModel, Drill).CountedText = "12";
        Row(viewModel, Saw).CountedText = "4";
        Row(viewModel, Tape).CountedText = "3";

        Assert.Equal(["+2", "−1", "✓"], viewModel.Rows.Select(row => row.DifferenceText));
        Assert.Equal(("3 of 3 counted", "Review 2 differences"), (viewModel.Progress, viewModel.ReviewText));
    }

    [Fact]
    public async Task TheCount_SurvivesLeavingThePage_AndAScanFocusesTheRow()
    {
        var viewModel = await OpenAsync();
        Row(viewModel, Drill).CountedText = "12";
        CountRow? focused = null;

        var reopened = await OpenAsync();
        reopened.FocusRequested += (_, row) => focused = row;
        reopened.ScanDetectedCommand.Execute(Saw);

        Assert.Equal("12", Row(reopened, Drill).CountedText);
        Assert.Equal(Saw, focused?.Barcode);
        reopened.NextCommand.Execute(Row(reopened, Saw));
        Assert.Equal(Tape, focused?.Barcode);
    }

    [Fact]
    public async Task Post_WritesAReceiptAndAnIssue_AndLeavesUncountedProductsAlone()
    {
        var viewModel = await OpenAsync();
        Row(viewModel, Drill).CountedText = "12";
        Row(viewModel, Saw).CountedText = "1";

        await viewModel.ReviewCommand.ExecuteAsync(null);

        var review = Assert.IsType<CountReviewViewModel>(Assert.Single(_store.Sheets.Shown));
        Assert.Equal("Post 2 corrections", review.PrimaryText);
        Assert.Equal("1 product not counted: unchanged.", review.Summary);
        Assert.Equal([OperationDirection.In, OperationDirection.Out], _store.Documents.Documents.Select(d => d.Direction));
        Assert.All(_store.Documents.Documents, d => Assert.StartsWith("Stock count ", d.Note));
        var quantities = (await _store.Catalog.GetSummariesAsync()).ToDictionary(p => p.Name, p => p.Quantity);
        Assert.Equal((12, 1, 3), (quantities["Drill"], quantities["Saw"], quantities["Tape"]));
        Assert.Equal(Routes.Back, _store.Navigation.RequestedRoutes.Last());
        Assert.Null(_store.AppState.GetDraft<StockCountDraft>());
    }

    [Fact]
    public async Task AProductThatChangedSinceItWasCounted_IsKeptOutAndMustBeCountedAgain()
    {
        var viewModel = await OpenAsync();
        Row(viewModel, Drill).CountedText = "12";
        Row(viewModel, Saw).CountedText = "4";
        var drill = (await _store.Products.GetByBarcodeAsync(Drill))!;
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.Out, Quantity = 1 });

        await viewModel.ReviewCommand.ExecuteAsync(null);

        Assert.Equal(OperationDirection.Out, Assert.Single(_store.Documents.Documents).Direction);
        Assert.Equal(StockCountViewModel.RecountText, Row(viewModel, Drill).Problem);
        Assert.Equal(string.Empty, Row(viewModel, Saw).CountedText);
    }

    [Fact]
    public async Task Menu_DiscardsTheCountAfterAQuestion()
    {
        var viewModel = await OpenAsync();
        Row(viewModel, Drill).CountedText = "12";
        _store.Sheets.OnShow = sheet =>
        {
            var menu = (ActionSheetViewModel)sheet;
            menu.ChooseCommand.Execute(menu.Actions.Single());
            return Task.CompletedTask;
        };

        await viewModel.OpenMenuCommand.ExecuteAsync(null);

        Assert.Null(_store.AppState.GetDraft<StockCountDraft>());
        Assert.Equal(string.Empty, Row(viewModel, Drill).CountedText);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task OnlyTheManagerCounts(Role role)
    {
        var viewModel = await OpenAsync(role);

        var result = await Count.PostAsync(new StockCountDraft(_store.Clock.UtcNow, []));

        Assert.Equal(Routes.Home, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Empty(viewModel.Rows);
        Assert.False(result.IsSuccess);
        Assert.Empty(_store.Documents.Documents);
    }
}
