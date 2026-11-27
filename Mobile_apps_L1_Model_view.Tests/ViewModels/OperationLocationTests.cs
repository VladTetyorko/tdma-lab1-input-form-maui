using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// "Attach my location" on the operation page: every outcome of the location request, the draft, and saving
public class OperationLocationTests
{
    private const string Barcode = "4006381333931";

    private static readonly GeoPoint Kyiv = new(50.4501, 30.5234);

    private readonly TestStore _store = new();
    private Product _product = null!;

    private OperationViewModel CreateViewModel() => new(
        _store.Stock, _store.CurrentUser, _store.Guard, _store.Drafts, _store.Navigation,
        _store.Location, _store.Feedback, _store.Logger<OperationViewModel>());

    private async Task<OperationViewModel> OpenAsync(Role role = Role.Driver)
    {
        await _store.SignInAsync(role);
        _product ??= await _store.AddProductAsync("Drill", Barcode);
        var viewModel = CreateViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = (await _store.Catalog.GetSummaryAsync(_product.Id))!
        });
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task Attach_Granted_ShowsLocationAttached_AndLogsWithoutCoordinates()
    {
        var viewModel = await OpenAsync();
        _store.Location.Result = LocationResult.Found(Kyiv);

        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        Assert.Equal(Kyiv, viewModel.Location);
        Assert.True(viewModel.HasLocation);
        Assert.Equal("Location attached: 50.45010, 30.52340", viewModel.LocationText);
        Assert.Equal(string.Empty, viewModel.LocationMessage);
        Assert.False(viewModel.IsLocating);
        var line = Assert.Single(_store.Logs, l => l.Category == nameof(OperationViewModel) && l.Message.StartsWith("Location attached"));
        Assert.Equal(LogLevel.Information, line.Level);
        Assert.DoesNotContain("50.45", line.Message);
    }

    [Fact]
    public async Task BeforeAttaching_ThereIsNoLocation()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasLocation);
        Assert.Equal("No location attached", viewModel.LocationText);
    }

    [Theory]
    [InlineData(DeviceStatus.PermissionDenied, LogLevel.Warning)]
    [InlineData(DeviceStatus.NotEnabled, LogLevel.Warning)]
    [InlineData(DeviceStatus.NotSupported, LogLevel.Warning)]
    [InlineData(DeviceStatus.Cancelled, LogLevel.Information)]
    [InlineData(DeviceStatus.Failed, LogLevel.Error)]
    public async Task Attach_NotSucceeded_ShowsOneMessage_LogsOneLine_AndAttachesNothing(DeviceStatus status, LogLevel level)
    {
        var viewModel = await OpenAsync();
        var error = status == DeviceStatus.Failed ? new InvalidOperationException("sensor") : null;
        _store.Location.Result = new LocationResult(status, null, error);

        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        Assert.Equal(DeviceMessages.For(DeviceFeature.Location, status), viewModel.LocationMessage);
        Assert.False(viewModel.HasLocation);
        var record = Assert.Single(_store.Logs, l => l.Category == nameof(DeviceFeedback));
        Assert.Equal(level, record.Level);
        Assert.Same(error, record.Exception);
        Assert.False(viewModel.IsLocating);
    }

    [Fact]
    public async Task Attach_GpsOff_TellsTheUserToTurnItOn()
    {
        var viewModel = await OpenAsync();
        _store.Location.Result = LocationResult.Of(DeviceStatus.NotEnabled);

        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        Assert.Contains("Turn on GPS", viewModel.LocationMessage);
    }

    [Fact]
    public async Task Attach_AfterAFailure_ASuccessClearsTheMessage()
    {
        var viewModel = await OpenAsync();
        _store.Location.Result = LocationResult.Of(DeviceStatus.PermissionDenied);
        await viewModel.AttachLocationCommand.ExecuteAsync(null);
        _store.Location.Result = LocationResult.Found(Kyiv);

        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.LocationMessage);
        Assert.True(viewModel.HasLocation);
    }

    [Fact]
    public async Task Attach_WhileLookingUp_IsIgnored()
    {
        var viewModel = await OpenAsync();
        _store.Location.Pending = new TaskCompletionSource<LocationResult>();

        var first = viewModel.AttachLocationCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsLocating);
        await viewModel.AttachLocationCommand.ExecuteAsync(null);
        _store.Location.Pending.SetResult(LocationResult.Found(Kyiv));
        await first;

        Assert.Equal(1, _store.Location.Calls);
        Assert.False(viewModel.IsLocating);
        Assert.Equal(Kyiv, viewModel.Location);
    }

    [Fact]
    public async Task Clear_RemovesTheLocationAndTheMessage()
    {
        var viewModel = await OpenAsync();
        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        viewModel.ClearLocationCommand.Execute(null);

        Assert.False(viewModel.HasLocation);
        Assert.Equal("No location attached", viewModel.LocationText);
    }

    [Fact]
    public async Task Save_WithLocation_RecordsItOnTheOperation()
    {
        var viewModel = await OpenAsync();
        viewModel.Quantity = 3;
        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        var operation = Assert.Single(await _store.Operations.GetAllAsync());
        Assert.Equal(_store.Location.Result.Point, operation.Location);
        Assert.Equal(Routes.Back, _store.Navigation.RequestedRoutes.Single());
    }

    [Fact]
    public async Task Save_WithoutLocation_RecordsNone()
    {
        var viewModel = await OpenAsync();

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(Assert.Single(await _store.Operations.GetAllAsync()).Location);
    }

    [Fact]
    public async Task Draft_KeepsTheLocation_WhenThePageIsOpenedAgain()
    {
        var viewModel = await OpenAsync();
        viewModel.Quantity = 4;
        await viewModel.AttachLocationCommand.ExecuteAsync(null);

        var reopened = await OpenAsync();

        Assert.Equal(_store.Location.Result.Point, reopened.Location);
        Assert.Equal(4, reopened.Quantity);
    }

    [Fact]
    public async Task Draft_ClearedLocation_IsNotRestored()
    {
        var viewModel = await OpenAsync();
        await viewModel.AttachLocationCommand.ExecuteAsync(null);
        viewModel.ClearLocationCommand.Execute(null);

        var reopened = await OpenAsync();

        Assert.Null(reopened.Location);
    }

    [Fact]
    public async Task Save_ClearsTheDraftIncludingTheLocation()
    {
        var viewModel = await OpenAsync();
        await viewModel.AttachLocationCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        var reopened = await OpenAsync();

        Assert.Null(reopened.Location);
    }

    [Fact]
    public async Task Draft_JsonRoundTripKeepsTheLocation()
    {
        var draft = new OperationDraft(Barcode, OperationDirection.In, 2, string.Empty, string.Empty, Kyiv);

        var back = System.Text.Json.JsonSerializer.Deserialize<OperationDraft>(System.Text.Json.JsonSerializer.Serialize(draft));

        Assert.Equal(Kyiv, back!.Location);
        await Task.CompletedTask;
    }
}
