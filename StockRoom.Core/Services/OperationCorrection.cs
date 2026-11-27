using System.Globalization;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// What has to be recorded so that an operation's quantity becomes the right one. Refusal: why it may not be recorded
public sealed record Correction(OperationDirection Direction, int Quantity, string Note, string? Refusal = null)
{
    public string Action => $"{(Direction == OperationDirection.In ? "Receive" : "Issue")} {Plural.Units(Quantity)}";
}

// Operations are immutable: a wrong quantity is corrected by a new operation of the difference, linked by its note
public static class OperationCorrection
{
    public const string AskStaff = "Ask a storekeeper or the manager to correct this.";

    // "Correction of R-20261125-0003"; an operation without a document is named by its local time
    public static string NoteFor(OperationEntry entry) =>
        "Correction of " + (entry.DocumentNumber
                            ?? "operation of " + DateTime.SpecifyKind(entry.At, DateTimeKind.Utc).ToLocalTime()
                                .ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture));

    // booked: what the operation stands for now (with earlier corrections); null when there is nothing to record
    public static Correction? For(OperationEntry entry, int booked, int right, Role role, int onHand)
    {
        if (right < 0 || right == booked)
        {
            return null;
        }

        // More received adds stock; more issued takes stock away
        var effect = (entry.Direction == OperationDirection.In ? 1 : -1) * (right - booked);
        var direction = effect > 0 ? OperationDirection.In : OperationDirection.Out;
        var quantity = Math.Abs(effect);
        var refusal = !RolePermissions.CanCorrect(role, direction) ? AskStaff
            : OperationRules.ExceedsStock(direction, quantity, onHand)
                ? $"Only {onHand} on hand. Ask the manager to count {entry.ProductName}."
                : null;
        return new Correction(direction, quantity, NoteFor(entry), refusal);
    }
}
