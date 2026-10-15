using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class OperationDraftStoreTests
{
    private const string Barcode = "4006381333931";
    private const string Counterparty = "Acme Ltd";

    private readonly FakePreferencesStore _preferences = new();
    private readonly FakeSecretStore _secrets = new();
    private readonly AppStateService _state;
    private readonly OperationDraftStore _store;

    public OperationDraftStoreTests()
    {
        (_state, _store) = Create(_preferences, _secrets);
    }

    [Fact]
    public async Task Save_PersistsNoPlainCounterparty()
    {
        await _store.SaveAsync(Draft(Counterparty));
        _state.Save();

        var persisted = string.Concat(_preferences.Values.Values);
        Assert.DoesNotContain(Counterparty, persisted);
        Assert.Contains(Barcode, persisted);
    }

    [Fact]
    public async Task Load_AfterRestart_ReturnsThePlainCounterparty()
    {
        await _store.SaveAsync(Draft(Counterparty));
        _state.Save();

        var (_, restarted) = Create(_preferences, _secrets);

        Assert.Equal(Draft(Counterparty), await restarted.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Load_DraftOfAnotherProduct_ReturnsNull()
    {
        await _store.SaveAsync(Draft(Counterparty));

        Assert.Null(await _store.LoadAsync("4820001000016"));
    }

    [Fact]
    public async Task Load_DifferentKey_ReturnsDraftWithEmptyCounterparty()
    {
        await _store.SaveAsync(Draft(Counterparty));
        _state.Save();

        var (_, restarted) = Create(_preferences, new FakeSecretStore());

        Assert.Equal(Draft(string.Empty), await restarted.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Load_SecureStorageFailing_ReturnsDraftWithEmptyCounterparty()
    {
        await _store.SaveAsync(Draft(Counterparty));
        _state.Save();
        _secrets.Failing = true;
        var (_, restarted) = Create(_preferences, _secrets);

        Assert.Equal(Draft(string.Empty), await restarted.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Load_TamperedCipherText_ReturnsDraftWithEmptyCounterparty()
    {
        var state = new AppStateService(_preferences);
        state.SetDraft(Draft("not-encrypted-data"));
        var store = new OperationDraftStore(state, new AesFieldEncryptor(_secrets), NullLogger<OperationDraftStore>.Instance);

        Assert.Equal(Draft(string.Empty), await store.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Save_SecureStorageFailing_StoresDraftWithoutCounterparty()
    {
        _secrets.Failing = true;

        await _store.SaveAsync(Draft(Counterparty));
        _state.Save();

        Assert.DoesNotContain(Counterparty, string.Concat(_preferences.Values.Values));
        Assert.Equal(Draft(string.Empty), await _store.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Save_EmptyCounterparty_NeedsNoKey()
    {
        await _store.SaveAsync(Draft(string.Empty));

        Assert.Empty(_secrets.Values);
        Assert.Equal(Draft(string.Empty), await _store.LoadAsync(Barcode));
    }

    [Fact]
    public async Task Clear_RemovesTheDraft()
    {
        await _store.SaveAsync(Draft(Counterparty));

        _store.Clear();

        Assert.Null(await _store.LoadAsync(Barcode));
    }

    private static (AppStateService State, OperationDraftStore Store) Create(FakePreferencesStore preferences, FakeSecretStore secrets)
    {
        var state = new AppStateService(preferences);
        state.Restore();
        return (state, new OperationDraftStore(state, new AesFieldEncryptor(secrets), NullLogger<OperationDraftStore>.Instance));
    }

    private static OperationDraft Draft(string counterparty) =>
        new(Barcode, OperationDirection.Out, 3, counterparty, "Site 7");
}
