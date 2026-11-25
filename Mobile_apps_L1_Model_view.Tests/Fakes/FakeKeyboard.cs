using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeKeyboard : IKeyboard
{
    public int Hides { get; private set; }

    public void Hide() => Hides++;
}
