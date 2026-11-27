using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class MauiScanFeedbackTests
{
    private sealed class FakeVibration : IVibration
    {
        public bool IsSupported { get; init; } = true;

        public bool Fails { get; init; }

        public List<TimeSpan> Buzzes { get; } = [];

        public void Vibrate() => Vibrate(TimeSpan.Zero);

        public void Vibrate(TimeSpan duration)
        {
            if (Fails)
            {
                throw new InvalidOperationException("no motor");
            }

            Buzzes.Add(duration);
        }

        public void Cancel()
        {
        }
    }

    private sealed class FakeTones : ITonePlayer
    {
        public List<string> Played { get; } = [];

        public void Accepted() => Played.Add("accepted");

        public void Refused() => Played.Add("refused");
    }

    private sealed class FakeSoundSettings : IScanSoundSettings
    {
        public bool IsEnabled { get; set; } = true;
    }

    private sealed class FakeScreenReader : ISemanticScreenReader
    {
        public List<string> Said { get; } = [];

        public void Announce(string text) => Said.Add(text);
    }

    private static MauiScanFeedback Create(FakeVibration vibration, FakeTones? tones = null, bool sound = true,
        ISemanticScreenReader? screenReader = null) =>
        new(vibration, tones ?? new FakeTones(), new FakeSoundSettings { IsEnabled = sound },
            screenReader ?? new FakeScreenReader(), NullLogger<MauiScanFeedback>.Instance);

    [Fact]
    public void Announce_GoesToTheScreenReader()
    {
        var reader = new FakeScreenReader();

        Create(new FakeVibration(), screenReader: reader).Announce("Drill +1");

        Assert.Equal(["Drill +1"], reader.Said);
    }

    [Theory]
    [InlineData(true, new[] { "accepted", "refused" })]
    [InlineData(false, new string[0])]
    public void Sounds_FollowTheSwitch(bool sound, string[] expected)
    {
        var tones = new FakeTones();
        var feedback = Create(new FakeVibration(), tones, sound);

        feedback.Success();
        feedback.Error();

        Assert.Equal(expected, tones.Played);
    }

    [Fact]
    public void Error_BuzzesLongerThanSuccess()
    {
        var vibration = new FakeVibration();
        var feedback = Create(vibration);

        feedback.Success();
        feedback.Error();

        Assert.True(vibration.Buzzes[1] > vibration.Buzzes[0]);
    }

    [Fact]
    public void Unsupported_DoesNothing()
    {
        var vibration = new FakeVibration { IsSupported = false };

        Create(vibration).Success();

        Assert.Empty(vibration.Buzzes);
    }

    [Fact]
    public void Failure_IsNotThrown()
    {
        Create(new FakeVibration { Fails = true }).Error();
    }
}
