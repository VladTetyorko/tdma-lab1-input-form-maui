using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductValidatorTests
{
    private const string ValidBarcode = "4006381333931";

    private readonly InMemoryProductRepository _products = new();
    private readonly ProductValidator _validator;

    public ProductValidatorTests()
    {
        _validator = new ProductValidator(_products);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_RequiresANonBlankName(string? name)
    {
        Assert.Equal("Name is required.", _validator.ValidateName(name));
    }

    [Fact]
    public void ValidateName_AcceptsNameOfExactlyMaxLength()
    {
        var name = new string('A', ProductValidator.MaxNameLength);

        Assert.Null(_validator.ValidateName(name));
    }

    [Fact]
    public void ValidateName_RejectsNameLongerThanMaxLength()
    {
        var name = new string('A', ProductValidator.MaxNameLength + 1);

        Assert.Equal($"Name must be at most {ProductValidator.MaxNameLength} characters.",
            _validator.ValidateName(name));
    }

    [Fact]
    public void ValidateName_CountsLengthAfterTrimming()
    {
        var name = "  " + new string('A', ProductValidator.MaxNameLength) + "  ";

        Assert.Null(_validator.ValidateName(name));
    }

    [Fact]
    public void ValidateBarcodeFormat_AcceptsValidEan13()
    {
        Assert.Null(_validator.ValidateBarcodeFormat(ValidBarcode));
    }

    [Fact]
    public void ValidateBarcodeFormat_IgnoresSurroundingSpaces()
    {
        Assert.Null(_validator.ValidateBarcodeFormat("  " + ValidBarcode + " "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateBarcodeFormat_RequiresABarcode(string? barcode)
    {
        Assert.Equal("Barcode is required.", _validator.ValidateBarcodeFormat(barcode));
    }

    [Theory]
    [InlineData("4006381333932")]
    [InlineData("123")]
    [InlineData("400638133393X")]
    public void ValidateBarcodeFormat_RejectsInvalidEan13(string barcode)
    {
        Assert.Equal("Barcode must be a valid EAN-13 (13 digits with a correct check digit).",
            _validator.ValidateBarcodeFormat(barcode));
    }

    [Fact]
    public async Task ValidateBarcodeAsync_AcceptsUnusedBarcode()
    {
        Assert.Null(await _validator.ValidateBarcodeAsync(ValidBarcode));
    }

    [Fact]
    public async Task ValidateBarcodeAsync_RejectsBarcodeOfExistingProduct()
    {
        await _products.AddAsync(new Product { Name = "Drill", Barcode = ValidBarcode });

        Assert.Equal("A product with this barcode already exists.",
            await _validator.ValidateBarcodeAsync(ValidBarcode));
    }

    [Fact]
    public async Task ValidateBarcodeAsync_FindsDuplicateIgnoringSurroundingSpaces()
    {
        await _products.AddAsync(new Product { Name = "Drill", Barcode = ValidBarcode });

        Assert.Equal("A product with this barcode already exists.",
            await _validator.ValidateBarcodeAsync("  " + ValidBarcode + "  "));
    }

    [Fact]
    public async Task ValidateBarcodeAsync_ReturnsFormatErrorBeforeCheckingDuplicates()
    {
        await _products.AddAsync(new Product { Name = "Drill", Barcode = ValidBarcode });

        Assert.Equal("Barcode is required.", await _validator.ValidateBarcodeAsync(""));
    }
}
