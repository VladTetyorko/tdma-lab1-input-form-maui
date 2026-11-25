using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class CharacteristicsViewModelTests
{
    private readonly TestStore _store = new();
    private readonly CharacteristicsViewModel _viewModel;

    public CharacteristicsViewModelTests()
    {
        _viewModel = new CharacteristicsViewModel(_store.CharacteristicService, _store.Validator, _store.CurrentUser,
            _store.Guard, _store.Navigation, _store.Sheets);
    }

    private NewCharacteristicViewModel Sheet() => new(_store.CharacteristicService, _store.Validator);

    // The user fills the sheet and taps its main button
    private void Fill(string name, CharacteristicValueType type) => _store.Sheets.OnShow = async sheet =>
    {
        var form = (NewCharacteristicViewModel)sheet;
        form.Name = name;
        form.SelectTypeCommand.Execute(type);
        await FakeSheetService.AcceptAsync(sheet);
    };

    [Fact]
    public async Task Load_ListsExistingCharacteristics()
    {
        await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Color", "Weight (kg)" }, _viewModel.Items.Select(c => c.Name));
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public async Task CanManage_OnlyForManager(Role role, bool expected)
    {
        await _store.SignInAsync(role);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(expected, _viewModel.CanManage);
    }

    [Fact]
    public void Sheet_ChecksTheNameWhileTyping()
    {
        var sheet = Sheet();
        Assert.False(sheet.PrimaryCommand.CanExecute(null));
        Assert.Equal(string.Empty, sheet.Hint);

        sheet.Name = "   ";
        Assert.Equal("Name is required.", sheet.Hint);

        sheet.Name = "Color";
        Assert.Equal(string.Empty, sheet.Hint);
        Assert.True(sheet.PrimaryCommand.CanExecute(null));
        Assert.Equal("Add characteristic", sheet.PrimaryText);
    }

    [Fact]
    public async Task New_AddsTheItemOfTheChosenType()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Fill("Weight (kg)", CharacteristicValueType.Number);

        await _viewModel.NewCommand.ExecuteAsync(null);

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal(("Weight (kg)", CharacteristicValueType.Number), (item.Name, item.ValueType));
    }

    [Fact]
    public async Task Sheet_Duplicate_StaysOpenWithTheError()
    {
        await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SignInAsync(Role.Manager);
        var sheet = Sheet();
        sheet.Name = "color";

        await sheet.PrimaryCommand.ExecuteAsync(null);

        Assert.False(sheet.Accepted);
        Assert.Equal("A characteristic with this name already exists.", sheet.Hint);
        Assert.Null(sheet.Added);
    }

    [Fact]
    public async Task Sheet_ServiceFailure_IsShown()
    {
        var sheet = Sheet();
        sheet.Name = "Color";

        await sheet.PrimaryCommand.ExecuteAsync(null);

        Assert.Equal("Sign in before you add characteristics.", sheet.Hint);
        Assert.Null(sheet.Added);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task New_IsRefusedForOtherRoles(Role role)
    {
        await _store.SignInAsync(role);

        await _viewModel.NewCommand.ExecuteAsync(null);

        Assert.Empty(_store.Sheets.Shown);
        Assert.Equal($"Your role ({role}) cannot add characteristics.", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task GoBack_AfterAdd_SendsCharacteristicsChanged()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Fill("Color", CharacteristicValueType.Text);
        await _viewModel.NewCommand.ExecuteAsync(null);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        var parameters = Assert.Single(_store.Navigation.RequestedParameters)!;
        Assert.Equal(true, parameters[NavigationKeys.CharacteristicsChanged]);
    }

    [Fact]
    public async Task GoBack_WithoutAdd_NavigatesBackWithoutParameters()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Null(Assert.Single(_store.Navigation.RequestedParameters));
    }
}
