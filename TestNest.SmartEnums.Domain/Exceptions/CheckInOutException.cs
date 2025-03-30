namespace TestNest.SmartEnums.Domain.Exceptions;

public class CheckInOutException : Exception
{
    public enum ErrorCode
    {
        NonUtcDateTime,
        InvalidNoneState,
        FutureCheckInTooFar,
        PastCheckInNotAllowed,
        CheckInRequiredBeforeCheckOut,
        InvalidDateRange,
        InvalidStatusTransition,
        StaleCheckIn,
        InvalidStatus
    }

    public ErrorCode Code { get; }

    private CheckInOutException(ErrorCode code, string message) : base(message)
    {
        Code = code;
    }

    private CheckInOutException(ErrorCode code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }

    public static CheckInOutException NonUtcDateTime() => new(
        ErrorCode.NonUtcDateTime,
        "All timestamps must be in UTC format"
    );

    public static CheckInOutException InvalidNoneState() => new(
        ErrorCode.InvalidNoneState,
        "None status requires minimum datetime values for both check-in and check-out"
    );

    public static CheckInOutException FutureCheckInTooFar() => new(
        ErrorCode.FutureCheckInTooFar,
        "Check-in time cannot be more than 1 year in the future"
    );

    public static CheckInOutException PastCheckInNotAllowed() => new(
        ErrorCode.PastCheckInNotAllowed,
        "Check-in time cannot be more than 5 seconds in the past for new entries"
    );

    public static CheckInOutException CheckInRequiredBeforeCheckOut() => new(
        ErrorCode.CheckInRequiredBeforeCheckOut,
        "Check-in must be completed before check-out"
    );

    public static CheckInOutException InvalidDateRange() => new(
        ErrorCode.InvalidDateRange,
        "Check-out time must be after check-in time"
    );

    public static CheckInOutException InvalidStatusTransition() => new(
        ErrorCode.InvalidStatusTransition,
        "Invalid status transition attempted"
    );

    public static CheckInOutException StaleCheckIn() => new(
        ErrorCode.StaleCheckIn,
        "Check-in timestamp is too old to complete check-out"
    );

    public static CheckInOutException InvalidStatus() => new(
        ErrorCode.InvalidStatus,
        "Invalid check-in/out status provided"
    );

   
}