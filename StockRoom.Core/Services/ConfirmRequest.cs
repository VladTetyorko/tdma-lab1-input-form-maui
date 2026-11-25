namespace Mobile_apps_L1_Model_view.Services;

// A question before something that cannot be taken back. Lines say what exactly will happen
public sealed record ConfirmRequest(string Title, string Message, string Accept, string Cancel)
{
    public IReadOnlyList<string> Lines { get; init; } = [];

    // The action throws something away: its button is red
    public bool IsDestructive { get; init; }
}
