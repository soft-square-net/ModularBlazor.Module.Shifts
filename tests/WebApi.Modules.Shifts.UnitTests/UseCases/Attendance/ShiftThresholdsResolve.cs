using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Attendance;

public class ShiftThresholdsResolve
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheDefaultsAreDecisionD5()
    {
        var fixture = new ShiftsFixture("a");
        var shift = ShiftBuilder.Planned(fixture.Clock.Now);

        var t = await fixture.Thresholds.ForAsync(shift, null, Ct);

        t.ShouldBe(new AttendanceThresholds(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public async Task EachValueComesFromTheNearestUnitThatSetsIt()
    {
        var fixture = new ShiftsFixture("a");
        var department = fixture.Unit(Guid.NewGuid(), "department");
        var section = fixture.Unit(Guid.NewGuid(), "section", department);
        var departmentRow = new UnitAttendance(department.Id);
        departmentRow.Set(45, 5, null, null, 30, "admin", fixture.Clock.Now);
        var sectionRow = new UnitAttendance(section.Id);
        sectionRow.Set(null, 20, null, null, null, "admin", fixture.Clock.Now);
        fixture.AttendanceRows.AddRange([departmentRow, sectionRow]);
        var shift = ShiftBuilder.Planned(fixture.Clock.Now, section.Id);

        var t = await fixture.Thresholds.ForAsync(shift, fixture.Section, Ct);

        t.ShouldBe(new AttendanceThresholds(TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public async Task AShiftWithoutAUnitTakesItsSectionsGrace()
    {
        var fixture = new ShiftsFixture("a");
        var section = Section.Create("Desk", ["politics"], null, checkoutGraceMinutes: 40);

        (await fixture.Thresholds.ForAsync(ShiftBuilder.Planned(fixture.Clock.Now), section, Ct)).CheckoutGrace.ShouldBe(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public void OverridesAreBetweenZeroAndADay()
    {
        var row = new UnitAttendance(Guid.NewGuid());
        Should.Throw<DomainException>(() => row.Set(-1, null, null, null, null, "a", TestClock.Start));
        Should.Throw<DomainException>(() => row.Set(null, null, 1441, null, null, "a", TestClock.Start));
    }
}
