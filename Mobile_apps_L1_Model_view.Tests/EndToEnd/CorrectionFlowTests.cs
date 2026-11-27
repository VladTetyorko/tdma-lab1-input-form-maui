using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Activity -> row -> Correct quantity -> type -> record: two operations, the first one unchanged
public class CorrectionFlowTests
{
    [Fact]
    public async Task TwentyOneBooked_CorrectedToTwelve_IssuesNine_AndCannotBeCorrectedTwiceByMistake()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await DemoAccounts.SignInAsync(services, Role.Storekeeper);
        var catalog = services.GetRequiredService<IProductCatalog>();
        var product = (await catalog.GetSummariesAsync())[0];
        var draft = new StockDocumentDraft(OperationDirection.In);
        draft.Add(product.Barcode, 21);
        Assert.True((await services.GetRequiredService<IStockDocumentService>().CommitAsync(draft)).IsSuccess);

        var activity = services.GetRequiredService<ActivityViewModel>();
        await activity.LoadCommand.ExecuteAsync(null);
        var row = activity.Days[0].Rows[0];
        var preview = string.Empty;
        var action = string.Empty;
        services.GetRequiredService<FakeSheetService>().OnShow = async sheet =>
        {
            var operation = (OperationSheetViewModel)sheet;
            await operation.PrimaryCommand.ExecuteAsync(null);
            Assert.Equal("21", operation.RightQuantity);
            operation.RightQuantity = "12";
            await operation.Previewing;
            (preview, action) = (operation.Preview, operation.PrimaryText);
            await operation.PrimaryCommand.ExecuteAsync(null);
        };

        await activity.OpenCommand.ExecuteAsync(row);

        var number = row.Entry.DocumentNumber;
        Assert.Equal($"Issues 9 units. Note: Correction of {number}", preview);
        Assert.Equal("Issue 9 units", action);
        Assert.Equal(product.Quantity + 12, (await catalog.GetSummaryAsync(product.Id))!.Quantity);
        var operations = await services.GetRequiredService<IOperationRepository>().GetByProductAsync(product.Id);
        Assert.Contains(operations, o => o.Id == row.Entry.Id && o.Quantity == 21);
        Assert.Equal(2, activity.Days.SelectMany(d => d.Rows).Count(r => r.Entry.ProductId == product.Id && r.Entry.DocumentNumber is not null));

        // The same row again: it now stands for 12
        Assert.Equal(12, await services.GetRequiredService<OperationCorrector>().GetBookedAsync(row.Entry));
    }

    [Fact]
    public async Task ADriver_IsRefusedADownwardCorrection_AndNothingIsWritten()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await DemoAccounts.SignInAsync(services, Role.Driver);
        var product = (await services.GetRequiredService<IProductCatalog>().GetSummariesAsync())[0];
        var draft = new StockDocumentDraft(OperationDirection.In);
        draft.Add(product.Barcode, 5);
        Assert.True((await services.GetRequiredService<IStockDocumentService>().CommitAsync(draft)).IsSuccess);
        var entry = (await services.GetRequiredService<IOperationFeed>().GetAsync(new OperationFilter()))[0];
        var before = (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count;

        var result = await services.GetRequiredService<OperationCorrector>().ApplyAsync(entry, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationCorrection.AskStaff, result.Error);
        Assert.Equal(before, (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count);
    }
}
