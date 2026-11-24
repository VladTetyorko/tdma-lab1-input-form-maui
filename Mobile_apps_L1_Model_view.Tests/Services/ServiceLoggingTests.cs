using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// The logging rules of AGENTS.md section 2a as tests: role denied and rejected input are Warnings,
// success is Information, and personal data never reaches a log message
public class ServiceLoggingTests
{
    private const string Counterparty = "Zyzzyva Trading GmbH";
    private const string Barcode = "4006381333931";

    private readonly TestStore _store = new();

    private LogRecord Single(LogLevel level, string category) =>
        Assert.Single(_store.Logs, r => r.Level == level && r.Category == category);

    [Fact]
    public async Task Stock_DriverRecordingOut_LogsAWarningWithTheRole()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", Barcode);

        var result = await _store.Stock.RecordAsync(product.Id, OperationDirection.Out, 1);

        Assert.False(result.IsSuccess);
        var warning = Single(LogLevel.Warning, "StockService");
        Assert.Contains("Driver", warning.Message);
        Assert.Contains(product.Id.ToString(), warning.Message);
    }

    [Fact]
    public async Task Stock_Success_LogsInformationWithIdsAndQuantity()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var product = await _store.AddProductAsync("Drill", Barcode);

        var result = await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 4);

        var info = Single(LogLevel.Information, "StockService");
        Assert.Contains(result.Value!.Id.ToString(), info.Message);
        Assert.Contains("4", info.Message);
    }

    [Fact]
    public async Task Stock_RejectedQuantity_LogsAWarning()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var product = await _store.AddProductAsync("Drill", Barcode);

        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 0);

        Single(LogLevel.Warning, "StockService");
    }

    [Fact]
    public async Task NoLogMessageEverContainsTheCounterparty()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var product = await _store.AddProductAsync("Drill", Barcode);
        var viewModel = new OperationViewModel(_store.Stock, _store.CurrentUser, _store.Drafts, _store.Navigation,
            _store.Logger<OperationViewModel>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = new ProductSummary(product.Id, product.Name, product.Barcode, 0)
        });
        await viewModel.LoadCommand.ExecuteAsync(null);

        // A refused Out, a rejected quantity and two saved operations, all with the counterparty typed in
        foreach (var (direction, quantity) in new[]
                 {
                     (OperationDirection.Out, 5), (OperationDirection.In, 0),
                     (OperationDirection.In, 3), (OperationDirection.Out, 1)
                 })
        {
            viewModel.SelectedDirection = direction;
            viewModel.Quantity = quantity;
            viewModel.Counterparty = Counterparty;
            await viewModel.SaveCommand.ExecuteAsync(null);
        }

        Assert.NotEmpty(_store.Logs);
        Assert.All(_store.Logs, record =>
        {
            Assert.DoesNotContain(Counterparty, record.Message);
            Assert.DoesNotContain(Counterparty, record.Exception?.ToString() ?? string.Empty);
        });
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task Product_AddingWithoutThePermission_LogsAWarning(Role role)
    {
        await _store.SignInAsync(role);

        await _store.ProductService.AddAsync("Saw", Barcode);

        Single(LogLevel.Warning, "ProductService");
        Assert.DoesNotContain(_store.Logs, r => r.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Product_AddRejectedByValidation_LogsAWarning()
    {
        await _store.SignInAsync(Role.Manager);

        await _store.ProductService.AddAsync("  ", Barcode);

        Single(LogLevel.Warning, "ProductService");
    }

    [Fact]
    public async Task Product_AddSucceeded_LogsInformationWithTheBarcode()
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _store.ProductService.AddAsync("Saw", Barcode);

        var info = Single(LogLevel.Information, "ProductService");
        Assert.Contains(Barcode, info.Message);
        Assert.Contains(result.Value!.Id.ToString(), info.Message);
    }

    [Fact]
    public async Task Characteristic_RoleDenied_Rejected_Success_UseWarningWarningInformation()
    {
        await _store.SignInAsync(Role.Storekeeper);
        await _store.CharacteristicService.AddAsync("Color", CharacteristicValueType.Text);
        Single(LogLevel.Warning, "CharacteristicService");

        _store.Logs.Clear();
        await _store.SignInAsync(Role.Manager);
        await _store.CharacteristicService.AddAsync("  ", CharacteristicValueType.Text);
        Single(LogLevel.Warning, "CharacteristicService");

        _store.Logs.Clear();
        await _store.CharacteristicService.AddAsync("Color", CharacteristicValueType.Text);
        Single(LogLevel.Information, "CharacteristicService");
    }
}
