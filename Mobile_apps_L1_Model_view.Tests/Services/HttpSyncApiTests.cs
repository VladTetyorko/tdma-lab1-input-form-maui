using System.Net;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class HttpSyncApiTests
{
    private readonly FakeHttpMessageHandler _network = new();
    private readonly List<LogRecord> _logs = [];
    private readonly HttpSyncApi _api;

    private static readonly SyncQueueItem Item = new()
    {
        EntityType = "Products", EntityId = Guid.NewGuid(), Operation = SyncOperation.Insert,
        PayloadJson = "{\"Name\":\"Secret Counterparty Ltd\"}"
    };

    public HttpSyncApiTests()
    {
        var client = new HttpClient(_network) { BaseAddress = new Uri("https://sync.example.test/") };
        _api = new HttpSyncApi(client, new CapturingLogger<HttpSyncApi>(_logs));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task Push_SuccessStatus_IsApplied_AndPostsTheChangeAsJson(HttpStatusCode status)
    {
        _network.RespondWith(status, "{}");

        Assert.Equal(SyncPushOutcome.Applied, await _api.PushAsync(Item));

        var request = Assert.Single(_network.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://sync.example.test/sync/changes", request.RequestUri!.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains(Item.EntityId.ToString(), body);
        Assert.Contains("\"entityType\":\"Products\"", body);
    }

    [Fact]
    public async Task Push_Conflict_IsStale()
    {
        _network.RespondWith(HttpStatusCode.Conflict, "{}");

        Assert.Equal(SyncPushOutcome.Stale, await _api.PushAsync(Item));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Push_OtherStatus_ThrowsSyncApiException_AndLogsAWarningWithoutThePayload(HttpStatusCode status)
    {
        _network.RespondWith(status, "{}");

        await Assert.ThrowsAsync<SyncApiException>(() => _api.PushAsync(Item));

        Assert.Contains(_logs, l => l.Level == LogLevel.Warning);
        Assert.All(_logs, l => Assert.DoesNotContain("Secret Counterparty", l.Message));
    }

    [Fact]
    public async Task Push_NetworkOrCertificateFailure_ThrowsSyncApiException()
    {
        _network.Throw(new HttpRequestException("The SSL connection could not be established."));

        var ex = await Assert.ThrowsAsync<SyncApiException>(() => _api.PushAsync(Item));

        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task Push_Timeout_ThrowsSyncApiException()
    {
        _network.Throw(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<SyncApiException>(() => _api.PushAsync(Item));
    }

    [Fact]
    public async Task Push_CancelledByTheCaller_IsNotWrapped()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _network.Throw(new TaskCanceledException("cancelled", null, cts.Token));

        await Assert.ThrowsAsync<TaskCanceledException>(() => _api.PushAsync(Item, cts.Token));
    }
}
