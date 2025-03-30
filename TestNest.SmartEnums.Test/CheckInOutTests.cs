using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects;

namespace TestNest.SmartEnums.Test;
public class CheckInOutTests
{
    private readonly DateTime _now = DateTime.UtcNow;
    private const int AllowedSeconds = 5;

    private DateTime UtcMinValue => DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    #region Creation Tests
    [Fact]
    public void Create_ValidCheckIn_ReturnsCheckInStatus()
    {
        // Arrange
        var checkInTime = _now;
        var checkOutTime = UtcMinValue;

        // Act
        var result = CheckInOut.Create(checkInTime, checkOutTime, CheckInOutStatus.CheckIn);

        // Assert
        Assert.Equal(CheckInOutStatus.CheckIn, result.Status);
        Assert.Equal(checkInTime, result.CheckInDateTime);
        Assert.Equal(checkOutTime, result.CheckOutDateTime);
    }

    [Fact]
    public void Create_ValidCheckOut_ReturnsCheckOutStatus()
    {
        // Arrange
        var checkInTime = DateTime.UtcNow.AddSeconds(-4); // Within 5-second window
        var checkOutTime = DateTime.UtcNow;

        // Act
        var result = CheckInOut.Create(
            checkInTime,
            checkOutTime,
            CheckInOutStatus.CheckOut,
            CheckInOutStatus.CheckIn);

        // Assert
        Assert.Equal(CheckInOutStatus.CheckOut, result.Status);
        Assert.Equal(checkInTime, result.CheckInDateTime);
        Assert.Equal(checkOutTime, result.CheckOutDateTime);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcDateTime_ThrowsException(DateTimeKind kind)
    {
        // Arrange
        var invalidTime = new DateTime(_now.Ticks, kind);

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(invalidTime, UtcMinValue, CheckInOutStatus.CheckIn));
    }

