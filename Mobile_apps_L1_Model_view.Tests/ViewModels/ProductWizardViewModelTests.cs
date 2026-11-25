using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class ProductWizardViewModelTests
{
    private const string Barcode = "3017620422003";

    private readonly TestStore _store = new();

    private async Task<ProductWizardViewModel> OpenAsync(
        Role role = Role.Manager, IDictionary<string, object>? query = null)
    {
        await _store.SignInAsync(role);
        var wizard = new ProductWizardViewModel(_store.ProductForm, _store.CharacteristicService, _store.Validator, _store.Sheets);
        if (query is not null)
        {
            wizard.ApplyQueryAttributes(query);
        }

        await wizard.AppearCommand.ExecuteAsync(null);
        return wizard;
    }

    private static Dictionary<string, object> WithCode => new() { [NavigationKeys.Barcode] = Barcode };

    [Fact]
    public async Task Next_IsDisabledWithAReason()
    {
        var wizard = await OpenAsync();
        Assert.Equal((1, "Next: name and photo", "Scan or type the barcode"), (wizard.Step, wizard.NextText, wizard.Hint));
        Assert.False(wizard.NextCommand.CanExecute(null));

        wizard.Form.Barcode = "123";
        Assert.False(wizard.NextCommand.CanExecute(null));
        Assert.NotEqual(string.Empty, wizard.Hint);

        wizard.Form.Barcode = Barcode;
        await wizard.NextCommand.ExecuteAsync(null);
        Assert.Equal((2, "Enter a name"), (wizard.Step, wizard.Hint));
    }

    [Fact]
    public async Task ACodeThatIsInTheCatalogue_CannotGoOn()
    {
        await _store.AddProductAsync("Nutella", Barcode);
        var wizard = await OpenAsync();

        wizard.Form.Barcode = Barcode;

        Assert.False(wizard.NextCommand.CanExecute(null));
        Assert.Equal("This code is already in the catalogue", wizard.Hint);
        Assert.True(wizard.Form.HasDuplicate);
    }

    [Fact]
    public async Task AKnownCode_SkipsStepOne_AndOpenFoodFactsFillsNameAndDetails()
    {
        await _store.AddCharacteristicAsync("Weight (kg)", CharacteristicValueType.Number);
        await _store.AddCharacteristicAsync("Brand", CharacteristicValueType.Text);
        await _store.AddCharacteristicAsync("Colour", CharacteristicValueType.Text);
        _store.Lookup.Result = LookupResult.Found("Nutella", "Ferrero", "400 g", "Spreads");

        var wizard = await OpenAsync(query: WithCode);

        Assert.Equal((2, "Nutella"), (wizard.Step, wizard.Form.Name));
        await wizard.NextCommand.ExecuteAsync(null);
        Assert.Equal((3, "Add product"), (wizard.Step, wizard.NextText));
        Assert.Equal([0.4m.ToString(), "Ferrero", ""], wizard.Details.Select(d => d.Value));
    }

    [Fact]
    public async Task Add_SavesTheProductWithItsValues_AndReturns()
    {
        await _store.AddCharacteristicAsync("Brand", CharacteristicValueType.Text);
        var wizard = await OpenAsync(query: WithCode);
        wizard.Form.Name = "Nutella";
        await wizard.NextCommand.ExecuteAsync(null);
        wizard.Details[0].Value = "Ferrero";

        await wizard.NextCommand.ExecuteAsync(null);

        var product = await _store.Products.GetByBarcodeAsync(Barcode);
        Assert.NotNull(product);
        Assert.Equal("Ferrero", (await _store.Values.GetByProductAsync(product.Id)).Single().Value);
        Assert.Equal(Routes.Back, _store.Navigation.RequestedRoutes.Last());
        var summary = (ProductSummary)_store.Navigation.RequestedParameters.Last()![NavigationKeys.UpdatedProduct];
        Assert.Equal("Ferrero", summary.Values.Values.Single());
    }

    [Fact]
    public async Task Back_KeepsWhatWasTyped_AndOnTheFirstStepAsksBeforeDiscarding()
    {
        var wizard = await OpenAsync();
        wizard.Form.Barcode = Barcode;
        await wizard.NextCommand.ExecuteAsync(null);
        wizard.Form.Name = "Nutella";

        await wizard.BackCommand.ExecuteAsync(null);
        Assert.Equal((1, "Nutella", Barcode), (wizard.Step, wizard.Form.Name, wizard.Form.Barcode));
        Assert.Empty(_store.Navigation.RequestedRoutes);

        _store.Confirm.Answer = false;
        await wizard.BackCommand.ExecuteAsync(null);
        Assert.Single(_store.Confirm.Asked);
        Assert.Empty(_store.Navigation.RequestedRoutes);

        _store.Confirm.Answer = true;
        await wizard.BackCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Back, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task ANewCharacteristic_AppearsInStepThree()
    {
        var wizard = await OpenAsync(query: WithCode);
        _store.Sheets.OnShow = async sheet =>
        {
            ((NewCharacteristicViewModel)sheet).Name = "Volume (l)";
            await FakeSheetService.AcceptAsync(sheet);
        };

        await wizard.NewCharacteristicCommand.ExecuteAsync(null);

        Assert.Equal("Volume (l)", Assert.Single(wizard.Details).Name);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task OnlyTheManagerCanAdd(Role role)
    {
        var wizard = await OpenAsync(role, WithCode);
        wizard.Form.Name = "Nutella";
        await wizard.NextCommand.ExecuteAsync(null);

        Assert.False(wizard.NextCommand.CanExecute(null));
        Assert.Null(await _store.Products.GetByBarcodeAsync(Barcode));
    }
}
