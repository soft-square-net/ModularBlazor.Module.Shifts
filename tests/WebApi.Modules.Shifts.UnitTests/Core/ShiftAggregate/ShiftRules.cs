using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate.ShiftBuilder;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

public class ShiftRules
{
    private static readonly DateTimeOffset Start = TestClock.Start;
    private static readonly AttendanceThresholds T = AttendanceThresholds.Default;

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
        var shift = Published(Start);

        shift.IsRunning(Start.AddMinutes(-1)).ShouldBeFalse();
        shift.IsRunning(Start.AddHours(1)).ShouldBeTrue();
        shift.CheckOut(Manager, Start.AddHours(2));
        shift.IsRunning(Start.AddHours(3)).ShouldBeFalse();
        shift.Ended.ShouldBeTrue();
        shift.EndedAt.ShouldBe(Start.AddHours(2));
        shift.EndedBy.ShouldBe(Manager);
    }

    [Fact]
    public void NobodyHasAccessWithoutCheckingIn()
    {
        var shift = Published(Start);

        shift.Members.ShouldAllBe(m => m.AccessOn(shift, T.CheckoutGrace) == null);
    }

    [Fact]
    public void AccessRunsFromCheckInToCheckOutOrToTheEndPlusTheGrace()
    {
        var shift = Published(Start);
        shift.CheckIn(Editor, Editor, Start.AddMinutes(-20), T);
        shift.CheckIn(Desk, Manager, Start.AddMinutes(5), T); // the lead checks the desk in

        shift.CheckOutMember(Editor, Editor, Start.AddHours(3));

        var editor = shift.Members.Single(m => m.UserId == Editor);
        editor.AccessOn(shift, T.CheckoutGrace).ShouldBe(new AccessWindow(Start.AddMinutes(-20), Start.AddHours(3)));
        var desk = shift.Members.Single(m => m.UserId == Desk);
        desk.CheckedInBy.ShouldBe(Manager);
        desk.AccessOn(shift, T.CheckoutGrace).ShouldBe(new AccessWindow(Start.AddMinutes(5), Start.AddHours(8).AddMinutes(15)));
    }

    [Fact]
    public void CheckInOpensEarlyAndClosesAtTheEnd()
    {
        var shift = Published(Start);

        Should.Throw<DomainException>(() => shift.CheckIn(Editor, Editor, Start.AddMinutes(-31), T));
        Should.Throw<DomainException>(() => shift.CheckIn("stranger", "stranger", Start, T));
        Should.Throw<DomainException>(() => shift.CheckIn(Editor, Editor, Start.AddHours(8), T));
        shift.CheckIn(Editor, Editor, Start.AddMinutes(-30), T);
        Should.Throw<DomainException>(() => shift.CheckIn(Editor, Editor, Start.AddMinutes(1), T)); // in already

        var draft = Planned(Start);
        Should.Throw<DomainException>(() => draft.CheckIn(Editor, Editor, Start, T));
    }

    [Fact]
    public void AMemberMayCheckInAgainBeforeTheEnd()
    {
        var shift = Published(Start);
        shift.CheckIn(Editor, Editor, Start, T);
        shift.CheckOutMember(Editor, Editor, Start.AddHours(1));

        shift.CheckIn(Editor, Editor, Start.AddHours(2), T);

        var editor = shift.Members.Single(m => m.UserId == Editor);
        editor.IsIn.ShouldBeTrue();
        editor.CheckedInAt.ShouldBe(Start);
    }

    [Fact]
    public void TheStatusFollowsTheThresholds()
    {
        var shift = Published(Start);
        AttendanceStatus StatusOf(string user, DateTimeOffset now) => shift.Members.First(m => m.UserId == user).StatusOn(shift, T, now);

        StatusOf(Editor, Start.AddMinutes(30)).ShouldBe(AttendanceStatus.Expected);
        shift.CheckIn(Editor, Editor, Start.AddMinutes(10), T);
        StatusOf(Editor, Start.AddMinutes(30)).ShouldBe(AttendanceStatus.Present); // at the late threshold: on time
        shift.CheckIn(Desk, Desk, Start.AddMinutes(11), T);
        StatusOf(Desk, Start.AddMinutes(30)).ShouldBe(AttendanceStatus.Late);

        shift.MarkAbsences(Start.AddMinutes(60), T).ShouldBe([Manager]);
        StatusOf(Manager, Start.AddMinutes(61)).ShouldBe(AttendanceStatus.Absent);

        shift.CheckOutMember(Editor, Editor, Start.AddHours(7));
        StatusOf(Editor, Start.AddHours(7)).ShouldBe(AttendanceStatus.LeftEarly);
        StatusOf(Desk, Start.AddHours(8).AddMinutes(15)).ShouldBe(AttendanceStatus.Late);
        StatusOf(Desk, Start.AddHours(8).AddMinutes(16)).ShouldBe(AttendanceStatus.Overtime); // still in after end + 15
    }

    [Fact]
    public void AbsencesAreMarkedOnceAndNeverBeforeTheThreshold()
    {
        var shift = Published(Start);
        shift.CheckIn(Desk, Desk, Start, T);

        shift.MarkAbsences(Start.AddMinutes(59), T).ShouldBeEmpty();
        shift.MarkAbsences(Start.AddMinutes(60), T).ShouldBe([Manager, Editor], ignoreOrder: true);
        shift.MarkAbsences(Start.AddMinutes(90), T).ShouldBeEmpty();
        shift.Members.Single(m => m.UserId == Editor).AbsentAt.ShouldBe(Start.AddMinutes(60));

        shift.CheckIn(Editor, Editor, Start.AddMinutes(90), T); // came after all: late, not absent
        shift.Members.Single(m => m.UserId == Editor).StatusOn(shift, T, Start.AddMinutes(91)).ShouldBe(AttendanceStatus.Late);
    }

    [Fact]
    public void TheClockEndsTheShiftAtTheEndPlusTheGraceOnce()
    {
        var shift = Published(Start);
        shift.CheckIn(Desk, Desk, Start, T);
        var end = Start.AddHours(8).AddMinutes(15);

        shift.EndOnTheClock("clock", end.AddSeconds(-1), T).ShouldBeNull();
        shift.EndOnTheClock("clock", end.AddMinutes(3), T).ShouldBe([Desk]);
        shift.EndOnTheClock("clock", end.AddMinutes(4), T).ShouldBeNull();

        shift.CheckedOutAt.ShouldBe(end);
        shift.EndedAt.ShouldBe(end.AddMinutes(3)); // when the tick ran
        shift.EndedBy.ShouldBe("clock");
        var desk = shift.Members.Single(m => m.UserId == Desk);
        (desk.CheckedOutAt, desk.CheckedOutAutomatically).ShouldBe((end, true));
        desk.StatusOn(shift, T, end).ShouldBe(AttendanceStatus.Present);
        shift.Members.Where(m => m.UserId != Desk).ShouldAllBe(m => m.AbsentAt != null);
    }

    [Fact]
    public void CheckingOutTheShiftChecksEveryoneOutNow()
    {
        var shift = Published(Start);
        shift.CheckIn(Desk, Desk, Start, T);
        shift.CheckIn(Editor, Editor, Start, T);

        shift.CheckOut(Manager, Start.AddHours(6)).ShouldBe([Desk, Editor], ignoreOrder: true);

        shift.CheckedOutBy.ShouldBe(Manager);
        shift.Members.Where(m => m.CheckedInAt != null).ShouldAllBe(m => m.CheckedOutAt == Start.AddHours(6));
    }

    [Fact]
    public void NothingChecksOutBeforeTheStartOrTwice()
    {
        var shift = Published(Start);

        Should.Throw<DomainException>(() => shift.CheckOut(Manager, Start.AddMinutes(-5)));
        Should.Throw<DomainException>(() => shift.CheckOutMember(Desk, Desk, Start.AddMinutes(5))); // never checked in
        shift.CheckOut(Manager, Start.AddHours(1));
        Should.Throw<DomainException>(() => shift.CheckOut(Manager, Start.AddHours(2)));
        Should.Throw<DomainException>(() => shift.SetRota([Lead("x")]));
        Should.Throw<DomainException>(() => shift.CheckIn(Desk, Desk, Start.AddHours(2), T));
    }

    [Fact]
    public void OnlyTheLeadWritesTheHandoverOnceTheShiftStarted()
    {
        var shift = Published(Start);

        Should.Throw<DomainException>(() => shift.WriteHandover(Manager, "Note", Start.AddMinutes(-1)));
        Should.Throw<DomainException>(() => shift.WriteHandover(Desk, "Note", Start.AddHours(1)));
        Should.Throw<DomainException>(() => shift.WriteHandover(Manager, "  ", Start.AddHours(1)));
        Should.Throw<DomainException>(() => shift.WriteHandover(Manager, new string('x', Shift.HandoverNoteLength + 1), Start.AddHours(1)));

        shift.CheckOut(Manager, Start.AddHours(8));
        shift.WriteHandover(Manager, " Story 12 waits for legal. ", Start.AddHours(8));

        (shift.HandoverNote, shift.HandoverBy, shift.HandoverAt).ShouldBe(("Story 12 waits for legal.", Manager, Start.AddHours(8)));
    }
}
