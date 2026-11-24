using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

namespace Mobile_apps_L1_Model_view.UiTests.Support;

// What a test can do with the running app: find a control by its AutomationId, type, tap, wait.
// AutomationId is the accessibility id on both platforms (Windows UIA AutomationId, Android content description)
public sealed class AppScreen(AppSession session)
{
    private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(20);

    // Demo account of the seeded database (see README): the Manager may add products
    public const string ManagerLogin = "olena";
    public const string ManagerPassword = "Manager#2026";

    private AppiumDriver Driver => session.Driver;

    public IWebElement Find(string automationId, TimeSpan? timeout = null) =>
        new WebDriverWait(Driver, timeout ?? DefaultWait).Until(d =>
        {
            var found = d.FindElements(MobileBy.AccessibilityId(automationId));
            return found.Count > 0 && found[0].Displayed ? found[0] : null;
        })!;

    public bool IsShown(string automationId, TimeSpan? timeout = null)
    {
        try
        {
            Find(automationId, timeout ?? TimeSpan.FromSeconds(5));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public void Type(string automationId, string text)
    {
        var element = Find(automationId);
        element.Click();
        element.Clear();
        element.SendKeys(text);
    }

    public void Tap(string automationId) => Find(automationId).Click();

    // A label with this exact text (a product name in the list). Windows exposes the text as Name, Android as text
    public bool IsTextShown(string text, TimeSpan? timeout = null)
    {
        var by = session.Platform == UiPlatform.Windows
            ? MobileBy.Name(text)
            : MobileBy.XPath($"//*[@text='{text}' or @content-desc='{text}']");
        try
        {
            new WebDriverWait(Driver, timeout ?? DefaultWait).Until(d => d.FindElements(by).Count > 0);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // Signs in with the demo Manager. A saved session may skip the login page: then it is signed out first,
    // so the test always starts from the same place
    public void SignInAsManager()
    {
        if (!IsShown("LoginEntry", TimeSpan.FromSeconds(8)))
        {
            Tap("SignOutButton");
        }

        Type("LoginEntry", ManagerLogin);
        Type("PasswordEntry", ManagerPassword);
        Tap("LoginButton");
    }
}
