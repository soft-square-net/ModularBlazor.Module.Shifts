using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate.ShiftBuilder;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

public class ShiftRules
{
    private static readonly DateTimeOffset Start = TestClock.Start;

    [Fact]
    public void HasAtMostOneLeadAndExactlyOneToPublish()
    {
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start.AddHours(8), [Lead("a"), Lead("b")]));

        var draft = Shift.Create(1, Start, Start.AddHours(8), [As("a", "desk")]); // a draft may wait for its lead
        Should.Throw<DomainException>(draft.Publish);
        draft.Published.ShouldBeFalse();

        draft.SetRota([Lead("b"), As("a", "desk")]);
        draft.Publish();
        Should.Throw<DomainException>(() => draft.SetRota([As("a", "desk")])); // a published shift keeps one lead
        draft.IsLead("b").ShouldBeTrue();
    }

    [Fact]
    public void BelongsToAUnitOrASection()
    {
        Should.Throw<DomainException>(() => Shift.Create(null, Start, Start.AddHours(8), [Lead("a")]));
        Shift.Create(null, Start, Start.AddHours(8), [Lead("a")], Guid.NewGuid()).SectionId.ShouldBeNull();
    }

    [Fact]
    public void EndsAfterItStartsAndLastsAtMostADay()
    {
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start, [Lead("a")]));
        Should.Throw<DomainException>(() => Shift.Create(1, Start, Start.AddHours(25), [Lead("a")]));
    }

    [Fact]
    public void APersonMayHoldSeveralRoles()
    {
        var shift = Shift.Create(1, Start, Start.AddHours(8), [Lead("a"), As("a", "desk"), As("a", "desk")]);

        shift.Members.Select(m => (m.RoleKey, m.IsLead)).ShouldBe([("manager", true), ("desk", false)]);
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
        Should.Throw<DomainException>(() => shift.SetRota([Lead("x")]));
    }
}
