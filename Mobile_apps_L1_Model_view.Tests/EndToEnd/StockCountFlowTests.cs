using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

public class StockCountFlowTests
{
    // Count three products, post: the product list shows exactly what was counted
    [Fact]
    public async Task ManagerCountsAndPosts_TheStockIsWhatWasCounted()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await DemoAccounts.SignInAsync(services, Role.Manager);
        var count = services.GetRequiredService<StockCountViewModel>();
        await count.AppearCommand.ExecuteAsync(null);
        var (more, less, same) = (count.Rows[0], count.Rows[1], count.Rows[2]);
        more.CountedText = (more.OnHand + 2).ToString();
        less.CountedText = (less.OnHand - 1).ToString();
        same.CountedText = same.OnHand.ToString();

        await count.ReviewCommand.ExecuteAsync(null);

        var now = (await services.GetRequiredService<IProductCatalog>().GetSummariesAsync()).ToDictionary(p => p.Id, p => p.Quantity);
        Assert.Equal((more.OnHand + 2, less.OnHand - 1, same.OnHand),
            (now[more.Product.Id], now[less.Product.Id], now[same.Product.Id]));
        Assert.Equal(2, (await services.GetRequiredService<IStockDocumentRepository>().GetRecentAsync(5)).Count);
    }

    // Both documents of a count are one transaction: when the second cannot be written, neither is
    [Fact]
    public async Task ABatchThatFails_WritesNoDocumentAtAll()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await DemoAccounts.SignInAsync(services, Role.Manager);
        var repository = services.GetRequiredService<IStockDocumentRepository>();
        var product = (await services.GetRequiredService<IProductCatalog>().GetSummariesAsync())[0];
        var user = services.GetRequiredService<ICurrentUserService>().CurrentUser!.Id;

        (StockDocument, IReadOnlyList<Operation>) Document(string number)
        {
            var document = new StockDocument { Number = number, Direction = OperationDirection.In, CreatedByUserId = user };
            return (document,
            [
                new Operation
                {
                    ProductId = product.Id, DocumentId = document.Id, Direction = OperationDirection.In, Quantity = 1,
                    CreatedByUserId = user
                }
            ]);
        }

        // The numbers are unique: the second insert fails
        await Assert.ThrowsAnyAsync<Exception>(() => repository.AddAsync([Document("R-X-0001"), Document("R-X-0001")]));

        Assert.Empty(await repository.GetRecentAsync(5));
        Assert.Equal(product.Quantity, (await services.GetRequiredService<IProductCatalog>().GetSummaryAsync(product.Id))!.Quantity);
    }
}
