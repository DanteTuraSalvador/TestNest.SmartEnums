using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects.Common;

namespace TestNest.SmartEnums.Domain.ValueObjects;
public sealed class CheckInOut : ValueObject
{
    private static readonly Lazy<CheckInOut> _empty = new(() => new CheckInOut());
    public static CheckInOut Empty => _empty.Value;
    public bool IsEmpty => this == Empty;

    public DateTime CheckInDateTime { get; }
    public DateTime CheckOutDateTime { get; }
    public CheckInOutStatus Status { get; }

    private CheckInOut() => (CheckInDateTime, CheckOutDateTime, Status) =
        (DateTime.MinValue, DateTime.MinValue, CheckInOutStatus.None);

    private CheckInOut(DateTime checkIn, DateTime checkOut, CheckInOutStatus status)
    {
        CheckInDateTime = checkIn;
        CheckOutDateTime = checkOut;
        Status = status;
    }

    public static CheckInOut Create(
        DateTime checkIn,
        DateTime checkOut,
        CheckInOutStatus status,
        CheckInOutStatus? previousStatus = null)
    {
        ValidateDateTimeKind(checkIn, checkOut);
        ValidateStatusRules(checkIn, checkOut, status, previousStatus);

        return new CheckInOut(checkIn, checkOut, status);
    }

    private static void ValidateDateTimeKind(DateTime checkIn, DateTime checkOut)
    {
         if (checkIn.Kind != DateTimeKind.Utc || checkOut.Kind != DateTimeKind.Utc)
            throw CheckInOutException.NonUtcDateTime();
    }

    private static void ValidateStatusRules(
        DateTime checkIn,
        DateTime checkOut,
        CheckInOutStatus status,
        CheckInOutStatus? previousStatus)
    {
        switch (status)
        {
            case CheckInOutStatus.CheckIn:
                ValidateCheckIn(checkIn, previousStatus);
                break;

            case CheckInOutStatus.CheckOut:
                ValidateCheckOut(checkIn, checkOut, previousStatus);
                break;

            case CheckInOutStatus.None:
                if (checkIn != DateTime.MinValue || checkOut != DateTime.MinValue)
                    throw CheckInOutException.InvalidNoneState();
                break;

            default:
                throw CheckInOutException.InvalidStatus();
        }
    }

    private static void ValidateCheckIn(DateTime checkIn, CheckInOutStatus? previousStatus)
    {
        var now = DateTime.UtcNow;

        if (checkIn > now.AddYears(1))
            throw CheckInOutException.FutureCheckInTooFar();

        if (previousStatus == null && checkIn < now.AddSeconds(-5))
            throw CheckInOutException.PastCheckInNotAllowed();
    }

    private static void ValidateCheckOut(
        DateTime checkIn,
        DateTime checkOut,
        CheckInOutStatus? previousStatus)
    {
        var now = DateTime.UtcNow;

        if (previousStatus != CheckInOutStatus.CheckIn)
            throw CheckInOutException.CheckInRequiredBeforeCheckOut();

        if (checkOut <= checkIn)
            throw CheckInOutException.InvalidDateRange();

        if (checkIn > now)
            throw CheckInOutException.InvalidStatusTransition();

        if (checkIn < now.AddSeconds(-5))
            throw CheckInOutException.StaleCheckIn();
    }

    public CheckInOut TransitionTo(CheckInOutStatus newStatus, DateTime timestamp)
    {
        return (Status, newStatus) switch
        {
            // None → CheckIn
            (CheckInOutStatus.None, CheckInOutStatus.CheckIn) =>
                Create(
                    timestamp,
                    DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    newStatus
                ),

            // CheckIn → CheckOut
            (CheckInOutStatus.CheckIn, CheckInOutStatus.CheckOut) =>
                Create(
                    CheckInDateTime,
                    timestamp,
                    newStatus,
                    previousStatus: Status // Pass current status as previous
                ),

            // CheckOut → None (Fixed UTC handling)
            (CheckInOutStatus.CheckOut, CheckInOutStatus.None) =>
                Create(
                    DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    newStatus
                ),

            _ => throw CheckInOutException.InvalidStatusTransition()
        };
    }

    //public CheckInOut TransitionTo(CheckInOutStatus newStatus, DateTime timestamp)
    //{
    //    return (Status, newStatus) switch
    //    {
    //        (CheckInOutStatus.None, CheckInOutStatus.CheckIn) =>
    //         Create(
    //             timestamp,
    //             DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), // Explicit UTC
    //             newStatus
    //         ),

    //        (CheckInOutStatus.CheckIn, CheckInOutStatus.CheckOut) =>
    //            Create(
    //                checkIn: CheckInDateTime,
    //                checkOut: timestamp,
    //                status: newStatus,
    //                previousStatus: Status // Add this parameter
    //            ),

    //        (CheckInOutStatus.CheckOut, CheckInOutStatus.None) =>
    //            Create(DateTime.MinValue, DateTime.MinValue, newStatus),

    //        _ => throw CheckInOutException.InvalidStatusTransition()
    //    };
    //}

    public TimeSpan GetDuration()
    {
        return Status == CheckInOutStatus.CheckOut
            ? CheckOutDateTime - CheckInDateTime
            : TimeSpan.Zero;
    }

    public bool IsActive()
    {
        return Status == CheckInOutStatus.CheckIn
            && CheckInDateTime <= DateTime.UtcNow
            && CheckInDateTime > DateTime.MinValue;
    }

    public CheckInOut Update(DateTime newCheckIn, DateTime newCheckOut, CheckInOutStatus newStatus)
        => Create(newCheckIn, newCheckOut, newStatus, Status);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return CheckInDateTime;
        yield return CheckOutDateTime;
        yield return Status;
    }

    public override string ToString()
    {
        return Status switch
        {
            CheckInOutStatus.None => "No check-in recorded",
            CheckInOutStatus.CheckIn => $"Checked in at {CheckInDateTime:u}",
            CheckInOutStatus.CheckOut => $"Checked out at {CheckOutDateTime:u} (Duration: {GetDuration():hh\\:mm})",
            _ => "Invalid status"
        };
    }
}

public enum CheckInOutStatus
{
    None,
    CheckIn,
    CheckOut
}
