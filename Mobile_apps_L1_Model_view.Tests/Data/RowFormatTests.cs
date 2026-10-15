using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Data;

public class RowFormatTests
{
    [Fact]
    public void Guid_RoundTripsThroughText()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, RowFormat.ToGuid(RowFormat.Text(id)));
    }

    [Fact]
    public void UtcTime_IsWrittenInAFixedWidthFormat()
    {
        var time = new DateTime(2026, 1, 31, 10, 15, 0, DateTimeKind.Utc);

        Assert.Equal("2026-01-31T10:15:00.0000000Z", RowFormat.Text(time));
    }

    [Fact]
    public void Time_KeepsTicksAndComesBackAsUtc()
    {
        var time = new DateTime(2026, 1, 31, 10, 15, 0, DateTimeKind.Utc).AddTicks(1_234_567);

        var back = RowFormat.ToTime(RowFormat.Text(time));

        Assert.Equal(time.Ticks, back.Ticks);
        Assert.Equal(DateTimeKind.Utc, back.Kind);
    }

    [Fact]
    public void LocalTime_IsStoredAsUtc()
    {
        var local = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Equal(RowFormat.Text(local.ToUniversalTime()), RowFormat.Text(local));
    }

    [Fact]
    public void TextOrder_EqualsTimeOrder()
    {
        var early = new DateTime(2026, 1, 9, 23, 59, 59, DateTimeKind.Utc);
        var late = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        Assert.True(string.CompareOrdinal(RowFormat.Text(early), RowFormat.Text(late)) < 0);
    }

    [Fact]
    public void Enum_IsWrittenByName_AndParsedBack()
    {
        Assert.Equal("Manager", RowFormat.Text(Role.Manager));
        Assert.Equal(Role.Driver, RowFormat.ToEnum<Role>("Driver"));
        Assert.Equal(OperationDirection.Out, RowFormat.ToEnum<OperationDirection>("Out"));
    }

    [Fact]
    public void Enum_UnknownName_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => RowFormat.ToEnum<Role>("Admin"));
    }
}
