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

    private static MauiScanFeedback Create(FakeVibration vibration) => new(vibration, NullLogger<MauiScanFeedback>.Instance);

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
