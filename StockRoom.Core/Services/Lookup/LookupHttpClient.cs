namespace Mobile_apps_L1_Model_view.Services.Lookup;

// The one HttpClient of all open databases: a short timeout per request and a User-Agent without personal data.
// No certificate pinning: these are third-party hosts
public static class LookupHttpClient
{
    public const string UserAgent = "StockRoom/1.0 (teaching project)";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static HttpClient Create(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false) { Timeout = Timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }
}
