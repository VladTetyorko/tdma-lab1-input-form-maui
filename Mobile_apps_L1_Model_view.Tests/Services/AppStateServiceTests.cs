using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class AppStateServiceTests
{
    private const string Barcode = "4820001000016";

    private readonly FakePreferencesStore _preferences = new();
    private readonly AppStateService _state;

    public AppStateServiceTests()
    {
        _state = new AppStateService(_preferences);
    }

    [Fact]
    public void SaveThenRestore_NewInstanceReadsWhatTheOldOneSaved()
    {
        var userId = Guid.NewGuid();
        _state.SignedInUserId = userId;
        _state.LastRoute = Routes.Operation;
        _state.SetDraft(new OperationDraft(Barcode, OperationDirection.Out, 3, "Acme Ltd", "Site 7"));
        _state.Save();

        var restored = new AppStateService(_preferences);
        restored.Restore();

        Assert.Equal(userId, restored.SignedInUserId);
        Assert.Equal(Routes.Operation, restored.LastRoute);
        Assert.Equal(new OperationDraft(Barcode, OperationDirection.Out, 3, "Acme Ltd", "Site 7"),
            restored.GetDraft<OperationDraft>());
    }

    [Fact]
    public void Restore_NothingSaved_LeavesEverythingEmpty()
    {
        _state.Restore();

        Assert.Null(_state.SignedInUserId);
        Assert.Null(_state.LastRoute);
        Assert.Null(_state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void GetDraft_ReturnsNullUntilOneIsSet()
    {
        Assert.Null(_state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void SetDraft_ThenGetDraft_ReturnsTheSameContent()
    {
        _state.SetDraft(new ProductFormDraft("Cordless Saw", Barcode));

        Assert.Equal(new ProductFormDraft("Cordless Saw", Barcode), _state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void SetDraft_ReplacesThePreviousDraftOfTheSameType()
    {
        _state.SetDraft(new ProductFormDraft("First", Barcode));

        _state.SetDraft(new ProductFormDraft("Second", Barcode));

        Assert.Equal("Second", _state.GetDraft<ProductFormDraft>()!.Name);
    }

    [Fact]
    public void Drafts_OfDifferentTypesAreKeptApart()
    {
        _state.SetDraft(new ProductFormDraft("Saw", Barcode));
        _state.SetDraft(new OperationDraft(Barcode, OperationDirection.In, 1, string.Empty, string.Empty));

        Assert.Equal("Saw", _state.GetDraft<ProductFormDraft>()!.Name);
        Assert.Equal(OperationDirection.In, _state.GetDraft<OperationDraft>()!.Direction);
    }

    [Fact]
    public void ClearDraft_RemovesOnlyThatType()
    {
        _state.SetDraft(new ProductFormDraft("Saw", Barcode));
        _state.SetDraft(new OperationDraft(Barcode, OperationDirection.In, 1, string.Empty, string.Empty));

        _state.ClearDraft<ProductFormDraft>();

        Assert.Null(_state.GetDraft<ProductFormDraft>());
        Assert.NotNull(_state.GetDraft<OperationDraft>());
    }

    [Fact]
    public void ClearDraft_WithoutADraft_DoesNothing()
    {
        _state.ClearDraft<ProductFormDraft>();

        Assert.Null(_state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void SetDraft_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _state.SetDraft<ProductFormDraft>(null!));
    }

    [Fact]
    public void ProductDetailDraft_SurvivesSaveAndRestore()
    {
        var characteristicId = Guid.NewGuid();
        _state.SetDraft(new ProductDetailDraft(Barcode, new Dictionary<Guid, string> { [characteristicId] = "2.5" }));
        _state.Save();

        var restored = new AppStateService(_preferences);
        restored.Restore();

        var draft = restored.GetDraft<ProductDetailDraft>();
        Assert.NotNull(draft);
        Assert.Equal(Barcode, draft.Barcode);
        Assert.Equal("2.5", draft.Values[characteristicId]);
    }

    [Fact]
    public void Restore_CorruptDraftsJson_IsIgnored()
    {
        _preferences.Values["state.drafts"] = "{ this is not json";

        _state.Restore();

        Assert.Null(_state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void GetDraft_CorruptDraftEntry_ReturnsNullAndDropsIt()
    {
        _preferences.Values["state.drafts"] = "{\"ProductFormDraft\":\"{broken\"}";
        _state.Restore();

        Assert.Null(_state.GetDraft<ProductFormDraft>());
        Assert.Null(_state.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void Restore_UnreadableUserId_MeansNobodyIsSignedIn()
    {
        _preferences.Values["state.user"] = "not-a-guid";

        _state.Restore();

        Assert.Null(_state.SignedInUserId);
    }

    [Fact]
    public void TakeResumeRoute_ReturnsTheRouteOnceThenNull()
    {
        _preferences.Values["state.route"] = Routes.ProductDetail;
        _state.Restore();

        Assert.Equal(Routes.ProductDetail, _state.TakeResumeRoute());
        Assert.Null(_state.TakeResumeRoute());
    }

    [Fact]
    public void TakeResumeRoute_WithoutRestore_IsNull()
    {
        _state.LastRoute = Routes.Operation;

        Assert.Null(_state.TakeResumeRoute());
    }

    [Fact]
    public void Clear_EmptiesMemoryAndPreferences()
    {
        _state.SignedInUserId = Guid.NewGuid();
        _state.LastRoute = Routes.Operation;
        _state.SetDraft(new ProductFormDraft("Saw", Barcode));
        _state.Save();

        _state.Clear();

        Assert.Null(_state.SignedInUserId);
        Assert.Null(_state.LastRoute);
        Assert.Null(_state.GetDraft<ProductFormDraft>());
        Assert.Empty(_preferences.Values);

        var restored = new AppStateService(_preferences);
        restored.Restore();
        Assert.Null(restored.TakeResumeRoute());
        Assert.Null(restored.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public void Save_WithoutDrafts_WritesNoDraftsKey()
    {
        _state.SignedInUserId = Guid.NewGuid();
        _state.Save();

        Assert.True(_preferences.Values.ContainsKey("state.user"));
        Assert.False(_preferences.Values.ContainsKey("state.drafts"));
    }
}
