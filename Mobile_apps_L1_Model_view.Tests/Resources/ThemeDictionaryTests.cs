using System.Text.RegularExpressions;

namespace Mobile_apps_L1_Model_view.Tests.Resources;

public class ThemeDictionaryTests
{
    // The keys of docs/design/design-system.md section 3: every theme dictionary must define all of them
    private static readonly string[] SemanticKeys =
    [
        "Background", "Surface", "SurfaceAlt", "Border", "BorderStrong", "TextPrimary", "TextSecondary", "TextDisabled",
        "Primary", "PrimaryHover", "PrimaryPressed", "PrimarySoft", "OnPrimary", "PrimaryText", "ChipSuccessForeground",
        "Danger", "DangerSoft", "OnDanger", "Warning", "WarningSoft", "Info", "Success", "FocusRing", "Overlay"
    ];

    [Theory]
    [InlineData("LightGreen.xaml")]
    [InlineData("Dark.xaml")]
    public void Theme_DefinesEverySemanticKeyAsAColor(string file)
    {
        var keys = XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Themes", file));

        Assert.All(SemanticKeys, key => Assert.Equal("Color", keys.GetValueOrDefault(key)));
    }

    [Fact]
    public void Themes_HaveTheSameKeys()
    {
        var light = XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Themes", "LightGreen.xaml"));
        var dark = XamlFiles.Keys(XamlFiles.Load("Resources", "Styles", "Themes", "Dark.xaml"));

        Assert.Equal(light.OrderBy(pair => pair.Key), dark.OrderBy(pair => pair.Key));
    }

    [Fact]
    public void PagesAndStyles_HaveNoRawColours()
    {
        var files = Directory.GetFiles(Path.Combine(XamlFiles.App, "Views"), "*.xaml", SearchOption.AllDirectories)
            .Append(Path.Combine(XamlFiles.App, "Resources", "Styles", "Styles.xaml"))
            .Append(Path.Combine(XamlFiles.App, "AppShell.xaml"));

        Assert.All(files, file => Assert.DoesNotMatch(new Regex("\"#[0-9A-Fa-f]{6,8}\"|>#[0-9A-Fa-f]{6,8}<"), File.ReadAllText(file)));
    }
}
