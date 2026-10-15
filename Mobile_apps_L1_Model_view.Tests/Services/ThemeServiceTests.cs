using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ThemeServiceTests
{
    private readonly FakePreferencesStore _preferences = new();
    private readonly FakeThemeApplier _applier = new();
    private readonly ThemeService _theme;

    public ThemeServiceTests()
    {
        _theme = new ThemeService(_preferences, _applier);
    }

    [Fact]
    public void Current_IsSystemBeforeAnythingIsLoaded()
    {
        Assert.Equal(ThemeSetting.System, _theme.Current);
    }

    [Fact]
    public void Load_MissingValue_UsesSystemAndAppliesIt()
    {
        _theme.Load();

        Assert.Equal(ThemeSetting.System, _theme.Current);
        Assert.Equal(new[] { ThemeSetting.System }, _applier.Applied);
    }

    [Theory]
    [InlineData("Purple")]
    [InlineData("")]
    [InlineData("42")]
    public void Load_InvalidValue_UsesSystem(string saved)
    {
        _preferences.Values["theme"] = saved;

        _theme.Load();

        Assert.Equal(ThemeSetting.System, _theme.Current);
        Assert.Equal(ThemeSetting.System, _applier.Applied.Last());
    }

    [Fact]
    public void Load_SavedChoice_IsAppliedAndKept()
    {
        _preferences.Values["theme"] = nameof(ThemeSetting.Light);

        _theme.Load();

        Assert.Equal(ThemeSetting.Light, _theme.Current);
        Assert.Equal(ThemeSetting.Light, _applier.Applied.Last());
    }

    [Fact]
    public void Set_SavesAndAppliesTheChoice()
    {
        _theme.Set(ThemeSetting.Dark);

        Assert.Equal(ThemeSetting.Dark, _theme.Current);
        Assert.Equal("Dark", _preferences.Values["theme"]);
        Assert.Equal(ThemeSetting.Dark, _applier.Applied.Last());
    }

    [Fact]
    public void Set_ThenNewServiceLoadsTheSameChoice()
    {
        _theme.Set(ThemeSetting.Light);

        var reloaded = new ThemeService(_preferences, new FakeThemeApplier());
        reloaded.Load();

        Assert.Equal(ThemeSetting.Light, reloaded.Current);
    }
}
