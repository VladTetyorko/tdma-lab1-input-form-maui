using System.Globalization;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The stock count (owner decision, design revision 4): the differences are posted as ordinary documents, one receipt
// for what was found extra and one issue for what is missing, both in one transaction. Uncounted products are untouched
public sealed class StockCountService(
    IProductCatalog catalog,
    IStockDocumentService documents,
    ICurrentUserService currentUser,
    IClock clock,
    ILogger<StockCountService> logger)
{
    public string Note => "Stock count " + clock.UtcNow.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    public async Task<StockCountPlan> PlanAsync(StockCountDraft draft)
    {
        var products = await catalog.GetSummariesAsync();
        var lines = products
            .Where(product => draft.Counts.ContainsKey(product.Id))
            .OrderBy(product => product.Name)
            .Select(product =>
            {
                var counted = draft.Counts[product.Id];
                return new StockCountLine(product, product.Quantity, counted.Counted,
                    counted.OnHandWhenCounted != product.Quantity);
            })
            .ToList();
        return new StockCountPlan(lines, products.Count - lines.Count);
    }

    // The documents that were written (none when everything matched)
    public async Task<Result<IReadOnlyList<StockDocument>>> PostAsync(StockCountDraft draft)
    {
        if (currentUser.CurrentUser is not { } user || !RolePermissions.CanCountStock(user.Role))
        {
            logger.LogWarning("Stock count refused for user {UserId} (role {Role})",
                currentUser.CurrentUser?.Id, currentUser.CurrentRole);
            return Result<IReadOnlyList<StockDocument>>.Failure("Only the manager can post a stock count.");
        }

        // Computed now, not when the counting was done
        var plan = await PlanAsync(draft);
        var drafts = new List<StockDocumentDraft>();
        foreach (var direction in new[] { OperationDirection.In, OperationDirection.Out })
        {
            var document = new StockDocumentDraft(direction) { Note = Note };
            foreach (var line in plan.Corrections.Where(line => line.Difference > 0 == (direction == OperationDirection.In)))
            {
                document.Add(line.Product.Barcode, Math.Abs(line.Difference));
            }

            if (document.Lines.Count > 0)
            {
                drafts.Add(document);
            }
        }

        if (drafts.Count == 0)
        {
            logger.LogInformation("Stock count finished by user {UserId}: nothing to correct", user.Id);
            return Result<IReadOnlyList<StockDocument>>.Success([]);
        }

        var result = await documents.CommitManyAsync(drafts);
        if (result.IsSuccess)
        {
            logger.LogInformation("Stock count posted: {In} in, {Out} out, documents {Numbers}",
                plan.Corrections.Count(line => line.Difference > 0), plan.Corrections.Count(line => line.Difference < 0),
                string.Join(" ", result.Value!.Select(document => document.Number)));
        }

        return result;
    }
}
