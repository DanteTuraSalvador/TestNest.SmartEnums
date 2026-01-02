using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects;
using TestNest.StronglyTypeId.StronglyTypeIds;
using TestNest.StronglyTypeId.ValueObjects;

namespace TestNest.SmartEnums.Domain.Entities;

public sealed class Visit
{
    // Strongly Typed IDs
    public VisitId Id { get; }
    public GuestId GuestId { get; }

    // SmartEnum - state machine for check-in/out tracking
    public CheckInOut CheckInOut { get; private set; }

    // Value Objects
    public VisitPurpose Purpose { get; private set; }
    public Address Location { get; private set; }
    public Email? NotificationEmail { get; private set; }
    public PhoneNumber? ContactPhone { get; private set; }

    // Simple properties
    public string? HostName { get; private set; }
    public string? BadgeNumber { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor - forces use of factory methods
    private Visit(
        VisitId id,
        GuestId guestId,
        VisitPurpose purpose,
        Address location,
        Email? notificationEmail,
        PhoneNumber? contactPhone,
        string? hostName)
    {
        Id = id;
        GuestId = guestId;
        Purpose = purpose;
        Location = location;
        NotificationEmail = notificationEmail;
        ContactPhone = contactPhone;
        HostName = hostName;
        CheckInOut = CheckInOut.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new Visit with a new VisitId
    /// </summary>
    public static Visit Create(
        GuestId guestId,
        VisitPurpose purpose,
        Address location,
        Email? notificationEmail = null,
        PhoneNumber? contactPhone = null,
        string? hostName = null)
    {
        ValidateGuestId(guestId);
        ValidateLocation(location);

        return new Visit(
            VisitId.New(),
            guestId,
            purpose,
            location,
            notificationEmail,
            contactPhone,
            hostName?.Trim()
        );
    }

    /// <summary>
    /// Creates a Visit with an existing VisitId (for reconstitution from persistence)
    /// </summary>
    public static Visit Create(
        VisitId id,
        GuestId guestId,
        VisitPurpose purpose,
        Address location,
        CheckInOut checkInOut,
        Email? notificationEmail = null,
        PhoneNumber? contactPhone = null,
        string? hostName = null,
        string? badgeNumber = null)
    {
        ValidateGuestId(guestId);
        ValidateLocation(location);

        var visit = new Visit(
            id,
            guestId,
            purpose,
            location,
            notificationEmail,
            contactPhone,
            hostName?.Trim()
        );

        visit.CheckInOut = checkInOut;
        visit.BadgeNumber = badgeNumber;

        return visit;
    }

    // ========== CheckInOut State Machine Methods ==========

    /// <summary>
    /// Checks in the guest. Transitions from None -> CheckIn status.
    /// </summary>
    public Visit CheckIn(DateTime? timestamp = null)
    {
        var checkInTime = timestamp ?? DateTime.UtcNow;

        if (CheckInOut.Status == CheckInOutStatus.CheckOut)
            throw VisitException.VisitAlreadyCheckedOut();

        CheckInOut = CheckInOut.TransitionTo(CheckInOutStatus.CheckIn, checkInTime);
        UpdatedAt = DateTime.UtcNow;

        return this;
    }

    /// <summary>
    /// Checks out the guest. Transitions from CheckIn -> CheckOut status.
    /// </summary>
    public Visit CheckOut(DateTime? timestamp = null)
    {
        var checkOutTime = timestamp ?? DateTime.UtcNow;

        if (CheckInOut.Status == CheckInOutStatus.None)
            throw VisitException.VisitNotCheckedIn();

        if (CheckInOut.Status == CheckInOutStatus.CheckOut)
            throw VisitException.VisitAlreadyCheckedOut();

        CheckInOut = CheckInOut.TransitionTo(CheckInOutStatus.CheckOut, checkOutTime);
        UpdatedAt = DateTime.UtcNow;

        return this;
    }

    /// <summary>
    /// Resets the visit for a new check-in cycle. Transitions from CheckOut -> None.
    /// </summary>
    public Visit Reset()
    {
        if (CheckInOut.Status != CheckInOutStatus.CheckOut)
            throw VisitException.InvalidVisitState();

        CheckInOut = CheckInOut.TransitionTo(
            CheckInOutStatus.None,
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)
        );
        UpdatedAt = DateTime.UtcNow;
        BadgeNumber = null;

        return this;
    }

    // ========== Update Methods ==========

    public Visit UpdatePurpose(VisitPurpose newPurpose)
    {
        EnsureCanModify();
        Purpose = newPurpose;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Visit UpdateLocation(Address newLocation)
    {
        EnsureCanModify();
        ValidateLocation(newLocation);
        Location = newLocation;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Visit UpdateNotificationEmail(Email? newEmail)
    {
        NotificationEmail = newEmail;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Visit UpdateContactPhone(PhoneNumber? newPhone)
    {
        ContactPhone = newPhone;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Visit UpdateHostName(string? newHostName)
    {
        HostName = newHostName?.Trim();
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    public Visit AssignBadge(string badgeNumber)
    {
        if (string.IsNullOrWhiteSpace(badgeNumber))
            throw new ArgumentException("Badge number cannot be empty", nameof(badgeNumber));

        BadgeNumber = badgeNumber.Trim();
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    // ========== Query Methods ==========

    /// <summary>
    /// Returns true if the guest is currently checked in (active visit).
    /// </summary>
    public bool IsActive() => CheckInOut.IsActive();

    /// <summary>
    /// Returns true if the visit has been completed (checked out).
    /// </summary>
    public bool IsCompleted() => CheckInOut.Status == CheckInOutStatus.CheckOut;

    /// <summary>
    /// Returns true if the visit has not started yet.
    /// </summary>
    public bool IsPending() => CheckInOut.Status == CheckInOutStatus.None;

    /// <summary>
    /// Gets the duration of the visit (only valid for completed visits).
    /// </summary>
    public TimeSpan GetDuration() => CheckInOut.GetDuration();

    /// <summary>
    /// Returns true if notification contact info is available.
    /// </summary>
    public bool HasNotificationInfo() =>
        (NotificationEmail != null && !NotificationEmail.IsEmpty()) ||
        (ContactPhone != null && !ContactPhone.IsEmpty());

    // ========== Validation Methods ==========

    private void EnsureCanModify()
    {
        if (IsCompleted())
            throw VisitException.CannotModifyCompletedVisit();
    }

    private static void ValidateGuestId(GuestId guestId)
    {
        if (guestId == GuestId.Empty())
            throw VisitException.InvalidGuestId();
    }

    private static void ValidateLocation(Address location)
    {
        if (location.IsEmpty())
            throw VisitException.InvalidLocationAddress();
    }

    // ========== ToString ==========

    public override string ToString()
    {
        var status = CheckInOut.Status switch
        {
            CheckInOutStatus.None => "Pending",
            CheckInOutStatus.CheckIn => "Active",
            CheckInOutStatus.CheckOut => $"Completed ({GetDuration():hh\\:mm})",
            _ => "Unknown"
        };

        return $"Visit {Id}: {Purpose.Category} - {status}";
    }
}
