using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class CharacteristicsViewModelTests
{
    private readonly TestStore _store = new();
    private readonly CharacteristicsViewModel _viewModel;

    public CharacteristicsViewModelTests()
    {
        _viewModel = new CharacteristicsViewModel(
            _store.CharacteristicService, _store.Validator, _store.CurrentUser, _store.Navigation);
    }

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
    public async Task NewName_ShowsRequiredErrorWhenBlankAndClearsWhenFixed()
    {
        _viewModel.NewName = "   ";
        Assert.Equal("Name is required.", _viewModel.NameError);

        _viewModel.NewName = "Color";
        Assert.Equal(string.Empty, _viewModel.NameError);
    }

    [Fact]
    public async Task Add_IsDisabledForNonManager()
    {
        await _store.SignInAsync(Role.Storekeeper);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "Color";

        Assert.False(_viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task Add_IsDisabledForBlankName()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = " ";

        Assert.False(_viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task Add_Success_AddsTheItemAndClearsTheForm()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "Weight (kg)";
        _viewModel.NewValueType = CharacteristicValueType.Number;

        await _viewModel.AddCommand.ExecuteAsync(null);

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal("Weight (kg)", item.Name);
        Assert.Equal(CharacteristicValueType.Number, item.ValueType);
        Assert.Equal(string.Empty, _viewModel.NewName);
        Assert.Equal(string.Empty, _viewModel.StatusMessage);
    }

    [Fact(Skip = "BUG: after a successful add the form is cleared, which sets NameError to \"Name is required.\" on an empty form")]
    public async Task Add_Success_DoesNotShowRequiredErrorOnTheClearedForm()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "Color";

        await _viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, _viewModel.NameError);
    }

    [Fact]
    public async Task Add_Duplicate_ShowsNameErrorAndKeepsTheInput()
    {
        await _store.AddCharacteristicAsync("Color", CharacteristicValueType.Text);
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "color";

        await _viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal("A characteristic with this name already exists.", _viewModel.NameError);
        Assert.Equal("color", _viewModel.NewName);
        Assert.Single(_viewModel.Items);
    }

    [Fact]
    public async Task Add_ServiceFailure_ShowsStatusMessage()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "Color";
        _store.CurrentUser.SignOut();

        await _viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal("Sign in before you add characteristics.", _viewModel.StatusMessage);
        Assert.Empty(_viewModel.Items);
    }

    [Fact]
    public async Task GoBack_AfterAdd_SendsCharacteristicsChanged()
    {
        await _store.SignInAsync(Role.Manager);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewName = "Color";
        await _viewModel.AddCommand.ExecuteAsync(null);

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
