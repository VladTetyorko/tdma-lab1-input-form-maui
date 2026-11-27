using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class StockDocumentService(
    IStockDocumentRepository documents,
    IProductRepository products,
    IStockService stock,
    ICurrentUserService currentUser,
    IClock clock,
    StockChanges changes,
    ILogger<StockDocumentService> logger) : IStockDocumentService
{
    public const string NotSaved = "Not saved, nothing was recorded. Try again.";
    public const int RecentNames = 3;

    public async Task<Result<StockDocument>> CommitAsync(StockDocumentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var result = await CommitManyAsync([draft]);
        return result.IsSuccess ? Result<StockDocument>.Success(result.Value![0]) : Result<StockDocument>.Failure(result.Error!);
    }

    public async Task<Result<IReadOnlyList<StockDocument>>> CommitManyAsync(IReadOnlyList<StockDocumentDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            return Refuse("Sign in before recording operations.", "nobody is signed in");
        }

        var checkedDrafts = new List<(StockDocumentDraft Draft, List<(Product Product, int Quantity)> Lines)>();
        foreach (var draft in drafts)
        {
            if (OperationRules.RoleRefusal(user.Role, draft.Direction) is { } refusal)
            {
                return Refuse(refusal, "the role may not record this direction");
            }

            if (draft.Lines.Count == 0 || draft.Lines.Any(line => line.Quantity <= 0))
            {
                return Refuse("Add at least one product with a quantity above zero.", "no lines or a quantity of zero");
            }

            var lines = new List<(Product Product, int Quantity)>();
            foreach (var group in draft.Lines.GroupBy(line => line.Barcode))
            {
                if (await products.GetByBarcodeAsync(group.Key) is not { } product)
                {
                    return Refuse($"No product has barcode {group.Key}. Create it or remove the line.", "unknown barcode");
                }

                var quantity = group.Sum(line => line.Quantity);
                if (draft.Direction == OperationDirection.Out)
                {
                    // Read now, not when the line was scanned
                    var onHand = await stock.GetQuantityOnHandAsync(product.Id);
                    if (OperationRules.ExceedsStock(draft.Direction, quantity, onHand))
                    {
                        return Refuse($"Only {onHand} of {product.Name} on hand.", "not enough on hand");
                    }
                }

                lines.Add((product, quantity));
            }

            checkedDrafts.Add((draft, lines));
        }

        return await SaveAsync(user, checkedDrafts);
    }

    public async Task<IReadOnlyList<string>> GetRecentCounterpartiesAsync(OperationDirection direction)
    {
        if (currentUser.CurrentUser is not { } user || !RolePermissions.CanRecord(user.Role, direction))
        {
            logger.LogWarning("Recent {Direction} names refused for user {UserId}", direction, currentUser.CurrentUser?.Id);
            return [];
        }

        try
        {
            return await documents.GetRecentCounterpartiesAsync(direction, RecentNames,
                RolePermissions.CanViewAllOperations(user.Role) ? null : user.Id);
        }
        catch (Exception ex)
        {
            // The chips are a help: the review works without them
            logger.LogWarning(ex, "Recent {Direction} names could not be read", direction);
            return [];
        }
    }

    private async Task<Result<IReadOnlyList<StockDocument>>> SaveAsync(
        User user, List<(StockDocumentDraft Draft, List<(Product Product, int Quantity)> Lines)> drafts)
    {
        var now = clock.UtcNow;
        try
        {
            var batch = new List<(StockDocument Document, IReadOnlyList<Operation> Operations)>();
            foreach (var (draft, lines) in drafts)
            {
                var prefix = DocumentNumberGenerator.Prefix(draft.Direction, now);
                var taken = await documents.CountByNumberPrefixAsync(prefix)
                            + batch.Count(item => item.Document.Direction == draft.Direction);
                var document = new StockDocument
                {
                    Number = DocumentNumberGenerator.Number(prefix, taken),
                    Direction = draft.Direction,
                    Counterparty = Blank(draft.Counterparty),
                    Note = Blank(draft.Note),
                    CreatedByUserId = user.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                batch.Add((document, lines.Select(line => new Operation
                {
                    ProductId = line.Product.Id,
                    DocumentId = document.Id,
                    Direction = draft.Direction,
                    Quantity = line.Quantity,
                    CreatedByUserId = user.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList()));
            }

            await documents.AddAsync(batch);
            changes.Mark();
            foreach (var (document, operations) in batch)
            {
                logger.LogInformation("Committed {Direction} document {DocumentId} with {LineCount} lines, {Units} units by user {UserId}",
                    document.Direction, document.Id, operations.Count, operations.Sum(o => o.Quantity), user.Id);
            }

            return Result<IReadOnlyList<StockDocument>>.Success([.. batch.Select(item => item.Document)]);
        }
        catch (EncryptionException ex)
        {
            // Personal data is never stored unprotected (and never logged)
            logger.LogError(ex, "A document was not saved: a field could not be encrypted");
            return Result<IReadOnlyList<StockDocument>>.Failure($"Not saved. {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A {Direction} document was not saved", drafts[0].Draft.Direction);
            return Result<IReadOnlyList<StockDocument>>.Failure(NotSaved);
        }
    }

    private Result<IReadOnlyList<StockDocument>> Refuse(string message, string reason)
    {
        logger.LogWarning("Document refused for user {UserId} (role {Role}): {Reason}",
            currentUser.CurrentUser?.Id, currentUser.CurrentRole, reason);
        return Result<IReadOnlyList<StockDocument>>.Failure(message);
    }

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
