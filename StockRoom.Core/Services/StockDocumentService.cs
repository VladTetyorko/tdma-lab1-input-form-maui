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

    public async Task<Result<StockDocument>> CommitAsync(StockDocumentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var user = currentUser.CurrentUser;
        if (user is null)
        {
            return Refuse("Sign in before recording operations.", "nobody is signed in");
        }

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

        return await SaveAsync(draft, user, lines);
    }

    private async Task<Result<StockDocument>> SaveAsync(
        StockDocumentDraft draft, User user, List<(Product Product, int Quantity)> lines)
    {
        var now = clock.UtcNow;
        var prefix = DocumentNumberGenerator.Prefix(draft.Direction, now);
        try
        {
            var document = new StockDocument
            {
                Number = DocumentNumberGenerator.Number(prefix, await documents.CountByNumberPrefixAsync(prefix)),
                Direction = draft.Direction,
                Counterparty = Blank(draft.Counterparty),
                Note = Blank(draft.Note),
                CreatedByUserId = user.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            var operations = lines.Select(line => new Operation
            {
                ProductId = line.Product.Id,
                DocumentId = document.Id,
                Direction = draft.Direction,
                Quantity = line.Quantity,
                CreatedByUserId = user.Id,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            await documents.AddAsync(document, operations);
            changes.Mark();
            logger.LogInformation("Committed {Direction} document {DocumentId} with {LineCount} lines, {Units} units by user {UserId}",
                draft.Direction, document.Id, operations.Count, operations.Sum(o => o.Quantity), user.Id);
            return Result<StockDocument>.Success(document);
        }
        catch (EncryptionException ex)
        {
            // Personal data is never stored unprotected (and never logged)
            logger.LogError(ex, "A {Direction} document was not saved: a field could not be encrypted", draft.Direction);
            return Result<StockDocument>.Failure($"Not saved. {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A {Direction} document was not saved", draft.Direction);
            return Result<StockDocument>.Failure(NotSaved);
        }
    }

    private Result<StockDocument> Refuse(string message, string reason)
    {
        logger.LogWarning("Document refused for user {UserId} (role {Role}): {Reason}",
            currentUser.CurrentUser?.Id, currentUser.CurrentRole, reason);
        return Result<StockDocument>.Failure(message);
    }

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
