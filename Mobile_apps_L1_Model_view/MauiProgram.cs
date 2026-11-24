using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;
using Serilog;
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
        builder.Services.AddSingleton<ILogPathProvider, MauiLogPathProvider>();
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

        ConfigureLogging(builder);

        return builder.Build();
    }

    // The only place that knows Serilog: everything else logs through ILogger<T>
    private static void ConfigureLogging(MauiAppBuilder builder)
    {
        var logDirectory = new MauiLogPathProvider().LogDirectory;
        var configuration = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: LogFormat.OutputTemplate)
#else
            .MinimumLevel.Information()
#endif
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDirectory, LogFormat.FileNamePattern),
                outputTemplate: LogFormat.OutputTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogFormat.RetainedFiles,
                shared: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(configuration.CreateLogger(), dispose: true);
#if DEBUG
        builder.Logging.AddDebug();
#endif
    }
}
