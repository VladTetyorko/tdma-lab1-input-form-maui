namespace Mobile_apps_L1_Model_view.Services;

// What must survive the app being stopped or killed: who is signed in, where they were and the
// unsaved form drafts. Lives in memory while the app runs; Save() writes it to preferences,
// Restore() reads it back on the next start.
public interface IAppStateService
{
    // The signed-in user (null = nobody)
    Guid? SignedInUserId { get; set; }

    // Leaf route of the page that was open when the state was saved (see Routes.Leaf)
    string? LastRoute { get; set; }

    // One draft per draft type; a draft is a plain record that System.Text.Json can round-trip
    T? GetDraft<T>() where T : class;

    void SetDraft<T>(T draft) where T : class;

    void ClearDraft<T>() where T : class;

    // Writes everything to preferences
    void Save();

    // Reads everything from preferences (replaces what is in memory) and remembers LastRoute as the
    // route to resume; call it once, at process start
    void Restore();

    // Route of the page to reopen after Restore(); returned once, then null. Null after a warm start
    string? TakeResumeRoute();

    // Forgets user, route and drafts, in memory and in preferences (sign-out)
    void Clear();
}
