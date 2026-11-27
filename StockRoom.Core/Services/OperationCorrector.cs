using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Records the correction of one operation as a one-line document in the opposite (or same) direction, through the same
// service as every receipt and issue, which checks the role and the stock again
public sealed class OperationCorrector(
    IOperationRepository operations,
    IStockDocumentRepository documents,
    IStockDocumentService documentService,
    IStockService stock,
    ICurrentUserService currentUser,
    ILogger<OperationCorrector> logger)
{
    // What the operation stands for now: its quantity and the corrections already recorded for it. Whoever made them
    public async Task<int> GetBookedAsync(OperationEntry entry)
    {
        var note = OperationCorrection.NoteFor(entry);
        var sign = entry.Direction == OperationDirection.In ? 1 : -1;
        var booked = entry.Quantity;
        foreach (var operation in await operations.GetByProductAsync(entry.ProductId))
        {
            if (operation.DocumentId is { } id && (await documents.GetByIdAsync(id))?.Note == note)
            {
                booked += sign * (operation.Direction == OperationDirection.In ? operation.Quantity : -operation.Quantity);
            }
        }

        return booked;
    }

    // Null when the number changes nothing
    public async Task<Correction?> PreviewAsync(OperationEntry entry, int right) =>
        currentUser.CurrentUser is not { } user
            ? null
            : OperationCorrection.For(entry, await GetBookedAsync(entry), right, user.Role,
                await stock.GetQuantityOnHandAsync(entry.ProductId));

    public async Task<Result<StockDocument>> ApplyAsync(OperationEntry entry, int right)
    {
        if (await PreviewAsync(entry, right) is not { } correction)
        {
            return Result<StockDocument>.Failure("Nothing to correct.");
        }

        if (correction.Refusal is { } refusal)
        {
            logger.LogWarning("Correction of operation {OperationId} refused for user {UserId}", entry.Id, currentUser.CurrentUser?.Id);
            return Result<StockDocument>.Failure(refusal);
        }

        var draft = new StockDocumentDraft(correction.Direction) { Note = correction.Note };
        draft.Add(entry.Barcode, correction.Quantity);
        var result = await documentService.CommitAsync(draft);
        if (result.IsSuccess)
        {
            logger.LogInformation("Operation {OperationId} corrected by document {DocumentId}", entry.Id, result.Value!.Id);
        }

        return result;
    }
}
