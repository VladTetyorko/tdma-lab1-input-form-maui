using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class OperationViewModelTests
{
    private const string DrillBarcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly OperationViewModel _viewModel;
    private Product _drill = null!;

    public OperationViewModelTests()
    {
        _viewModel = new OperationViewModel(_store.Stock, _store.CurrentUser, _store.Drafts, _store.Navigation);
    }

    [Fact]
    public async Task ApplyQueryAttributes_ShowsProductNameAndQuantityOnHand()
    {
        await SeedDrillWithTenUnitsAsync();

        ApplyDrill();

        Assert.Equal("Drill", _viewModel.ProductName);
        Assert.Equal(10, _viewModel.QuantityOnHand);
    }

    [Fact]
    public void Quantity_DefaultsToOneAndDirectionToIn()
    {
        Assert.Equal(1, _viewModel.Quantity);
        Assert.Equal(OperationDirection.In, _viewModel.SelectedDirection);
    }

    [Theory]
    [InlineData(Role.Manager, new[] { OperationDirection.In, OperationDirection.Out })]
    [InlineData(Role.Storekeeper, new[] { OperationDirection.In, OperationDirection.Out })]
    [InlineData(Role.Driver, new[] { OperationDirection.In })]
    public async Task Load_OffersOnlyTheDirectionsTheRoleMayRecord(Role role, OperationDirection[] expected)
    {
        await _store.SignInAsync(role);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(expected, _viewModel.AvailableDirections);
    }

    [Fact]
    public async Task Load_DriverWithOutSelected_SwitchesToIn()
    {
        await _store.SignInAsync(Role.Driver);
        _viewModel.SelectedDirection = OperationDirection.Out;

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(OperationDirection.In, _viewModel.SelectedDirection);
    }

    [Fact]
    public async Task Load_RefreshesQuantityOnHand()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();

        await _store.Stock.RecordAsync(_drill.Id, OperationDirection.Out, 3);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(7, _viewModel.QuantityOnHand);
    }

    [Fact]
    public async Task Save_Success_RecordsTheOperationAndNavigatesBack()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.SelectedDirection = OperationDirection.Out;
        _viewModel.Quantity = 2;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var recorded = (await _store.Operations.GetByProductAsync(_drill.Id))
            .Single(o => o.Direction == OperationDirection.Out);
        Assert.Equal(2, recorded.Quantity);
        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Null(Assert.Single(_store.Navigation.RequestedParameters));
    }

    [Fact]
    public async Task Save_BlankCounterpartyAndReason_StoresNull()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.Counterparty = "   ";
        _viewModel.Reason = string.Empty;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var recorded = Assert.Single(await _store.Operations.GetByProductAsync(_drill.Id), o => o.Quantity == 1);
        Assert.Null(recorded.Counterparty);
        Assert.Null(recorded.Reason);
    }

    [Fact]
    public async Task Save_TrimsCounterpartyAndReason()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Driver);
        ApplyDrill();
        _viewModel.Counterparty = "  Acme Ltd ";
        _viewModel.Reason = " Initial delivery ";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var recorded = Assert.Single(await _store.Operations.GetByProductAsync(_drill.Id), o => o.Quantity == 1);
        Assert.Equal("Acme Ltd", recorded.Counterparty);
        Assert.Equal("Initial delivery", recorded.Reason);
    }

    [Fact]
    public async Task Save_ZeroQuantity_SetsErrorMessageAndDoesNotNavigate()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.Quantity = 0;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Quantity must be greater than zero.", _viewModel.ErrorMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task Save_OutMoreThanOnHand_SetsErrorMessageAndDoesNotNavigate()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.SelectedDirection = OperationDirection.Out;
        _viewModel.Quantity = 11;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Not enough units on hand.", _viewModel.ErrorMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Equal(10, _viewModel.QuantityOnHand);
    }

    [Fact]
    public async Task Save_DriverRecordingOut_SetsRoleErrorAndDoesNotNavigate()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Driver);
        ApplyDrill();
        _viewModel.SelectedDirection = OperationDirection.Out;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Your role (Driver) cannot record Out operations.", _viewModel.ErrorMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task GoBack_NavigatesBack()
    {
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task ChangingTheForm_SavesADraftForTheProduct()
    {
        await SeedDrillWithTenUnitsAsync();
        ApplyDrill();

        _viewModel.SelectedDirection = OperationDirection.Out;
        _viewModel.Quantity = 4;
        _viewModel.Counterparty = "Acme Ltd";
        _viewModel.Reason = "Site 7";

        var draft = await _store.Drafts.LoadAsync(DrillBarcode);
        Assert.Equal(new OperationDraft(DrillBarcode, OperationDirection.Out, 4, "Acme Ltd", "Site 7"), draft);
    }

    [Fact]
    public async Task ApplyQueryAttributes_RestoresTheDraftForTheSameProduct()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.Drafts.SaveAsync(new OperationDraft(DrillBarcode, OperationDirection.Out, 4, "Acme Ltd", "Site 7"));

        ApplyDrill();
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(OperationDirection.Out, _viewModel.SelectedDirection);
        Assert.Equal(4, _viewModel.Quantity);
        Assert.Equal("Acme Ltd", _viewModel.Counterparty);
        Assert.Equal("Site 7", _viewModel.Reason);
    }

    [Fact]
    public async Task ApplyQueryAttributes_DraftForAnotherProduct_IsNotRestored()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.Drafts.SaveAsync(new OperationDraft("4820001000016", OperationDirection.Out, 4, "Acme Ltd", "Site 7"));

        ApplyDrill();

        Assert.Equal(OperationDirection.In, _viewModel.SelectedDirection);
        Assert.Equal(1, _viewModel.Quantity);
        Assert.Equal(string.Empty, _viewModel.Counterparty);
    }

    [Fact]
    public async Task Save_Success_ClearsTheDraft()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.Quantity = 2;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(await _store.Drafts.LoadAsync(DrillBarcode));
    }

    [Fact]
    public async Task Save_Failure_KeepsTheDraft()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.SignInAsync(Role.Storekeeper);
        ApplyDrill();
        _viewModel.SelectedDirection = OperationDirection.Out;
        _viewModel.Quantity = 11;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(11, (await _store.Drafts.LoadAsync(DrillBarcode))!.Quantity);
    }

    [Fact]
    public async Task GoBack_ClearsTheDraft()
    {
        await SeedDrillWithTenUnitsAsync();
        ApplyDrill();
        _viewModel.Quantity = 3;

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Null(await _store.Drafts.LoadAsync(DrillBarcode));
    }

    [Fact]
    public async Task Draft_IsPersistedWithoutThePlainCounterparty()
    {
        await SeedDrillWithTenUnitsAsync();
        ApplyDrill();

        _viewModel.Counterparty = "Acme Ltd";
        _store.AppState.Save();

        Assert.DoesNotContain("Acme Ltd", string.Concat(_store.Preferences.Values.Values));
    }

    [Fact]
    public async Task ApplyQueryAttributes_DraftWithLostKey_RestoresTheRestWithoutCounterparty()
    {
        await SeedDrillWithTenUnitsAsync();
        await _store.Drafts.SaveAsync(new OperationDraft(DrillBarcode, OperationDirection.Out, 4, "Acme Ltd", "Site 7"));
        _store.Secrets.Values.Clear();
        var viewModel = new OperationViewModel(_store.Stock, _store.CurrentUser, new OperationDraftStore(
            _store.AppState, new AesFieldEncryptor(_store.Secrets), NullLogger<OperationDraftStore>.Instance),
            _store.Navigation);

        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(_drill.Id, _drill.Name, _drill.Barcode, 10)
        });
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Quantity);
        Assert.Equal("Site 7", viewModel.Reason);
        Assert.Equal(string.Empty, viewModel.Counterparty);
    }

    private async Task SeedDrillWithTenUnitsAsync()
    {
        _drill = await _store.AddProductAsync("Drill", DrillBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = _drill.Id, Direction = OperationDirection.In, Quantity = 10 });
    }

    private void ApplyDrill()
    {
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(_drill.Id, _drill.Name, _drill.Barcode, 10)
        });
    }
}
