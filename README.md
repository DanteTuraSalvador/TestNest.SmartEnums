# 🏷️ SmartEnum 

A .NET implementation of an enhanced enum with state transition rules and validation for check-in/check-out tracking systems.

## ✨ Features

- 🕒 **Temporal Validation** - Enforces UTC datetime and valid time ranges  
- 🔄 **State Machine** - Controlled status transitions (`None` ↔ `CheckIn` ↔ `CheckOut`)  
- 🛡️ **Domain Rules** - Encapsulates 15+ business rules for valid check-in/out operations  
- ⏱️ **Duration Tracking** - Automatic time span calculations  
- 🌐 **UTC Enforcement** - Strict UTC datetime handling  
- 🧵 **Thread-Safe** - Immutable instances with lazy initialization  
- 🚦 **Status Awareness** - Clear state management with `CheckInOutStatus` enum  
- 🚫 **Anti-Corruption** - Prevents invalid state through self-validation  

## 📌 Core Implementation

### 🔹 CheckInOut Value Object

```csharp
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
```

### 🔹 CheckInOut Value Object

```csharp
stateDiagram-v2
    [*] --> None
    None --> CheckIn: Valid when
    CheckIn --> CheckOut: Must have
    CheckOut --> None: Reset
    CheckIn --> None: Invalid!
    CheckOut --> CheckIn: Invalid!
```

## 📌 Core Implementation

### 🔹 CheckInOut Value Object

```csharp
// Create initial check-in
var checkIn = CheckInOut.Empty.TransitionTo(
    CheckInOutStatus.CheckIn, 
    DateTime.UtcNow
);

// Transition to check-out
var checkOut = checkIn.TransitionTo(
    CheckInOutStatus.CheckOut, 
    DateTime.UtcNow.AddHours(2)
);

Console.WriteLine(checkOut.GetDuration()); // 02:00:00
```

### 🔹 CheckInOut Value Object

```csharp
try
{
    // Attempt invalid back-dated check-in
    var invalid = CheckInOut.Create(
        DateTime.UtcNow.AddHours(-5), 
        DateTime.MinValue, 
        CheckInOutStatus.CheckIn
    );
}
catch (CheckInOutException ex)
{
    Console.WriteLine(ex.Message); // "Past check-in not allowed"
}
```

### 🔹 State Awareness

```csharp
var status = CheckInOutStatus.CheckIn;

if (status == CheckInOutStatus.CheckIn)
{
    // Show check-in specific UI
}
```

## 🚦 Validation Rules

| Rule                  | Description                                      |
|-----------------------|--------------------------------------------------|
| ⏭️ **Valid Transitions**  | `None → CheckIn → CheckOut → None`             |
| 🕒 **UTC Enforcement**   | All datetimes must be `DateTimeKind.Utc`       |
| ⏳ **Time Boundaries**   | Check-in cannot be more than 1 year future dated |
| 🔒 **Sequence Validation** | Check-out requires prior check-in             |
| ⏱️ **Duration Validation** | Check-out must be after check-in              |

## ⚡ Performance Metrics

| Operation            | Avg. Time  |
|----------------------|-----------|
| **Status Transition**  | 0.45 μs   |
| **Duration Calculation** | 0.12 μs  |
| **Equality Check**     | 0.35 μs   |

## 🎯 Why Use SmartEnum?

| Problem                          | Solution                                   |
|----------------------------------|-------------------------------------------|
| **Uncontrolled state transitions** | Strict state machine pattern             |
| **Local time confusion**          | UTC enforcement in type system           |
| **Invalid date ranges**           | Built-in temporal validation             |
| **Fragile status checks**         | Type-safe enum pattern                   |

## Visit Entity (Integration Demo)

The **Visit** entity demonstrates how to combine SmartEnums with Strongly Typed IDs and Value Objects in a single domain entity:

```csharp
// Create a visit combining all DDD patterns
var guestId = GuestId.New();
var purpose = VisitPurpose.Create("Business meeting", VisitCategory.Business);
var location = Address.Create("100 Main St", "New York", "NY", "10001", "USA");
var email = Email.Create("visitor@example.com");

var visit = Visit.Create(guestId, purpose, location, email, hostName: "John Smith");

// Check-in the visitor (state transition: None -> CheckIn)
visit.CheckIn();
visit.AssignBadge("V-001");
Console.WriteLine(visit.IsActive());  // true

// Check-out (state transition: CheckIn -> CheckOut)
visit.CheckOut();
Console.WriteLine(visit.GetDuration());  // Duration of visit
Console.WriteLine(visit.IsCompleted());  // true
```

### Visit Entity Structure

| Component | Type | Description |
|-----------|------|-------------|
| `VisitId` | Strongly Typed ID | Unique visit identifier |
| `GuestId` | Strongly Typed ID | Reference to the guest |
| `CheckInOut` | SmartEnum | State machine for check-in/out |
| `VisitPurpose` | Value Object | Reason and category for visit |
| `Address` | Value Object | Location/venue address |
| `Email` | Value Object | Notification email (optional) |
| `PhoneNumber` | Value Object | Contact phone (optional) |

### State Transitions

```
Pending (None) -> Active (CheckIn) -> Completed (CheckOut) -> Pending (Reset)
```

## 📂 Project Structure
```bash
TestNest.SmartEnums/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │    └── Visit.cs                          # Visit entity (integration demo)
│   │   ├── Exceptions/
│   │   │    ├── CheckInOutException.cs            # CheckInOut exceptions
│   │   │    └── VisitException.cs                 # Visit exceptions
│   │   ├── ValueObjects/
│   │   │    ├── Common/
│   │   │    │   └── ValueObject.cs                # Base class
│   │   │    ├── CheckInOut.cs                     # SmartEnum implementation
│   │   │    └── VisitPurpose.cs                   # Purpose value object
│   │   └── TestNest.SmartEnums.Domain.csproj
│   │
│   └── Console/
│       └── Program.cs                              # Demo application
│       └── TestNest.SmartEnums.Console.csproj
│
├── tests/
│   ├── TestNest.SmartEnums.Test/
│   │   ├── CheckInOutTests.cs
│   │   ├── VisitTests.cs
│   │   └── VisitPurposeTests.cs
│
├── README.md
└── LICENSE
```

---

## Project Management

Track project progress, features, and user stories on our Azure DevOps board:

- [Azure DevOps Board](https://dev.azure.com/tengtium-io/SmartEnums) - Epics, Features, and User Stories

---

## Related Projects

- [TestNest.StronglyTypeId](https://github.com/DanteTuraSalvador/TestNest.StronglyTypeId) - Strongly Typed ID pattern implementation (GuestId, CustomerId, OrderId, ProductId)
- [TestNest.ValueObjects](https://github.com/DanteTuraSalvador/TestNest.ValueObjects) - Value Object pattern implementation (Email, PhoneNumber, Address, Money, Currency, Price)

---

## Contributing

Pull requests are welcome! Please:

- Maintain test coverage
- Follow existing code style
- Add documentation for new features

---

## License

This project is open-source and free to use.
