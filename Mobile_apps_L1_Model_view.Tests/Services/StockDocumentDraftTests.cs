using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class StockDocumentDraftTests
{
    private const string Drill = "4006381333931";
    private const string Saw = "4820001000016";

    private readonly StockDocumentDraft _draft = new(OperationDirection.In);

    [Fact]
    public void Add_SameBarcodeAgain_IncrementsAndMovesToTheTop()
    {
        _draft.Add(Drill);
        _draft.Add(Saw);

        _draft.Add(Drill);

        Assert.Equal([new DraftLine(Drill, 2), new DraftLine(Saw, 1)], _draft.Lines);
        Assert.Equal(3, _draft.TotalUnits);
    }

    [Fact]
    public void SetQuantity_ZeroRemovesTheLine()
    {
        _draft.Add(Drill);

        _draft.SetQuantity(Drill, 0);

        Assert.Empty(_draft.Lines);
    }

    [Fact]
    public void Restore_PutsTheRemovedLineBackWhereItWas()
    {
        _draft.Add(Saw);
        _draft.Add(Drill, 4);
        var removed = _draft.Remove(Drill)!.Value;

        _draft.Restore(removed.Line, removed.Index);

        Assert.Equal([new DraftLine(Drill, 4), new DraftLine(Saw, 1)], _draft.Lines);
        Assert.Null(_draft.Remove("unknown"));
    }

    [Fact]
    public void Debouncer_IgnoresTheSameCodeWhileItStaysInView()
    {
        var clock = new FakeClock();
        var debouncer = new ScanDebouncer(clock);

        Assert.True(debouncer.Accept(Drill));
        clock.UtcNow = clock.UtcNow.AddSeconds(1);
        Assert.False(debouncer.Accept(Drill));
        Assert.True(debouncer.Accept(Saw));
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        Assert.True(debouncer.Accept(Saw));
    }
}

public class StockDocumentDraftStoreTests
{
    private const string Drill = "4006381333931";

    private readonly FakePreferencesStore _preferences = new();
    private readonly FakeSecretStore _secrets = new();
    private readonly CapturingLogger<StockDocumentDraftStore> _logger = new();

    private (StockDocumentDraftStore Store, AppStateService State) Create()
    {
        var state = new AppStateService(_preferences);
        return (new StockDocumentDraftStore(state, new AesFieldEncryptor(_secrets), _logger), state);
    }

    private static StockDocumentDraft Draft(OperationDirection direction)
    {
        var draft = new StockDocumentDraft(direction) { Counterparty = "Acme Foods", Note = "waybill 7" };
        draft.Add("4820001000016");
        draft.Add(Drill, 5);
        return draft;
    }

    [Fact]
    public async Task SaveAndRestart_KeepsLinesInOrderAndTheCounterpartyEncrypted()
    {
        var (store, state) = Create();
        await store.SaveAsync(Draft(OperationDirection.In));
        state.Save();

        var (restarted, restartedState) = Create();
        restartedState.Restore();
        var loaded = await restarted.LoadAsync(OperationDirection.In);

        Assert.Equal(Draft(OperationDirection.In).Lines, loaded.Lines);
        Assert.Equal(("Acme Foods", "waybill 7"), (loaded.Counterparty, loaded.Note));
        Assert.DoesNotContain("Acme", string.Concat(_preferences.Values.Values));
    }

    [Fact]
    public async Task TheTwoDirectionsAreSeparate()
    {
        var (store, _) = Create();
        await store.SaveAsync(Draft(OperationDirection.In));
        await store.SaveAsync(Draft(OperationDirection.Out));

        store.Clear(OperationDirection.In);

        Assert.Equal(0, store.LineCount(OperationDirection.In));
        Assert.Equal(2, store.LineCount(OperationDirection.Out));
        Assert.Empty((await store.LoadAsync(OperationDirection.In)).Lines);
    }

    [Fact]
    public async Task UndecryptableCounterparty_IsDroppedWithAWarningAndNeverLogged()
    {
        var (store, state) = Create();
        await store.SaveAsync(Draft(OperationDirection.In));
        _secrets.Values.Clear();

        var loaded = await new StockDocumentDraftStore(state, new AesFieldEncryptor(_secrets), _logger)
            .LoadAsync(OperationDirection.In);

        Assert.Equal(string.Empty, loaded.Counterparty);
        Assert.Equal(2, loaded.Lines.Count);
        Assert.Contains(_logger.Records, r => r.Level == LogLevel.Warning);
        Assert.DoesNotContain(_logger.Records, r => r.Message.Contains("Acme"));
    }
}
