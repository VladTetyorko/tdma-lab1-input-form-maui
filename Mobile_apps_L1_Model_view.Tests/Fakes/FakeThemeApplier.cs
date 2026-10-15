using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Remembers every theme that was put on the app, in order
public sealed class FakeThemeApplier : IThemeApplier
{
    public List<ThemeSetting> Applied { get; } = [];

    public void Apply(ThemeSetting theme) => Applied.Add(theme);
}
