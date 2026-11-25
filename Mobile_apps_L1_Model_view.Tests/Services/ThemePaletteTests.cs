using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ThemePaletteTests
{
    [Theory]
    [InlineData(ThemeSetting.Light, true, PaletteKey.LightGreen)]
    [InlineData(ThemeSetting.Light, false, PaletteKey.LightGreen)]
    [InlineData(ThemeSetting.Dark, true, PaletteKey.Dark)]
    [InlineData(ThemeSetting.Dark, false, PaletteKey.Dark)]
    [InlineData(ThemeSetting.System, true, PaletteKey.Dark)]
    [InlineData(ThemeSetting.System, false, PaletteKey.LightGreen)]
    public void Resolve_FollowsChoiceThenDevice(ThemeSetting setting, bool systemIsDark, PaletteKey expected)
    {
        Assert.Equal(expected, ThemePalette.Resolve(setting, systemIsDark));
    }
}