    [Fact]
    public void Create_CheckInTooFarInFuture_ThrowsException()
    {
        // Arrange
        var futureTime = _now.AddYears(2);
        var checkOutTime = UtcMinValue;

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(futureTime, checkOutTime, CheckInOutStatus.CheckIn));
    }

    [Fact]
    public void Create_CheckInTooFarInPast_ThrowsException()
    {
        // Arrange
        var pastTime = _now.AddSeconds(-AllowedSeconds - 1);
        var checkOutTime = UtcMinValue;

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(pastTime, checkOutTime, CheckInOutStatus.CheckIn));
    }
    #endregion

    #region Transition Tests
    [Fact]
    public void Transition_FromNoneToCheckIn_Success()
    {
        // Arrange
        var initial = CheckInOut.Empty;
        var timestamp = DateTime.UtcNow; // Fresh UTC timestamp

        // Act
        var result = initial.TransitionTo(CheckInOutStatus.CheckIn, timestamp);

        // Assert
        Assert.Equal(CheckInOutStatus.CheckIn, result.Status);
        Assert.Equal(timestamp, result.CheckInDateTime);
        Assert.Equal(
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            result.CheckOutDateTime
        );
    }

    [Fact]
    public void Transition_FromCheckInToCheckOut_Success()
    {
        // Arrange
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        var checkOutTime = DateTime.UtcNow;

        var initial = CheckInOut.Create(
            checkInTime,
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            CheckInOutStatus.CheckIn);

        // Act
        var result = initial.TransitionTo(
            CheckInOutStatus.CheckOut,
            checkOutTime);

        // Assert
        Assert.Equal(CheckInOutStatus.CheckOut, result.Status);
        Assert.Equal(checkInTime, result.CheckInDateTime);
        Assert.Equal(checkOutTime, result.CheckOutDateTime);
    }

    [Fact]
    public void Transition_InvalidStateChange_ThrowsException()
    {
        // Arrange
        var initial = CheckInOut.Create(_now, UtcMinValue, CheckInOutStatus.CheckIn);

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            initial.TransitionTo(CheckInOutStatus.None, _now));
    }
    #endregion

    #region Method Tests
    [Fact]
    public void GetDuration_ForCheckOut_ReturnsCorrectDuration()
    {
        // Arrange
        var checkInTime = DateTime.UtcNow;
        var checkOutTime = checkInTime.AddSeconds(4); // Exact 4 second difference

        var co = CheckInOut.Create(
            checkInTime,
            checkOutTime,
            CheckInOutStatus.CheckOut,
            CheckInOutStatus.CheckIn);

        // Act
        var duration = co.GetDuration();

        // Assert
        Assert.Equal(4, duration.TotalSeconds, precision: 0); // Whole seconds only
    }

    [Fact]
    public void GetDuration_ForNonCheckOut_ReturnsZero()
    {
        // Arrange
        var co = CheckInOut.Create(_now, UtcMinValue, CheckInOutStatus.CheckIn);

        // Act
        var duration = co.GetDuration();

        // Assert
        Assert.Equal(TimeSpan.Zero, duration);
    }

    [Fact]
    public void IsActive_ForValidCheckIn_ReturnsTrue()
    {
        // Arrange
        var validCheckInTime = DateTime.UtcNow.AddSeconds(-4); // Within 5-second window
        var co = CheckInOut.Create(
            validCheckInTime,
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            CheckInOutStatus.CheckIn);

        // Act & Assert
        Assert.True(co.IsActive());
    }

    [Fact]
    public void IsActive_ForFutureCheckIn_ReturnsFalse()
    {
        // Arrange
        var co = CheckInOut.Create(_now.AddMinutes(5), UtcMinValue, CheckInOutStatus.CheckIn);

        // Act & Assert
        Assert.False(co.IsActive());
    }
    #endregion

    #region Update Tests
    [Fact]
    public void Update_ChangesValuesCorrectly()
    {
        // Arrange
        // Original check-in within 5-second window
        var originalCheckIn = DateTime.UtcNow.AddSeconds(-4);
        var original = CheckInOut.Create(
            originalCheckIn,
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            CheckInOutStatus.CheckIn
        );

        // New check-in still within 5-second window
        var newCheckIn = DateTime.UtcNow.AddSeconds(-2);
        var checkOutTime = DateTime.UtcNow;

        // Act
        var updated = original.Update(
            newCheckIn,
            checkOutTime,
            CheckInOutStatus.CheckOut
        );

        // Assert
        Assert.Equal(newCheckIn, updated.CheckInDateTime);
        Assert.Equal(checkOutTime, updated.CheckOutDateTime);
        Assert.Equal(CheckInOutStatus.CheckOut, updated.Status);
    }

    [Fact]
    public void Update_LeavesOriginalInstanceUnchanged()
    {
        // Arrange
        // Create original instance with valid check-in time
        var originalCheckIn = DateTime.UtcNow.AddSeconds(-4); // Within 5-second window
        var original = CheckInOut.Create(
            originalCheckIn,
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            CheckInOutStatus.CheckIn,
            previousStatus: CheckInOutStatus.None // Transition from "None"
        );

        // New check-in time still within valid window
        var newCheckIn = DateTime.UtcNow.AddSeconds(-2);
        var checkOutTime = DateTime.UtcNow;

        // Act
        var updated = original.Update(newCheckIn, checkOutTime, CheckInOutStatus.CheckOut);

        // Assert
        Assert.NotEqual(original.CheckInDateTime, updated.CheckInDateTime);
        Assert.NotEqual(original.Status, updated.Status);
    }
    #endregion

    #region Edge Cases
    [Fact]
    public void Empty_HasCorrectDefaultValues()
    {
        // Act
        var empty = CheckInOut.Empty;

        // Assert
        Assert.Equal(UtcMinValue, empty.CheckInDateTime);
        Assert.Equal(UtcMinValue, empty.CheckOutDateTime);
        Assert.Equal(CheckInOutStatus.None, empty.Status);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public void ToString_ForCheckIn_ReturnsFormattedString()
    {
        // Arrange
        var co = CheckInOut.Create(_now, UtcMinValue, CheckInOutStatus.CheckIn);

        // Act
        var result = co.ToString();

        // Assert
        Assert.Contains(_now.ToString("u"), result);
        Assert.Contains("Checked in", result);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var co1 = CheckInOut.Create(_now, UtcMinValue, CheckInOutStatus.CheckIn);
        var co2 = CheckInOut.Create(_now, UtcMinValue, CheckInOutStatus.CheckIn);

        // Act & Assert
        Assert.Equal(co1, co2);
        Assert.True(co1 == co2);
    }
    #endregion

    #region Validation Tests
    [Fact]
    public void Create_CheckOutWithoutCheckIn_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(_now, _now.AddHours(1), CheckInOutStatus.CheckOut));
    }

    [Fact]
    public void Create_CheckOutBeforeCheckIn_ThrowsException()
    {
        // Arrange
        var checkOut = _now.AddHours(-1);

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(_now, checkOut, CheckInOutStatus.CheckOut, CheckInOutStatus.CheckIn));
    }

    [Fact]
    public void Create_InvalidNoneState_ThrowsException()
    {
        // Arrange
        var checkIn = _now;
        var checkOut = UtcMinValue;

        // Act & Assert
        Assert.Throws<CheckInOutException>(() =>
            CheckInOut.Create(checkIn, checkOut, CheckInOutStatus.None));
    }
    #endregion
}