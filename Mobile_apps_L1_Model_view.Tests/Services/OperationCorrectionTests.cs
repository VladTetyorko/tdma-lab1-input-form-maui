using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class OperationCorrectionTests
{
    private static OperationEntry Entry(OperationDirection direction = OperationDirection.In, string? number = "R-20261125-0003") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Nutella", "3017620422003", direction, 21, Guid.NewGuid(), "Petro",
            new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc)) { DocumentNumber = number };

    [Theory]
    [InlineData(OperationDirection.In, 12, OperationDirection.Out, 9)]
    [InlineData(OperationDirection.In, 30, OperationDirection.In, 9)]
    [InlineData(OperationDirection.Out, 12, OperationDirection.In, 9)]
    [InlineData(OperationDirection.Out, 30, OperationDirection.Out, 9)]
    [InlineData(OperationDirection.In, 0, OperationDirection.Out, 21)]
    public void TheDifference_IsRecordedInTheDirectionThatFixesIt(
        OperationDirection booked, int right, OperationDirection expected, int quantity)
    {
        var correction = OperationCorrection.For(Entry(booked), 21, right, Role.Storekeeper, onHand: 100)!;

        Assert.Equal((expected, quantity, (string?)null), (correction.Direction, correction.Quantity, correction.Refusal));
        Assert.Equal("Correction of R-20261125-0003", correction.Note);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(-1)]
    public void NothingToRecord_WhenTheNumberIsTheSameOrNegative(int right) =>
        Assert.Null(OperationCorrection.For(Entry(), 21, right, Role.Manager, 100));

    [Fact]
    public void ADriver_CorrectsUpwardsOnly()
    {
        Assert.Null(OperationCorrection.For(Entry(), 21, 30, Role.Driver, 100)!.Refusal);
        Assert.Equal(OperationCorrection.AskStaff, OperationCorrection.For(Entry(), 21, 12, Role.Driver, 100)!.Refusal);
    }

    [Fact]
    public void AnIssue_NeverExceedsWhatIsOnHand() =>
        Assert.Equal("Only 6 on hand. Ask the manager to count Nutella.",
            OperationCorrection.For(Entry(), 21, 12, Role.Manager, onHand: 6)!.Refusal);

    [Fact]
    public void AnOperationWithoutADocument_IsNamedByItsTime() =>
        Assert.StartsWith("Correction of operation of 25 Nov 2026", OperationCorrection.NoteFor(Entry(number: null)));
}
