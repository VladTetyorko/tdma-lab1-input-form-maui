using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The real server side of synchronisation: one POST per queued change to {BaseUrl}sync/changes.
// 200/201/204 = stored, 409 = the server has a newer version (last write wins), anything else is an error and the
// item stays queued. The HttpClient comes from the host: with a PinnedHandlerFactory handler when a server is configured.
// Never logs the payload, only ids and status codes
public sealed class HttpSyncApi(HttpClient http, ILogger<HttpSyncApi> logger) : ISyncApi
{
    public const string Path = "sync/changes";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task<SyncPushOutcome> PushAsync(SyncQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var change = new SyncChange(item.EntityType, item.EntityId, item.Operation.ToString(), item.PayloadJson, item.CreatedAt);
        try
        {
            using var response = await http.PostAsJsonAsync(Path, change, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogInformation("The server has a newer {EntityType} {EntityId}: change ignored", item.EntityType, item.EntityId);
                return SyncPushOutcome.Stale;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The sync server answered {StatusCode} for {EntityType} {EntityId}",
                    (int)response.StatusCode, item.EntityType, item.EntityId);
                throw new SyncApiException($"The server answered {(int)response.StatusCode}.");
            }

            return SyncPushOutcome.Applied;
        }
        catch (HttpRequestException ex)
        {
            // A certificate that does not match the pins also ends up here (an AuthenticationException inside)
            logger.LogWarning(ex, "The sync server could not be reached or its certificate was refused");
            throw new SyncApiException("The server could not be reached or its certificate was not accepted.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The sync server did not answer in time");
            throw new SyncApiException("The server did not answer in time.", ex);
        }
    }

    private sealed record SyncChange(string EntityType, Guid EntityId, string Operation, string PayloadJson, DateTime ChangedAt);
}
