using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// Message: for the page's status line (empty = nothing to say). Changed: stock moved, the page reads its lists again
public sealed record OperationViewResult(string Message, bool Changed = false);

// A tap on a row of the operation table, the same on every page: the sheet of the operation and what was chosen in it
public sealed class OperationViewer(
    ISheetService sheets,
    INavigationService navigation,
    IProductCatalog catalog,
    IMapLauncher maps,
    IDeviceFeedback feedback,
    OperationCorrector corrector,
    ICurrentUserService currentUser,
    IToastService toast,
    IClock clock,
    ILogger<OperationViewer> logger)
{
    public async Task<OperationViewResult> OpenAsync(OperationEntry entry)
    {
        var canCorrect = currentUser.CurrentUser is { } user
                         && (RolePermissions.CanCorrect(user.Role, OperationDirection.In)
                             || RolePermissions.CanCorrect(user.Role, OperationDirection.Out));
        var sheet = new OperationSheetViewModel(entry, clock.UtcNow, corrector, canCorrect);
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

                return new OperationViewResult(feedback.Report(DeviceFeature.Maps, status));
            case OperationChoice.Corrected:
                await toast.ShowAsync(sheet.Result);
                return new OperationViewResult(string.Empty, Changed: true);
        }

        return new OperationViewResult(string.Empty);
    }
}
