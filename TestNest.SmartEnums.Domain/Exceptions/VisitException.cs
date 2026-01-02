namespace TestNest.SmartEnums.Domain.Exceptions;

public class VisitException : Exception
{
    public enum ErrorCode
    {
        EmptyPurpose,
        PurposeTooShort,
        PurposeTooLong,
        InvalidGuestId,
        VisitNotCheckedIn,
        VisitAlreadyCheckedOut,
        InvalidLocationAddress,
        CannotModifyCompletedVisit,
        InvalidVisitState
    }

    public ErrorCode Code { get; }

    private VisitException(ErrorCode code, string message) : base(message)
    {
        Code = code;
    }

    public static VisitException EmptyPurpose() => new(
        ErrorCode.EmptyPurpose,
        "Visit purpose cannot be empty"
    );

    public static VisitException PurposeTooShort() => new(
        ErrorCode.PurposeTooShort,
        "Visit purpose must be at least 3 characters"
    );

    public static VisitException PurposeTooLong() => new(
        ErrorCode.PurposeTooLong,
        "Visit purpose cannot exceed 500 characters"
    );

    public static VisitException InvalidGuestId() => new(
        ErrorCode.InvalidGuestId,
        "Guest ID cannot be empty"
    );

    public static VisitException VisitNotCheckedIn() => new(
        ErrorCode.VisitNotCheckedIn,
        "Cannot check out - visit has not been checked in"
    );

    public static VisitException VisitAlreadyCheckedOut() => new(
        ErrorCode.VisitAlreadyCheckedOut,
        "Visit has already been checked out"
    );

    public static VisitException InvalidLocationAddress() => new(
        ErrorCode.InvalidLocationAddress,
        "Location address cannot be empty"
    );

    public static VisitException CannotModifyCompletedVisit() => new(
        ErrorCode.CannotModifyCompletedVisit,
        "Cannot modify a completed visit"
    );

    public static VisitException InvalidVisitState() => new(
        ErrorCode.InvalidVisitState,
        "Invalid visit state for this operation"
    );
}
