namespace Mobile_apps_L1_Model_view.Services;

// Opens the screen a stockroom:// link points at
public interface IDeepLinkHandler
{
    // The app is running: open the product now. Not signed in = remember the link until sign-in;
    // unknown barcode or unsupported link = a friendly message
    Task HandleAsync(string? link);

    // The app is starting and no page exists yet: remember the link until the product list is shown
    void Defer(string? link);

    // Opens the remembered link, if there is one. True when there was one
    Task<bool> HandlePendingAsync();
}
