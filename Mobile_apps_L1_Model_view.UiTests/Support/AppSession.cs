using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Windows;

namespace Mobile_apps_L1_Model_view.UiTests.Support;

// Starts the app through an Appium server and hands the driver to the tests. Configuration is by environment variables
// so the same tests run on Windows and on Android (see docs/steps/08-testing-ci.md for the commands):
//   STOCKROOM_UI_PLATFORM  windows (default) | android
//   STOCKROOM_UI_APP       Windows: path to the built .exe. Android: path to the .apk (or empty when already installed)
//   STOCKROOM_UI_SERVER    Appium server URL, default http://127.0.0.1:4723
//   STOCKROOM_UI_DEVICE    Android: device or emulator name (default "emulator-5554")
//   STOCKROOM_UI_PACKAGE   Android: app package (default com.companyname.mobile_apps_l1_model_view)
public sealed class AppSession : IDisposable
{
    public const string DefaultAndroidPackage = "com.companyname.mobile_apps_l1_model_view";

    private AppSession(AppiumDriver driver, UiPlatform platform)
    {
        Driver = driver;
        Platform = platform;
    }

    public AppiumDriver Driver { get; }

    public UiPlatform Platform { get; }

    public static AppSession Start()
    {
        var platform = Environment.GetEnvironmentVariable("STOCKROOM_UI_PLATFORM")?.Trim().ToLowerInvariant() switch
        {
            null or "" or "windows" => UiPlatform.Windows,
            "android" => UiPlatform.Android,
            var other => throw new InvalidOperationException($"STOCKROOM_UI_PLATFORM must be windows or android, not '{other}'.")
        };

        var server = new Uri(Environment.GetEnvironmentVariable("STOCKROOM_UI_SERVER") ?? "http://127.0.0.1:4723");
        var app = Environment.GetEnvironmentVariable("STOCKROOM_UI_APP");
        var timeout = TimeSpan.FromMinutes(2);

        return platform == UiPlatform.Windows
            ? new AppSession(new WindowsDriver(server, WindowsOptions(app), timeout), platform)
            : new AppSession(new AndroidDriver(server, AndroidOptions(app), timeout), platform);
    }

    // Appium Windows driver: starts the exe and attaches to its window
    private static AppiumOptions WindowsOptions(string? app)
    {
        if (string.IsNullOrWhiteSpace(app))
        {
            throw new InvalidOperationException("Set STOCKROOM_UI_APP to the path of Mobile_apps_L1_Model_view.exe.");
        }

        var options = new AppiumOptions { PlatformName = "Windows", AutomationName = "Windows" };
        options.App = app;
        return options;
    }

    // UiAutomator2: installs the apk when one is given, then starts the main activity of the package
    private static AppiumOptions AndroidOptions(string? app)
    {
        var options = new AppiumOptions { PlatformName = "Android", AutomationName = "UiAutomator2" };
        options.DeviceName = Environment.GetEnvironmentVariable("STOCKROOM_UI_DEVICE") ?? "emulator-5554";
        if (!string.IsNullOrWhiteSpace(app))
        {
            options.App = app;
        }

        var package = Environment.GetEnvironmentVariable("STOCKROOM_UI_PACKAGE") ?? DefaultAndroidPackage;
        options.AddAdditionalAppiumOption("appPackage", package);
        options.AddAdditionalAppiumOption("appWaitActivity", "*");
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);
        options.AddAdditionalAppiumOption("noReset", false);
        return options;
    }

    public void Dispose()
    {
        try
        {
            Driver.Quit();
        }
        finally
        {
            Driver.Dispose();
        }
    }
}
