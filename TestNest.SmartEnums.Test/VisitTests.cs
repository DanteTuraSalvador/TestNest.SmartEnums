using TestNest.SmartEnums.Domain.Entities;
using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects;
using TestNest.StronglyTypeId.StronglyTypeIds;
using TestNest.StronglyTypeId.ValueObjects;

namespace TestNest.SmartEnums.Test;

public class VisitTests
{
    // Helper methods
    private static GuestId CreateValidGuestId() => GuestId.New();
    private static VisitPurpose CreateValidPurpose() =>
        VisitPurpose.Create("Business meeting with CEO", VisitCategory.Business);
    private static Address CreateValidLocation() =>
        Address.Create("100 Corporate Plaza", "New York", "NY", "10001", "USA");
    private static Email CreateValidEmail() => Email.Create("visitor@example.com");
    private static PhoneNumber CreateValidPhone() => PhoneNumber.Create("+1", "5551234567");

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateVisit()
    {
        // Arrange
        var guestId = CreateValidGuestId();
        var purpose = CreateValidPurpose();
        var location = CreateValidLocation();

        // Act
        var visit = Visit.Create(guestId, purpose, location);

        // Assert
        Assert.NotNull(visit);
        Assert.NotEqual(VisitId.Empty(), visit.Id);
        Assert.Equal(guestId, visit.GuestId);
        Assert.Equal(purpose, visit.Purpose);
        Assert.Equal(location, visit.Location);
        Assert.True(visit.IsPending());
        Assert.Equal(CheckInOutStatus.None, visit.CheckInOut.Status);
    }

    [Fact]
    public void Create_WithAllOptionalData_ShouldCreateVisitWithAllFields()
    {
        // Arrange
        var guestId = CreateValidGuestId();
        var purpose = CreateValidPurpose();
        var location = CreateValidLocation();
        var email = CreateValidEmail();
        var phone = CreateValidPhone();
        var hostName = "John Smith";

        // Act
        var visit = Visit.Create(guestId, purpose, location, email, phone, hostName);

        // Assert
        Assert.Equal(email, visit.NotificationEmail);
        Assert.Equal(phone, visit.ContactPhone);
        Assert.Equal(hostName, visit.HostName);
    }

