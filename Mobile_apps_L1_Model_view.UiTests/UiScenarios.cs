using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.UiTests.Support;

namespace Mobile_apps_L1_Model_view.UiTests;

// Two end-to-end scenarios through the real UI. They need a running Appium server and an app (Windows exe or Android
// emulator): see docs/steps/08-testing-ci.md. Every test is in the "UI" category, so CI filters them out
[Trait("Category", "UI")]
public class UiScenarios : IDisposable
{
    private readonly AppSession _session = AppSession.Start();
    private readonly AppScreen _screen;

    public UiScenarios() => _screen = new AppScreen(_session);

    public void Dispose() => _session.Dispose();

    [Fact]
    public void Login_WithTheDemoManager_ShowsTheProductList()
    {
        _screen.SignInAsManager();

        Assert.True(_screen.IsShown("ProductsCollection"), "The product list should be visible after signing in.");
        Assert.True(_screen.IsShown("AddProductButton"), "The Manager may add products.");
    }

    [Fact]
    public void CreatingAProduct_MakesItAppearInTheList()
    {
        // Starts with "AAA" so that it sorts first and is on screen without scrolling (default sort: by name)
        var name = $"AAA UI test {DateTime.UtcNow:HHmmss}";
        var barcode = NewValidBarcode();
        _screen.SignInAsManager();
        Assert.True(_screen.IsShown("ProductsCollection"));

        _screen.Tap("AddProductButton");
        _screen.Type("ProductNameEntry", name);
        _screen.Type("BarcodeEntry", barcode);
        _screen.Tap("SaveProductButton");

        Assert.True(_screen.IsShown("ProductsCollection"), "Save returns to the list.");
        Assert.True(_screen.IsTextShown(name), $"The new product '{name}' should be in the list.");
    }

    // 12 random digits in the "restricted circulation" range (20-29) plus the check digit from the app's own rule
    private static string NewValidBarcode()
    {
        var digits = "2" + Random.Shared.NextInt64(0, 100_000_000_000).ToString("D11");
        return Ean13.WithCheckDigit(digits);
    }
}
