using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services.Lookup;

// One GET returning JSON. Network problems become Offline, bad answers become Failed: the app never crashes because
// of a database. A subclass says which address to ask and how to read the answer
public abstract class HttpLookupSource(HttpClient http, ILogger logger) : ILookupSource
{
    public abstract string Name { get; }

    protected abstract string Attribution { get; }

    public virtual bool Covers(string barcode) => true;

    public async Task<LookupResult> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync(Address(barcode), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return LookupResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Source} answered {StatusCode} for {Barcode}", Name, (int)response.StatusCode, barcode);
                return LookupResult.Failed();
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return Complete(Read(document.RootElement, barcode));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "{Source} is not reachable for {Barcode}", Name, barcode);
            return LookupResult.Offline();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation
            logger.LogWarning(ex, "{Source} timed out for {Barcode}", Name, barcode);
            return LookupResult.Offline();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            // Not JSON, or JSON of another shape than documented
            logger.LogWarning(ex, "{Source} sent unreadable data for {Barcode}", Name, barcode);
            return LookupResult.Failed();
        }
    }

    protected abstract Uri Address(string barcode);

    // NotFound, or Found without Source and Attribution (added here)
    protected abstract LookupResult Read(JsonElement root, string barcode);

    protected static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!.Trim()
            : string.Empty;

    // The name is cut to the longest one a product may have
    private LookupResult Complete(LookupResult result)
    {
        if (!result.IsFound)
        {
            return result;
        }

        var name = result.Name.Length > ProductValidator.MaxNameLength
            ? result.Name[..ProductValidator.MaxNameLength].TrimEnd()
            : result.Name;
        return result with { Name = name, Source = Name, Attribution = Attribution };
    }
}
