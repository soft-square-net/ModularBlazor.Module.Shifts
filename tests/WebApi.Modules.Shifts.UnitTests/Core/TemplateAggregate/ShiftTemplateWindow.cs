using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.TemplateAggregate;

/// <summary>
/// A template's window on a day, in the unit's time zone, across daylight-saving changes. Berlin in 2026: the clocks go
/// forward on 29 March (02:00 CET becomes 03:00 CEST) and back on 25 October (03:00 CEST becomes 02:00 CET).
/// </summary>
public class ShiftTemplateWindow
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static ShiftTemplate Template(int hour, int minute = 0, int hours = 8) =>
        ShiftTemplate.Create(Guid.NewGuid(), "Shift", new TimeOnly(hour, minute), TimeSpan.FromHours(hours), [("manager", 1)], []);

    private static DateTimeOffset Utc(int month, int day, int hour, int minute = 0) => new(2026, month, day, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void TheSameLocalStartIsAnHourEarlierInUtcAfterTheClocksGoForward()
    {
        var morning = Template(6);

        morning.WindowOn(new DateOnly(2026, 3, 28), Berlin).ShouldBe((Utc(3, 28, 5), Utc(3, 28, 13))); // CET, +1
        morning.WindowOn(new DateOnly(2026, 3, 29), Berlin).ShouldBe((Utc(3, 29, 4), Utc(3, 29, 12))); // CEST, +2
    }

    [Fact]
    public void AStartTheClocksSkipMovesForwardByTheGap()
    {
        var (startsAt, endsAt) = Template(2, 30).WindowOn(new DateOnly(2026, 3, 29), Berlin);

        startsAt.ShouldBe(Utc(3, 29, 1, 30)); // 03:30 CEST, as the clocks show it
        TimeZoneInfo.ConvertTime(startsAt, Berlin).TimeOfDay.ShouldBe(new TimeSpan(3, 30, 0));
        (endsAt - startsAt).ShouldBe(TimeSpan.FromHours(8));
    }

    [Fact]
    public void AStartTheClocksPassTwiceIsTheFirst()
    {
        var (startsAt, _) = Template(2, 30).WindowOn(new DateOnly(2026, 10, 25), Berlin);

        startsAt.ShouldBe(Utc(10, 25, 0, 30)); // 02:30 CEST, before the clocks go back
    }

    [Fact]
    public void ANightAcrossTheChangeLastsItsDurationAndEndsAnHourEarlierOnTheClock()
    {
        var (startsAt, endsAt) = Template(22).WindowOn(new DateOnly(2026, 10, 24), Berlin);

        startsAt.ShouldBe(Utc(10, 24, 20)); // 22:00 CEST
        endsAt.ShouldBe(Utc(10, 25, 4)); // eight hours later: 05:00 CET
        TimeZoneInfo.ConvertTime(endsAt, Berlin).TimeOfDay.ShouldBe(new TimeSpan(5, 0, 0));
    }

    [Fact]
    public void UtcHasNoSurprises() =>
        Template(6).WindowOn(new DateOnly(2026, 3, 29), TimeZoneInfo.Utc).ShouldBe((Utc(3, 29, 6), Utc(3, 29, 14)));

    [Fact]
    public void LastsSomeTimeAndAtMostADayWithOneSlotPerRole()
    {
        var unit = Guid.NewGuid();
        Should.Throw<DomainException>(() => ShiftTemplate.Create(unit, "x", new TimeOnly(6, 0), TimeSpan.Zero, [], []));
        Should.Throw<DomainException>(() => ShiftTemplate.Create(unit, "x", new TimeOnly(6, 0), TimeSpan.FromHours(25), [], []));
        Should.Throw<DomainException>(() => ShiftTemplate.Create(unit, "x", new TimeOnly(6, 0), TimeSpan.FromHours(8), [("desk", 1), ("desk", 2)], []));
        Should.Throw<DomainException>(() => ShiftTemplate.Create(unit, "x", new TimeOnly(6, 0), TimeSpan.FromHours(8), [("desk", 0)], []));
        Should.Throw<DomainException>(() => ShiftTemplate.Create(unit, " ", new TimeOnly(6, 0), TimeSpan.FromHours(8), [], []));
        Should.Throw<DomainException>(() => ShiftTemplate.Create(Guid.Empty, "x", new TimeOnly(6, 0), TimeSpan.FromHours(8), [], []));

        var template = ShiftTemplate.Create(unit, " Morning ", new TimeOnly(6, 0), TimeSpan.FromHours(24), [("desk", 2)], [("u1", "desk"), ("u1", "desk")]);
        template.Name.ShouldBe("Morning");
        template.DefaultMembers.Count.ShouldBe(1);
    }
}
