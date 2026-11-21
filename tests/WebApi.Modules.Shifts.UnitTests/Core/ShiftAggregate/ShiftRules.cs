using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

public class ShiftRules
{
    private static readonly DateTimeOffset Start = TestClock.Start;

    [Fact]
    public void NeedsExactlyOneManager()
    {
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start.AddHours(8), [("a", ShiftRole.Desk)]));
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start.AddHours(8), [("a", ShiftRole.Manager), ("b", ShiftRole.Manager)]));
    }

    [Fact]
    public void EndsAfterItStartsAndLastsAtMostADay()
    {
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start, [("a", ShiftRole.Manager)]));
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start.AddHours(25), [("a", ShiftRole.Manager)]));
    }

    [Fact]
    public void APersonMayHoldSeveralRoles()
    {
        var shift = Shift.Create(1, Start, Start.AddHours(8), [("a", ShiftRole.Manager), ("a", ShiftRole.Desk), ("a", ShiftRole.Desk)]);

        shift.Members.Select(m => m.Role).ShouldBe([ShiftRole.Manager, ShiftRole.Desk]);
    }

    [Fact]
    public void RunsBetweenStartAndEndUntilCheckedOut()
    {
        var shift = ShiftBuilder.Planned(Start);

        shift.IsRunning(Start.AddMinutes(-1)).ShouldBeFalse();
        shift.IsRunning(Start.AddHours(1)).ShouldBeTrue();
        shift.CheckOut(ShiftBuilder.Manager, Start.AddHours(2));
        shift.IsRunning(Start.AddHours(3)).ShouldBeFalse();
    }

    [Fact]
    public void AMemberWhoChecksOutLosesAccessThen()
    {
        var shift = ShiftBuilder.Planned(Start);

        shift.CheckOutMember(ShiftBuilder.Editor, Start.AddHours(3));

        var editor = shift.Members.Single(m => m.UserId == ShiftBuilder.Editor);
        shift.EndOf(editor).ShouldBe(Start.AddHours(3));
        shift.EndOf(shift.Members.Single(m => m.UserId == ShiftBuilder.Desk)).ShouldBe(Start.AddHours(8));
    }

    [Fact]
    public void CheckingOutTheShiftEndsEveryonesAccess()
    {
        var shift = ShiftBuilder.Planned(Start);

        shift.CheckOut(ShiftBuilder.Manager, Start.AddHours(6));

        shift.CheckedOutBy.ShouldBe(ShiftBuilder.Manager);
        shift.Members.ShouldAllBe(m => shift.EndOf(m) == Start.AddHours(6));
    }

    [Fact]
    public void CheckOutAfterTheEndStopsAtTheEnd()
    {
        var shift = ShiftBuilder.Planned(Start);

        shift.CheckOut("workflow", Start.AddHours(9));

        shift.CheckedOutAt.ShouldBe(Start.AddHours(8));
    }

    [Fact]
    public void NothingChecksOutBeforeTheStartOrTwice()
    {
        var shift = ShiftBuilder.Planned(Start);

        Should.Throw<DomainException>(() => shift.CheckOut(ShiftBuilder.Manager, Start.AddMinutes(-5)));
        Should.Throw<DomainException>(() => shift.CheckOutMember(ShiftBuilder.Desk, Start.AddMinutes(-5)));
        shift.CheckOut(ShiftBuilder.Manager, Start.AddHours(1));
        Should.Throw<DomainException>(() => shift.CheckOut(ShiftBuilder.Manager, Start.AddHours(2)));
        Should.Throw<DomainException>(() => shift.SetRota([("x", ShiftRole.Manager)]));
    }
}
