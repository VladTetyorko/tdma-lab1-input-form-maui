using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view;

public static class ServiceCollectionExtensions
{
    // Everything except pages and platform services (INavigationService),
    // so tests can build the same container with fake platform services
    public static IServiceCollection AddStockRoomCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddSingleton<ICharacteristicRepository, InMemoryCharacteristicRepository>();
        services.AddSingleton<IProductCharacteristicRepository, InMemoryProductCharacteristicRepository>();
        services.AddSingleton<IOperationRepository, InMemoryOperationRepository>();
        services.AddSingleton<IDataSeeder, DataSeeder>();

        services.AddSingleton<IProductValidator, ProductValidator>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IStockService, StockService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductFormViewModel>();

        return services;
    }
}
