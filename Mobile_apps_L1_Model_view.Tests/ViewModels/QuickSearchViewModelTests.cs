using Microsoft.Extensions.Logging;
using Moq;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class QuickSearchViewModelTests
{
    private const string Drill = "4006381333931";
    private const string Saw = "4820001000016";
    private const string Unknown = "3017620422003";

    private readonly TestStore _store = new();
    private readonly List<string> _picked = [];
    private readonly List<string> _submitted = [];

    private async Task<QuickSearchViewModel> OpenAsync(Role role = Role.Manager)
    {
        await _store.SignInAsync(role);
        await _store.AddProductAsync("Drill small", Drill);
        await _store.AddProductAsync("Drill big", Saw);
        var viewModel = new QuickSearchViewModel(_store.Catalog, _store.CharacteristicService, _store.CurrentUser,
            _store.Guard, _store.Navigation, _store.Keyboard, _store.Logger<QuickSearchViewModel>());
        viewModel.Picked += (_, product) => _picked.Add(product.Name);
        viewModel.SubmittedText += (_, text) => _submitted.Add(text);
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task Picking_HidesTheKeyboard_TypingDoesNot()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "dri";
        Assert.Equal(0, _store.Keyboard.Hides);

        viewModel.PickCommand.Execute(viewModel.Suggestions[0]);

        Assert.Equal(1, _store.Keyboard.Hides);
    }

    [Fact]
    public async Task Enter_NeverPicksASimilarName()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "drlil smal";
        Assert.True(Assert.Single(viewModel.Suggestions).Suggestion.IsFuzzy);

        viewModel.SubmitCommand.Execute(null);

        Assert.Empty(_picked);
        Assert.Empty(_submitted);
    }

    [Fact]
    public async Task Enter_OnACode_TakesTheExactBarcodeWay()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = Unknown;

        viewModel.SubmitCommand.Execute(null);

        Assert.Equal([Unknown], _submitted);
        Assert.Empty(_picked);
    }

    [Fact]
    public async Task UnreadableCatalog_IsLoggedAndNothingBreaks()
    {
        var catalog = new Mock<IProductCatalog>();
        catalog.Setup(c => c.GetSummariesAsync()).ThrowsAsync(new IOException("no database"));
        var viewModel = new QuickSearchViewModel(catalog.Object, _store.CharacteristicService, _store.CurrentUser,
            _store.Guard, _store.Navigation, _store.Keyboard, _store.Logger<QuickSearchViewModel>());

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.Query = "dri";

        Assert.Empty(viewModel.Suggestions);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Typing_OpensAndCloseHides()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "d";
        Assert.False(viewModel.IsOpen);

        viewModel.Query = "dri";
        Assert.True(viewModel is { IsOpen: true, HasSuggestions: true, HasNoMatch: false, HighlightedIndex: -1 });
        Assert.Equal(2, viewModel.Suggestions.Count);

        viewModel.CloseCommand.Execute(null);
        Assert.False(viewModel.IsOpen);
        Assert.Empty(_picked);
    }

    [Fact]
    public async Task Arrows_MoveTheHighlightAndWrap()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "dri";

        viewModel.MovePreviousCommand.Execute(null);
        Assert.Equal(1, viewModel.HighlightedIndex);
        viewModel.MoveNextCommand.Execute(null);
        Assert.Equal(0, viewModel.HighlightedIndex);
        viewModel.MoveNextCommand.Execute(null);
        viewModel.MoveNextCommand.Execute(null);

        Assert.Equal(0, viewModel.HighlightedIndex);
        Assert.Equal([true, false], viewModel.Suggestions.Select(row => row.IsHighlighted));
    }

    [Fact]
    public async Task Enter_PicksTheHighlightedRow()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "dri";
        viewModel.MoveNextCommand.Execute(null);
        viewModel.MoveNextCommand.Execute(null);

        viewModel.SubmitCommand.Execute(null);

        Assert.Equal(["Drill small"], _picked);
        Assert.Equal(string.Empty, viewModel.Query);
        Assert.False(viewModel.IsOpen);
    }

    [Theory]
    [InlineData(Drill)]
    [InlineData(Unknown)]
    public async Task Enter_OnABarcode_TakesTheBarcodeWay_AsAKeyboardScannerDoes(string barcode)
    {
        var viewModel = await OpenAsync();
        viewModel.Query = barcode;

        viewModel.SubmitCommand.Execute(null);

        Assert.Equal([barcode], _submitted);
        Assert.Empty(_picked);
    }

    [Fact]
    public async Task Enter_WithOneSuggestionPicksIt_WithSeveralDoesNothing()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = "dri";
        viewModel.SubmitCommand.Execute(null);
        Assert.Empty(_picked);

        viewModel.Query = "small";
        viewModel.SubmitCommand.Execute(null);

        Assert.Equal(["Drill small"], _picked);
        Assert.Empty(_submitted);
    }

    [Fact]
    public async Task NoMatch_TheManagerMayCreateFromTheName()
    {
        var viewModel = await OpenAsync();
        var requested = 0;
        viewModel.CreateRequested += (_, _) => requested++;
        viewModel.Query = "hammer";
        Assert.Equal("No product matches \"hammer\".", viewModel.NoMatchText);
        Assert.Equal("Create product \"hammer\"", viewModel.CreateText);
        Assert.True(viewModel.CanCreate);

        await viewModel.CreateCommand.ExecuteAsync(null);

        Assert.Equal(1, requested);
        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal("hammer", _store.Navigation.RequestedParameters[0]![NavigationKeys.ProductName]);
    }

    [Fact]
    public async Task NoMatch_DigitsBecomeTheBarcode()
    {
        var viewModel = await OpenAsync();
        viewModel.Query = Unknown;

        await viewModel.CreateCommand.ExecuteAsync(null);

        Assert.Equal(Unknown, _store.Navigation.RequestedParameters[0]![NavigationKeys.Barcode]);
    }

    [Fact]
    public async Task NoMatch_AStorekeeperIsToldToAsk_AndCreateIsRefused()
    {
        var viewModel = await OpenAsync(Role.Storekeeper);
        viewModel.Query = "hammer";
        Assert.Equal("No product matches \"hammer\". Ask a manager to add it.", viewModel.NoMatchText);
        Assert.False(viewModel.CanCreate);

        await viewModel.CreateCommand.ExecuteAsync(null);

        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }
}
