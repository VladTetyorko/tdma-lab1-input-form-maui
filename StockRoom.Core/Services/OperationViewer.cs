using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// A tap on a row of the operation table, the same on every page: the sheet of the operation and what was chosen in it.
// Returns the message for the page's status line (empty = nothing to say)
public sealed class OperationViewer(
    ISheetService sheets,
    INavigationService navigation,
    IProductCatalog catalog,
    IMapLauncher maps,
    IDeviceFeedback feedback,
    IClock clock,
    ILogger<OperationViewer> logger)
{
    public async Task<string> OpenAsync(OperationEntry entry)
    {
        var sheet = new OperationSheetViewModel(entry, clock.UtcNow);
        await sheets.ShowAsync(sheet);
        switch (sheet.Choice)
        {
            case OperationChoice.OpenProduct when await catalog.GetSummaryAsync(entry.ProductId) is { } product:
                await navigation.GoToAsync(Routes.ProductDetail,
                    new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
                break;
            case OperationChoice.ShowOnMap when entry.Location is { } location:
                var status = await maps.OpenAsync(location, $"{entry.Direction} {entry.Quantity} x {entry.ProductName}");
                if (status == DeviceStatus.Succeeded)
                {
                    logger.LogInformation("Maps opened for operation {OperationId}", entry.Id);
                }

                return feedback.Report(DeviceFeature.Maps, status);
        }

        return string.Empty;
    }
}
