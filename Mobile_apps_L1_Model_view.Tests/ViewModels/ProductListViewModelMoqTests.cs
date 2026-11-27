using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Lab 8 unit tests written with Moq (collaborators as mocks) and FluentAssertions. The older fake-based tests of the
// same ViewModel are in ProductListViewModelTests
public class ProductListViewModelMoqTests
{
    private static readonly ProductSummary Drill = new(Guid.NewGuid(), "Drill", "4006381333931", 6);
    private static readonly ProductSummary Saw = new(Guid.NewGuid(), "Saw", "4820001000016", 0);

    private readonly Mock<IProductCatalog> _catalog = new();
    private readonly Mock<IProductService> _productService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAlertService> _alerts = new();
    private readonly Mock<INavigationService> _navigation = new();
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelMoqTests()
    {
        var characteristics = new Mock<ICharacteristicService>();
        characteristics.Setup(c => c.GetAllAsync()).ReturnsAsync([]);
        _catalog.Setup(c => c.GetSummariesAsync()).ReturnsAsync([Drill, Saw]);

        // The real guard over mocked user and alerts: the refusal path is part of what these tests check
        var guard = new PermissionGuard(_currentUser.Object, _alerts.Object, NullLogger<PermissionGuard>.Instance);

        _viewModel = new ProductListViewModel(_catalog.Object, characteristics.Object, _productService.Object,
            _currentUser.Object, Mock.Of<IStartupNavigator>(),
            _alerts.Object, guard, _navigation.Object, Mock.Of<IShakeGesture>(),
            NullLogger<ProductListViewModel>.Instance);
    }

    private void SignInAs(Role role) =>
        _currentUser.SetupGet(c => c.CurrentUser).Returns(new User { FullName = "Test User", Role = role });

    [Fact]
    public async Task Load_FillsTheListFromTheCatalog()
    {
        SignInAs(Role.Manager);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.Products.Should().BeEquivalentTo([Drill, Saw]);
        _catalog.Verify(c => c.GetSummariesAsync(), Times.Once);
    }

    [Fact]
    public async Task OpenDetails_NavigatesToTheDetailRouteWithTheSelectedProduct()
    {
        SignInAs(Role.Storekeeper);

        await _viewModel.OpenDetailsCommand.ExecuteAsync(Drill);

        _navigation.Verify(n => n.GoToAsync(Routes.ProductDetail,
            It.Is<IDictionary<string, object>>(p => p.Count == 1 && ReferenceEquals(p[NavigationKeys.SelectedProduct], Drill))),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ByDriver_IsDeniedWithAnAlertAndTheServiceIsNeverCalled()
    {
        SignInAs(Role.Driver);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.DeleteProductCommand.ExecuteAsync(Saw);

        _productService.Verify(s => s.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        _alerts.Verify(a => a.ShowAsync(PermissionGuard.AlertTitle, It.Is<string>(m => m.Contains("Driver"))), Times.Once);
        _viewModel.StatusMessage.Should().Contain("Driver");
        _viewModel.Products.Should().Contain(Saw);
    }

    [Fact]
    public async Task Delete_ByManager_AfterConfirmation_CallsTheServiceOnceAndRemovesTheRow()
    {
        SignInAs(Role.Manager);
        _alerts.Setup(a => a.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _productService.Setup(s => s.DeleteAsync(Saw.Id))
            .ReturnsAsync(Result<Product>.Success(new Product { Name = Saw.Name, Barcode = Saw.Barcode }));
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.DeleteProductCommand.ExecuteAsync(Saw);

        _productService.Verify(s => s.DeleteAsync(Saw.Id), Times.Once);
        _viewModel.Products.Should().NotContain(Saw).And.Contain(Drill);
    }

    [Fact]
    public async Task Delete_WhenTheManagerDeclinesTheQuestion_TheServiceIsNeverCalled()
    {
        SignInAs(Role.Manager);
        _alerts.Setup(a => a.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.DeleteProductCommand.ExecuteAsync(Saw);

        _productService.Verify(s => s.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        _viewModel.Products.Should().HaveCount(2);
    }
}
