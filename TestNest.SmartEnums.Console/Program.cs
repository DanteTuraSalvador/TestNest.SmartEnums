using TestNest.SmartEnums.Domain.Exceptions;
using TestNest.SmartEnums.Domain.ValueObjects;
using System;

namespace TestNest.SmartEnums.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("🏨 Check-In/Out System Demo 🏨\n");

            try
            {
                DemoEmptyState();
                DemoValidCheckIn();
                DemoInvalidCheckIn();
                DemoCheckInToCheckOut();
                DemoInvalidTransitions();
                DemoCompleteLifecycle();
                DemoImmutability();
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"\n💥 Critical Error: {ex.Message}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine("\n🚀 Demo Complete. Press any key to exit...");
            System.Console.ReadKey();
        }

        static void LogDemoHeader(string demoName, params (string Label, object Value)[] inputs)
        {
            System.Console.ForegroundColor = ConsoleColor.Magenta;
            System.Console.WriteLine($"\n🔹 {demoName}");
            System.Console.ResetColor();
            System.Console.WriteLine("📥 Input Data:");

            foreach (var input in inputs)
            {
                if (input.Value is DateTime dt)
                {
                    System.Console.WriteLine($"  {input.Label}: {dt:yyyy-MM-dd HH:mm:ss.fff} ({dt.Kind})");
                }
                else
                {
                    System.Console.WriteLine($"  {input.Label}: {input.Value}");
                }
            }
            System.Console.WriteLine();
        }

        static void DemoEmptyState()
        {
            LogDemoHeader("1️⃣ EMPTY STATE DEMO");
            var empty = CheckInOut.Empty;
            DisplayCheckInOut(empty);
            System.Console.WriteLine($"Active: {empty.IsActive()}");
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoValidCheckIn()
        {
            var checkInTime = DateTime.UtcNow.AddSeconds(-2);
            var checkOutTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

            LogDemoHeader("2️⃣ VALID CHECK-IN DEMO",
                ("Check-in Time", checkInTime),
                ("Check-out Time", checkOutTime),
                ("Requested Status", CheckInOutStatus.CheckIn));

            var checkIn = CheckInOut.Create(checkInTime, checkOutTime, CheckInOutStatus.CheckIn);
            DisplayCheckInOut(checkIn);
            System.Console.WriteLine($"Active: {checkIn.IsActive()}");
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoInvalidCheckIn()
        {
            var invalidTime = DateTime.Now;  // Local time
            var checkOutTime = DateTime.MinValue;

            LogDemoHeader("3️⃣ INVALID CHECK-IN DEMO",
                ("Invalid Check-in Time", invalidTime),
                ("Check-out Time", checkOutTime),
                ("DateTime.Kind", invalidTime.Kind));

            try
            {
                var invalidCheckIn = CheckInOut.Create(invalidTime, checkOutTime, CheckInOutStatus.CheckIn);
            }
            catch (CheckInOutException ex)
            {
                DisplayError(ex);
            }
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoCheckInToCheckOut()
        {
            var checkInTime = DateTime.UtcNow.AddSeconds(-4);
            var checkOutTime = DateTime.UtcNow;

            LogDemoHeader("4️⃣ CHECK-OUT PROCESS DEMO",
                ("Initial Check-in Time", checkInTime),
                ("Check-out Time", checkOutTime),
                ("Previous Status", CheckInOutStatus.CheckIn));

            var checkIn = CheckInOut.Empty.TransitionTo(
                CheckInOutStatus.CheckIn,
                checkInTime
            );

            var checkOut = checkIn.TransitionTo(
                CheckInOutStatus.CheckOut,
                checkOutTime
            );

            DisplayCheckInOut(checkOut);
            System.Console.WriteLine($"Duration: {checkOut.GetDuration():hh\\:mm\\:ss}");
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoInvalidTransitions()
        {
            var checkInTime = DateTime.UtcNow.AddSeconds(-3);
            var invalidCheckOutTime = DateTime.UtcNow.AddHours(-1);

            LogDemoHeader("5️⃣ INVALID TRANSITIONS DEMO",
                ("Valid Check-in Time", checkInTime),
                ("Invalid Check-out Time", invalidCheckOutTime));

            var checkIn = CheckInOut.Create(
                checkInTime,
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                CheckInOutStatus.CheckIn);

            TryInvalidTransition(checkIn, CheckInOutStatus.None);
            TryInvalidCheckOut(checkIn, invalidCheckOutTime);
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoCompleteLifecycle()
        {
            LogDemoHeader("6️⃣ COMPLETE LIFECYCLE DEMO",
                ("Initial State", CheckInOutStatus.None));

            var state = CheckInOut.Empty;
            
            // None → CheckIn
            var checkInTime = DateTime.UtcNow.AddSeconds(-4);
            LogDemoStep("Transition: None → CheckIn", checkInTime);
            state = state.TransitionTo(CheckInOutStatus.CheckIn, checkInTime);
            DisplayCheckInOut(state);

            // CheckIn → CheckOut
            var checkOutTime = DateTime.UtcNow;
            LogDemoStep("Transition: CheckIn → CheckOut", checkOutTime);
            state = state.TransitionTo(CheckInOutStatus.CheckOut, checkOutTime);
            DisplayCheckInOut(state);
            System.Console.WriteLine($"Duration: {state.GetDuration():hh\\:mm\\:ss}");

            // CheckOut → None
            LogDemoStep("Transition: CheckOut → None", DateTime.MinValue);
            state = state.TransitionTo(
                CheckInOutStatus.None,
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            DisplayCheckInOut(state);
            System.Console.WriteLine("-----------------------------");
        }

        static void DemoImmutability()
        {
            var originalCheckIn = DateTime.UtcNow.AddSeconds(-4);
            var newCheckIn = DateTime.UtcNow.AddSeconds(-2);
            var checkOutTime = DateTime.UtcNow;

            LogDemoHeader("7️⃣ IMMUTABILITY DEMO",
                ("Original Check-in", originalCheckIn),
                ("Updated Check-in", newCheckIn),
                ("Check-out Time", checkOutTime));

            var original = CheckInOut.Empty
                .TransitionTo(CheckInOutStatus.CheckIn, originalCheckIn);

            var updated = original.Update(newCheckIn, checkOutTime, CheckInOutStatus.CheckOut);

            System.Console.WriteLine("Original:");
            DisplayCheckInOut(original);

            System.Console.WriteLine("\nUpdated:");
            DisplayCheckInOut(updated);
            System.Console.WriteLine("-----------------------------");
        }

        static void LogDemoStep(string stepName, DateTime timestamp)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine($"\n🔄 {stepName}");
            System.Console.ResetColor();
            System.Console.WriteLine($"  Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff} ({timestamp.Kind})");
        }

         static void TryInvalidTransition(CheckInOut current, CheckInOutStatus newStatus)
 {
     try
     {
         System.Console.WriteLine($"Attempting invalid transition: {current.Status} → {newStatus}");
         current.TransitionTo(newStatus, DateTime.UtcNow);
     }
     catch (CheckInOutException ex)
     {
         DisplayError(ex);
     }
 }

 static void TryInvalidCheckOut(CheckInOut checkIn, DateTime checkOutTime)
 {
     try
     {
         System.Console.WriteLine($"Attempting check-out with invalid time ({checkOutTime:HH:mm:ss})");
         checkIn.TransitionTo(CheckInOutStatus.CheckOut, checkOutTime);
     }
     catch (CheckInOutException ex)
     {
         DisplayError(ex);
     }
 }

        static void DisplayCheckInOut(CheckInOut cio)
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine($"\n{cio}");
            System.Console.ResetColor();

            // Show actual values instead of debug messages
            System.Console.WriteLine($"Status: {cio.Status}");
            System.Console.WriteLine($"Check-In: {cio.CheckInDateTime:yyyy-MM-dd HH:mm:ss UTC}");
            System.Console.WriteLine($"Check-Out: {(cio.CheckOutDateTime == DateTime.MinValue
                ? "N/A"
                : cio.CheckOutDateTime.ToString("yyyy-MM-dd HH:mm:ss UTC"))}");

            // Optional: Show duration if checked out
            if (cio.Status == CheckInOutStatus.CheckOut)
            {
                System.Console.WriteLine($"Duration: {cio.GetDuration():hh\\:mm\\:ss}");
            }
        }

        static void DisplayError(Exception ex)
 {
     System.Console.ForegroundColor = ConsoleColor.Yellow;
     System.Console.WriteLine($"⚠️ {ex.Message}");
     System.Console.ResetColor();
 }
    }
}