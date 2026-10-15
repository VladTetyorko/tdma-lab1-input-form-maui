using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;

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

        builder.Services.AddStockRoomCore();
        builder.Services.AddSingleton<IPreferencesStore, MauiPreferencesStore>();
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // In-memory seeding completes synchronously; Lab 4 replaces it with migrations
        app.Services.GetRequiredService<IDataSeeder>().SeedAsync().GetAwaiter().GetResult();

        return app;
    }
}