    [Fact]
    public void Create_WithEmptyGuestId_ShouldThrowVisitException()
    {
        // Arrange
        var emptyGuestId = GuestId.Empty();
        var purpose = CreateValidPurpose();
        var location = CreateValidLocation();

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() =>
            Visit.Create(emptyGuestId, purpose, location));
        Assert.Equal(VisitException.ErrorCode.InvalidGuestId, ex.Code);
    }

    [Fact]
    public void Create_WithEmptyLocation_ShouldThrowVisitException()
    {
        // Arrange
        var guestId = CreateValidGuestId();
        var purpose = CreateValidPurpose();
        var emptyLocation = Address.Empty;

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() =>
            Visit.Create(guestId, purpose, emptyLocation));
        Assert.Equal(VisitException.ErrorCode.InvalidLocationAddress, ex.Code);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Assert
        Assert.True(visit.CreatedAt >= beforeCreate);
        Assert.True(visit.CreatedAt <= DateTime.UtcNow);
    }

    #endregion

    #region CheckIn Tests

    [Fact]
    public void CheckIn_FromPendingState_ShouldTransitionToCheckedIn()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        visit.CheckIn();

        // Assert
        Assert.Equal(CheckInOutStatus.CheckIn, visit.CheckInOut.Status);
        Assert.True(visit.IsActive());
        Assert.False(visit.IsPending());
        Assert.False(visit.IsCompleted());
    }

    [Fact]
    public void CheckIn_WithSpecificTimestamp_ShouldUseProvidedTimestamp()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-2);

        // Act
        visit.CheckIn(checkInTime);

        // Assert
        Assert.Equal(checkInTime, visit.CheckInOut.CheckInDateTime);
    }

    [Fact]
    public void CheckIn_WhenAlreadyCheckedOut_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        var checkOutTime = DateTime.UtcNow;
        visit.CheckIn(checkInTime);
        visit.CheckOut(checkOutTime);

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.CheckIn());
        Assert.Equal(VisitException.ErrorCode.VisitAlreadyCheckedOut, ex.Code);
    }

    [Fact]
    public void CheckIn_ShouldSetUpdatedAt()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        visit.CheckIn();

        // Assert
        Assert.NotNull(visit.UpdatedAt);
    }

    [Fact]
    public void CheckIn_ShouldReturnSameInstance()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        var result = visit.CheckIn();

        // Assert
        Assert.Same(visit, result);
    }

    #endregion

    #region CheckOut Tests

    [Fact]
    public void CheckOut_FromCheckedInState_ShouldTransitionToCheckedOut()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);

        // Act
        visit.CheckOut();

        // Assert
        Assert.Equal(CheckInOutStatus.CheckOut, visit.CheckInOut.Status);
        Assert.False(visit.IsActive());
        Assert.True(visit.IsCompleted());
    }

    [Fact]
    public void CheckOut_FromPendingState_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.CheckOut());
        Assert.Equal(VisitException.ErrorCode.VisitNotCheckedIn, ex.Code);
    }

    [Fact]
    public void CheckOut_WhenAlreadyCheckedOut_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);
        visit.CheckOut();

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.CheckOut());
        Assert.Equal(VisitException.ErrorCode.VisitAlreadyCheckedOut, ex.Code);
    }

    #endregion

    #region Duration Tests

    [Fact]
    public void GetDuration_ForCompletedVisit_ShouldReturnCorrectDuration()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        var checkOutTime = DateTime.UtcNow;
        visit.CheckIn(checkInTime);
        visit.CheckOut(checkOutTime);

        // Act
        var duration = visit.GetDuration();

        // Assert
        Assert.True(duration.TotalSeconds >= 4);
    }

    [Fact]
    public void GetDuration_ForPendingVisit_ShouldReturnZero()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        var duration = visit.GetDuration();

        // Assert
        Assert.Equal(TimeSpan.Zero, duration);
    }

    [Fact]
    public void GetDuration_ForActiveVisit_ShouldReturnZero()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        visit.CheckIn();

        // Act
        var duration = visit.GetDuration();

        // Assert
        Assert.Equal(TimeSpan.Zero, duration);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_FromCheckedOutState_ShouldTransitionToNone()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);
        visit.CheckOut();
        visit.AssignBadge("V-001");

        // Act
        visit.Reset();

        // Assert
        Assert.Equal(CheckInOutStatus.None, visit.CheckInOut.Status);
        Assert.True(visit.IsPending());
        Assert.Null(visit.BadgeNumber);
    }

    [Fact]
    public void Reset_FromPendingState_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.Reset());
        Assert.Equal(VisitException.ErrorCode.InvalidVisitState, ex.Code);
    }

    [Fact]
    public void Reset_FromCheckedInState_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        visit.CheckIn();

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.Reset());
        Assert.Equal(VisitException.ErrorCode.InvalidVisitState, ex.Code);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void UpdatePurpose_WhenPending_ShouldUpdatePurpose()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var newPurpose = VisitPurpose.Create("Interview for engineering position", VisitCategory.Interview);

        // Act
        visit.UpdatePurpose(newPurpose);

        // Assert
        Assert.Equal(newPurpose, visit.Purpose);
        Assert.NotNull(visit.UpdatedAt);
    }

    [Fact]
    public void UpdatePurpose_WhenActive_ShouldUpdatePurpose()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        visit.CheckIn();
        var newPurpose = VisitPurpose.Create("Changed to interview", VisitCategory.Interview);

        // Act
        visit.UpdatePurpose(newPurpose);

        // Assert
        Assert.Equal(newPurpose, visit.Purpose);
    }

    [Fact]
    public void UpdatePurpose_WhenCompleted_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);
        visit.CheckOut();
        var newPurpose = VisitPurpose.Create("Changed purpose", VisitCategory.Other);

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.UpdatePurpose(newPurpose));
        Assert.Equal(VisitException.ErrorCode.CannotModifyCompletedVisit, ex.Code);
    }

    [Fact]
    public void UpdateLocation_WhenCompleted_ShouldThrowVisitException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);
        visit.CheckOut();
        var newLocation = Address.Create("200 New Street", "Boston", "MA", "02101", "USA");

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() => visit.UpdateLocation(newLocation));
        Assert.Equal(VisitException.ErrorCode.CannotModifyCompletedVisit, ex.Code);
    }

    [Fact]
    public void AssignBadge_ShouldSetBadgeNumber()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        visit.AssignBadge("V-12345");

        // Assert
        Assert.Equal("V-12345", visit.BadgeNumber);
    }

    [Fact]
    public void AssignBadge_WithEmptyBadge_ShouldThrowArgumentException()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => visit.AssignBadge(""));
    }

    [Fact]
    public void UpdateNotificationEmail_ShouldUpdateEmail()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var newEmail = Email.Create("newemail@example.com");

        // Act
        visit.UpdateNotificationEmail(newEmail);

        // Assert
        Assert.Equal(newEmail, visit.NotificationEmail);
    }

    [Fact]
    public void UpdateContactPhone_ShouldUpdatePhone()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var newPhone = PhoneNumber.Create("+44", "7911123456");

        // Act
        visit.UpdateContactPhone(newPhone);

        // Assert
        Assert.Equal(newPhone, visit.ContactPhone);
    }

    [Fact]
    public void UpdateHostName_ShouldUpdateHost()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        visit.UpdateHostName("Jane Doe");

        // Assert
        Assert.Equal("Jane Doe", visit.HostName);
    }

    #endregion

    #region Query Tests

    [Fact]
    public void HasNotificationInfo_WithEmail_ShouldReturnTrue()
    {
        // Arrange
        var visit = Visit.Create(
            CreateValidGuestId(),
            CreateValidPurpose(),
            CreateValidLocation(),
            CreateValidEmail());

        // Act & Assert
        Assert.True(visit.HasNotificationInfo());
    }

    [Fact]
    public void HasNotificationInfo_WithPhone_ShouldReturnTrue()
    {
        // Arrange
        var visit = Visit.Create(
            CreateValidGuestId(),
            CreateValidPurpose(),
            CreateValidLocation(),
            contactPhone: CreateValidPhone());

        // Act & Assert
        Assert.True(visit.HasNotificationInfo());
    }

    [Fact]
    public void HasNotificationInfo_WithNoContactInfo_ShouldReturnFalse()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act & Assert
        Assert.False(visit.HasNotificationInfo());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ForPendingVisit_ShouldIncludePending()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());

        // Act
        var result = visit.ToString();

        // Assert
        Assert.Contains("Pending", result);
        Assert.Contains("Business", result);
    }

    [Fact]
    public void ToString_ForActiveVisit_ShouldIncludeActive()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        visit.CheckIn();

        // Act
        var result = visit.ToString();

        // Assert
        Assert.Contains("Active", result);
    }

    [Fact]
    public void ToString_ForCompletedVisit_ShouldIncludeCompleted()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);
        visit.CheckIn(checkInTime);
        visit.CheckOut();

        // Act
        var result = visit.ToString();

        // Assert
        Assert.Contains("Completed", result);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FluentApi_ShouldAllowMethodChaining()
    {
        // Arrange
        var visit = Visit.Create(CreateValidGuestId(), CreateValidPurpose(), CreateValidLocation());
        var checkInTime = DateTime.UtcNow.AddSeconds(-4);

        // Act
        visit
            .UpdateHostName("John Smith")
            .UpdateNotificationEmail(CreateValidEmail())
            .CheckIn(checkInTime)
            .AssignBadge("V-001")
            .CheckOut();

        // Assert
        Assert.Equal("John Smith", visit.HostName);
        Assert.NotNull(visit.NotificationEmail);
        Assert.Equal("V-001", visit.BadgeNumber);
        Assert.True(visit.IsCompleted());
    }

    #endregion
}
