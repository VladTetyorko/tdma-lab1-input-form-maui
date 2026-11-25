using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Sentry;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;
using Serilog;
#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;
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
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentIcons");
            });

#if ANDROID || IOS
        builder.UseBarcodeReader();
        builder.UseMauiMaps();
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
        builder.Services.AddSingleton<IBiometricAuthenticator, MauiBiometricAuthenticator>();
        builder.Services.AddSingleton<IConnectivityMonitor, MauiConnectivityMonitor>();
        builder.Services.AddSingleton<IFileSharer, MauiFileSharer>();

        // Device capabilities (Lab 7). The Essentials interfaces are registered here, so only the Maui* classes
        // below touch them; everything else asks for IPhotoService, ILocationService, IShakeDetector, ...
        builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IAccelerometer>(Accelerometer.Default);
        builder.Services.AddSingleton<IMap>(Microsoft.Maui.ApplicationModel.Map.Default);
        builder.Services.AddSingleton<IAppDataPathProvider, MauiAppDataPathProvider>();
        builder.Services.AddSingleton<IPlatformPermissions, MauiPlatformPermissions>();
        builder.Services.AddSingleton<IPhotoService, MauiPhotoService>();
        builder.Services.AddSingleton<ILocationService, MauiLocationService>();
        builder.Services.AddSingleton<IShakeDetector, MauiShakeDetector>();
        builder.Services.AddSingleton<IMapLauncher, MauiMapLauncher>();
        builder.Services.AddSingleton<IMainThread, MauiMainThread>();
        builder.Services.AddSingleton<IAlertService, ShellAlertService>();
        builder.Services.AddSingleton<IToastService, MauiToastService>();
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
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<ReceivePage>();
        builder.Services.AddTransient<IssuePage>();

        ConfigureLogging(builder);
        var settingsFile = ReadSettingsFile();
        ConfigureCrashReporting(builder, settingsFile);
        ConfigureSync(builder, settingsFile);

        var app = builder.Build();
        app.Services.GetRequiredService<UnhandledExceptionLogger>().Attach();
        return app;
    }

    // The only place that decides between Sentry and the null reporter: a DSN exists or it does not
    private static void ConfigureCrashReporting(MauiAppBuilder builder, string? settingsFile)
    {
        var dsn = SentryDsn.Resolve(Environment.GetEnvironmentVariable(SentryDsn.EnvironmentVariable), settingsFile);
        if (dsn is null)
        {
            builder.Services.AddSingleton<ICrashReporter, NullCrashReporter>();
            return;
        }

        // The plain Sentry SDK, not Sentry.Maui: Sentry.Maui needs WinRT.Runtime 2.3.1 on Windows, the app ships 2.2.0
        // and fails to start. Only handled and unhandled exceptions are sent; log lines are not
        SentrySdk.Init(options =>
        {
            options.Dsn = dsn;
            options.SendDefaultPii = false;
            options.AttachStacktrace = true;
            options.AutoSessionTracking = false;
        });
        builder.Services.AddSingleton<ICrashReporter, SentryCrashReporter>();
    }

    // The local stub server stays the default; a configured https server (Sync:BaseUrl) replaces it. Its HttpClient is the
    // only one with certificate pinning: Open Food Facts and everything else keep the normal handler
    private static void ConfigureSync(MauiAppBuilder builder, string? settingsFile)
    {
        var sync = SyncSettings.Resolve(settingsFile);
        if (!sync.IsConfigured)
        {
            return;
        }

        builder.Services.AddSingleton<ISyncApi>(provider => new HttpSyncApi(
            new HttpClient(PinnedHandlerFactory.Create(new CertificatePinValidator(sync.Pins)), disposeHandler: true)
            {
                BaseAddress = sync.BaseUrl,
                Timeout = HttpSyncApi.Timeout
            },
            provider.GetRequiredService<ILogger<HttpSyncApi>>()));
    }

    // appsettings.local.json is an untracked app asset (see README); missing = null
    private static string? ReadSettingsFile()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(LocalSettings.FileName).GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            return null;
        }
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
