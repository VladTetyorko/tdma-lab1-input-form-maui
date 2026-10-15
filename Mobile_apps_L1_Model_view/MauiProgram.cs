using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;
#if ANDROID || IOS
using ZXing.Net.Maui.Controls;
#endif

namespace Mobile_apps_L1_Model_view;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID || IOS
        builder.UseBarcodeReader();
        builder.Services.AddSingleton<IBarcodeScanner, CameraBarcodeScanner>();
#else
        // No camera scanning here (Windows): the scanner page offers manual entry
        builder.Services.AddSingleton<IBarcodeScanner, ManualBarcodeScanner>();
#endif

        builder.Services.AddStockRoomCore();
        builder.Services.AddSingleton<IPreferencesStore, MauiPreferencesStore>();
        builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
        builder.Services.AddSingleton<ISecretStore, MauiSecretStore>();
        builder.Services.AddSingleton<IConnectivityMonitor, MauiConnectivityMonitor>();
        builder.Services.AddSingleton<IFileSharer, MauiFileSharer>();
        builder.Services.AddSingleton<IAlertService, ShellAlertService>();
        builder.Services.AddSingleton<IThemeApplier, MauiThemeApplier>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<ProductFormPage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<CharacteristicsPage>();
        builder.Services.AddTransient<OperationPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ScannerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
