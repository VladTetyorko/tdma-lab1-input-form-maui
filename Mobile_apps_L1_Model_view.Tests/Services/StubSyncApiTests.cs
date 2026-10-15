using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// The stub server on its own: the failure switch and the last-write-wins rule
public class StubSyncApiTests
{
    private static readonly DateTime Earlier = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Earlier.AddMinutes(10);

    private static SyncQueueItem Change(Guid id, SyncOperation operation, DateTime at, string payload = "{}") => new()
    {
        EntityType = "Products", EntityId = id, Operation = operation, PayloadJson = payload, CreatedAt = at
    };

    [Fact]
    public async Task Fail_MakesEveryPushThrow_AndStoresNothing()
    {
        var server = new StubSyncApi { Fail = true };

        await Assert.ThrowsAsync<SyncApiException>(() =>
            server.PushAsync(Change(Guid.NewGuid(), SyncOperation.Insert, Earlier)));

        Assert.Empty(server.Records);
    }

    [Fact]
    public async Task AfterFailureIsCleared_ThePushIsAcceptedAgain()
    {
        var server = new StubSyncApi { Fail = true };
        var item = Change(Guid.NewGuid(), SyncOperation.Insert, Earlier);
        await Assert.ThrowsAsync<SyncApiException>(() => server.PushAsync(item));

        server.Fail = false;

        Assert.Equal(SyncPushOutcome.Applied, await server.PushAsync(item));
        Assert.Single(server.Records);
    }

    [Fact]
    public async Task OlderChange_ArrivingAfterANewerOne_IsStale_AndTheNewerStays()
    {
        var server = new StubSyncApi();
        var id = Guid.NewGuid();
        await server.PushAsync(Change(id, SyncOperation.Update, Later, "{\"Name\":\"new\"}"));

        var outcome = await server.PushAsync(Change(id, SyncOperation.Update, Earlier, "{\"Name\":\"old\"}"));

        Assert.Equal(SyncPushOutcome.Stale, outcome);
        var record = Assert.Single(server.Records);
        Assert.Contains("new", record.PayloadJson);
        Assert.Equal(Later, record.ChangedAt);
    }

    [Fact]
    public async Task NewerChange_ReplacesTheOlderOne()
    {
        var server = new StubSyncApi();
        var id = Guid.NewGuid();
        await server.PushAsync(Change(id, SyncOperation.Update, Earlier, "{\"Name\":\"old\"}"));

        var outcome = await server.PushAsync(Change(id, SyncOperation.Update, Later, "{\"Name\":\"new\"}"));

        Assert.Equal(SyncPushOutcome.Applied, outcome);
        Assert.Contains("new", Assert.Single(server.Records).PayloadJson);
    }

    [Fact]
    public async Task Delete_IsKeptAsATombstone()
    {
        var server = new StubSyncApi();
        var id = Guid.NewGuid();
        await server.PushAsync(Change(id, SyncOperation.Insert, Earlier));

        await server.PushAsync(Change(id, SyncOperation.Delete, Later));

        var record = Assert.Single(server.Records);
        Assert.True(record.IsDeleted);
    }
}
