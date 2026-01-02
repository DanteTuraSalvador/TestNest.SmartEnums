using TestNest.SmartEnums.Domain.ValueObjects.Common;
using TestNest.SmartEnums.Domain.Exceptions;

namespace TestNest.SmartEnums.Domain.ValueObjects;

public sealed class VisitPurpose : ValueObject
{
    private static readonly Lazy<VisitPurpose> _empty = new(() => new VisitPurpose());
    public static VisitPurpose Empty => _empty.Value;

    public bool IsEmpty => this == Empty;

    public string Reason { get; }
    public VisitCategory Category { get; }
    public string? Notes { get; }

    private VisitPurpose()
    {
        Reason = string.Empty;
        Category = VisitCategory.Other;
        Notes = null;
    }

    private VisitPurpose(string reason, VisitCategory category, string? notes)
    {
        Reason = reason;
        Category = category;
        Notes = notes;
    }

    public static VisitPurpose Create(string reason, VisitCategory category, string? notes = null)
    {
        ValidateReason(reason);

        return new VisitPurpose(
            reason.Trim(),
            category,
            notes?.Trim()
        );
    }

    public static bool TryCreate(
        string? reason,
        VisitCategory category,
        string? notes,
        out VisitPurpose? purpose)
    {
        purpose = null;

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            return false;

        if (reason.Trim().Length > 500)
            return false;

        purpose = new VisitPurpose(reason.Trim(), category, notes?.Trim());
        return true;
    }

    public VisitPurpose WithReason(string newReason)
        => Create(newReason, Category, Notes);

    public VisitPurpose WithCategory(VisitCategory newCategory)
        => Create(Reason, newCategory, Notes);

    public VisitPurpose WithNotes(string? newNotes)
        => Create(Reason, Category, newNotes);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Reason;
        yield return Category;
        yield return Notes;
    }

    public override string ToString()
    {
        if (IsEmpty) return "[No Purpose Specified]";
        return $"{Category}: {Reason}";
    }

    private static void ValidateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw VisitException.EmptyPurpose();

        if (reason.Trim().Length < 3)
            throw VisitException.PurposeTooShort();

        if (reason.Trim().Length > 500)
            throw VisitException.PurposeTooLong();
    }
}

public enum VisitCategory
{
    Business,
    Personal,
    Medical,
    Delivery,
    Maintenance,
    Interview,
    Meeting,
    Tour,
    Event,
    Other
}
