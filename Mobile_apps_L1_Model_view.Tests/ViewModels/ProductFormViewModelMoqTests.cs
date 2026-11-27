using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Lab 8 unit tests with Moq and FluentAssertions: validation drives CanExecute, Save calls the service exactly once
public class ProductFormViewModelMoqTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly Mock<IProductService> _productService = new();
    private readonly Mock<INavigationService> _navigation = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAlertService> _alerts = new();
    private readonly ProductFormViewModel _viewModel;

    public ProductFormViewModelMoqTests()
    {
        _currentUser.SetupGet(c => c.CurrentUser).Returns(new User { FullName = "Olena", Role = Role.Manager });

        // The real validator over a mocked repository that has no products: the format rules are the real ones
        var validator = new ProductValidator(Mock.Of<IProductRepository>());
        var guard = new PermissionGuard(_currentUser.Object, _alerts.Object, NullLogger<PermissionGuard>.Instance);
        var photo = new ProductPhotoEditor(Mock.Of<IPhotoService>(), Mock.Of<IDeviceFeedback>(), guard,
            NullLogger<ProductPhotoEditor>.Instance);

        var search = new QuickSearchViewModel(Mock.Of<IProductCatalog>(), Mock.Of<ICharacteristicService>(),
            _currentUser.Object, guard, _navigation.Object, Mock.Of<IKeyboard>(),
            NullLogger<QuickSearchViewModel>.Instance);

        _viewModel = new ProductFormViewModel(validator, _productService.Object, _currentUser.Object, guard,
            Mock.Of<IAppStateService>(), _navigation.Object, photo, Mock.Of<IBarcodeScanner>(),
            Mock.Of<IProductLookup>(lookup => lookup.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
                == Task.FromResult(LookupResult.NotFound())), search, Mock.Of<IConfirmSheet>(sheet =>
                sheet.AskAsync(It.IsAny<ConfirmRequest>()) == Task.FromResult(true)), NullLogger<ProductFormViewModel>.Instance);
    }

    [Theory]
    [InlineData("Drill", ValidBarcode, true)]
    [InlineData("", ValidBarcode, false)]
    [InlineData("   ", ValidBarcode, false)]
    [InlineData("Drill", "", false)]
    [InlineData("Drill", "123", false)]
    [InlineData("Drill", "4006381333932", false)] // wrong check digit
    [InlineData("Drill", "40063813339AB", false)]
    public void SaveCommand_CanExecute_DependsOnTheNameAndTheBarcode(string name, string barcode, bool expected)
    {
        _viewModel.Name = name;
        _viewModel.Barcode = barcode;

        _viewModel.SaveCommand.CanExecute(null).Should().Be(expected);
    }

    [Fact]
    public async Task Save_WithValidInput_CallsTheServiceOnceAndNavigatesBack()
    {
        var saved = new Product { Name = "Drill", Barcode = ValidBarcode };
        _productService.Setup(s => s.AddAsync("Drill", ValidBarcode, null)).ReturnsAsync(Result<Product>.Success(saved));
        _viewModel.Name = "Drill";
        _viewModel.Barcode = ValidBarcode;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        _productService.Verify(s => s.AddAsync("Drill", ValidBarcode, null), Times.Once);
        _navigation.Verify(n => n.GoToAsync(Routes.Back,
            It.Is<IDictionary<string, object>>(p => ((ProductSummary)p[NavigationKeys.UpdatedProduct]).Id == saved.Id)),
            Times.Once);
    }

    [Fact]
    public async Task Save_WhenTheServiceRefuses_ShowsTheReasonAndStaysOnThePage()
    {
        _productService.Setup(s => s.AddAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<Product>.Failure("Something went wrong."));
        _viewModel.Name = "Drill";
        _viewModel.Barcode = ValidBarcode;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        _viewModel.StatusMessage.Should().Be("Something went wrong.");
        _navigation.Verify(n => n.GoToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        _navigation.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
