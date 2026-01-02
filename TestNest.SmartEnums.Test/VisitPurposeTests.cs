using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects;

namespace TestNest.SmartEnums.Test;

public class VisitPurposeTests
{
    #region Creation Tests

    [Fact]
    public void Create_WithValidData_ShouldCreatePurpose()
    {
        // Arrange
        var reason = "Business meeting";
        var category = VisitCategory.Business;

        // Act
        var purpose = VisitPurpose.Create(reason, category);

        // Assert
        Assert.Equal(reason, purpose.Reason);
        Assert.Equal(category, purpose.Category);
        Assert.False(purpose.IsEmpty);
    }

    [Fact]
    public void Create_WithNotes_ShouldIncludeNotes()
    {
        // Arrange
        var reason = "Interview";
        var category = VisitCategory.Interview;
        var notes = "Engineering position - 2nd round";

        // Act
        var purpose = VisitPurpose.Create(reason, category, notes);

        // Assert
        Assert.Equal(notes, purpose.Notes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyReason_ShouldThrowException(string? reason)
    {
        // Act & Assert
        var ex = Assert.Throws<VisitException>(() =>
            VisitPurpose.Create(reason!, VisitCategory.Business));
        Assert.Equal(VisitException.ErrorCode.EmptyPurpose, ex.Code);
    }

    [Fact]
    public void Create_WithTooShortReason_ShouldThrowException()
    {
        // Arrange
        var shortReason = "AB";

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() =>
            VisitPurpose.Create(shortReason, VisitCategory.Business));
        Assert.Equal(VisitException.ErrorCode.PurposeTooShort, ex.Code);
    }

    [Fact]
    public void Create_WithTooLongReason_ShouldThrowException()
    {
        // Arrange
        var longReason = new string('x', 501);

        // Act & Assert
        var ex = Assert.Throws<VisitException>(() =>
            VisitPurpose.Create(longReason, VisitCategory.Business));
        Assert.Equal(VisitException.ErrorCode.PurposeTooLong, ex.Code);
    }

    #endregion

    #region Empty Tests

    [Fact]
    public void Empty_ShouldReturnEmptyInstance()
    {
        // Act
        var empty = VisitPurpose.Empty;

        // Assert
        Assert.True(empty.IsEmpty);
        Assert.Equal(string.Empty, empty.Reason);
        Assert.Equal(VisitCategory.Other, empty.Category);
        Assert.Null(empty.Notes);
    }

    [Fact]
    public void Empty_ShouldReturnSameSingletonInstance()
    {
        // Act
        var empty1 = VisitPurpose.Empty;
        var empty2 = VisitPurpose.Empty;

        // Assert
        Assert.Same(empty1, empty2);
    }

    #endregion

    #region Immutable Update Tests

    [Fact]
    public void WithCategory_ShouldReturnNewInstanceWithUpdatedCategory()
    {
        // Arrange
        var original = VisitPurpose.Create("Meeting", VisitCategory.Business);

        // Act
        var updated = original.WithCategory(VisitCategory.Personal);

        // Assert
        Assert.Equal(VisitCategory.Personal, updated.Category);
        Assert.Equal(VisitCategory.Business, original.Category);
    }

    [Fact]
    public void WithReason_ShouldReturnNewInstanceWithUpdatedReason()
    {
        // Arrange
        var original = VisitPurpose.Create("Meeting", VisitCategory.Business);

        // Act
        var updated = original.WithReason("Conference");

        // Assert
        Assert.Equal("Conference", updated.Reason);
        Assert.Equal("Meeting", original.Reason);
    }

    [Fact]
    public void WithNotes_ShouldReturnNewInstanceWithUpdatedNotes()
    {
        // Arrange
        var original = VisitPurpose.Create("Meeting", VisitCategory.Business, "Original notes");

        // Act
        var updated = original.WithNotes("Updated notes");

        // Assert
        Assert.Equal("Updated notes", updated.Notes);
        Assert.Equal("Original notes", original.Notes);
    }

    #endregion

    #region TryCreate Tests

    [Fact]
    public void TryCreate_WithValidData_ShouldReturnTrue()
    {
        // Act
        var result = VisitPurpose.TryCreate("Meeting", VisitCategory.Business, null, out var purpose);

        // Assert
        Assert.True(result);
        Assert.NotNull(purpose);
        Assert.Equal("Meeting", purpose.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB")]
    public void TryCreate_WithInvalidData_ShouldReturnFalse(string? reason)
    {
        // Act
        var result = VisitPurpose.TryCreate(reason, VisitCategory.Business, null, out var purpose);

        // Assert
        Assert.False(result);
        Assert.Null(purpose);
    }

    [Fact]
    public void TryCreate_WithTooLongReason_ShouldReturnFalse()
    {
        // Arrange
        var longReason = new string('x', 501);

        // Act
        var result = VisitPurpose.TryCreate(longReason, VisitCategory.Business, null, out var purpose);

        // Assert
        Assert.False(result);
        Assert.Null(purpose);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var purpose1 = VisitPurpose.Create("Meeting", VisitCategory.Business, "Notes");
        var purpose2 = VisitPurpose.Create("Meeting", VisitCategory.Business, "Notes");

        // Assert
        Assert.Equal(purpose1, purpose2);
        Assert.True(purpose1 == purpose2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var purpose1 = VisitPurpose.Create("Meeting", VisitCategory.Business);
        var purpose2 = VisitPurpose.Create("Interview", VisitCategory.Interview);

        // Assert
        Assert.NotEqual(purpose1, purpose2);
        Assert.True(purpose1 != purpose2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ForValidPurpose_ShouldReturnFormattedString()
    {
        // Arrange
        var purpose = VisitPurpose.Create("Meeting with CEO", VisitCategory.Business);

        // Act
        var result = purpose.ToString();

        // Assert
        Assert.Equal("Business: Meeting with CEO", result);
    }

    [Fact]
    public void ToString_ForEmpty_ShouldReturnPlaceholder()
    {
        // Arrange
        var empty = VisitPurpose.Empty;

        // Act
        var result = empty.ToString();

        // Assert
        Assert.Equal("[No Purpose Specified]", result);
    }

    #endregion
}
