using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// Fake themes: the real ones create brushes, which need a UI thread. Their content is checked in the Core test project
public class ThemeDictionarySwapperTests
{
    private sealed class FakeTheme : ResourceDictionary, IThemeDictionary;

    [Fact]
    public void Swap_LeavesExactlyOneThemeAndKeepsTheRest()
    {
        var other = new ResourceDictionary();
        var merged = new List<ResourceDictionary> { other, new FakeTheme() };
        var next = new FakeTheme();

        ThemeDictionarySwapper.Swap(merged, new FakeTheme());
        ThemeDictionarySwapper.Swap(merged, next);

        Assert.Equal([other, next], merged);
    }
}
