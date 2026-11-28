using System.Text.Json;

namespace soft.square.net.Module.Shifts.Tests;

public class ShiftFormTests
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    private static readonly DateTimeOffset Now = new(2026, 12, 1, 10, 20, 0, TimeSpan.Zero);
    private static readonly Guid Unit = Guid.NewGuid();

    [Fact]
    public void A_new_shift_starts_at_the_units_next_full_hour_with_me_as_manager()
    {
        var form = ShiftForm.New(Berlin, "me", Unit, now: Now);

        (form.StartsAt, form.EndsAt).ShouldBe(("2026-12-01T12:00", "2026-12-01T20:00"));
        form.Rota.Select(r => (r.UserId, r.RoleKey)).ShouldBe([("me", "manager")]);
    }

    [Fact]
    public void A_shift_added_on_a_day_starts_at_nine_there()
    {
        var request = ShiftForm.New(Berlin, null, Unit, day: new DateOnly(2026, 12, 3), now: Now).ToRequest();

        request.StartsAt.ShouldBe(new DateTimeOffset(2026, 12, 3, 8, 0, 0, TimeSpan.Zero));
        (request.UnitId, request.SectionId).ShouldBe((Unit, null));
    }

    [Fact]
    public void A_shift_without_a_unit_needs_a_section()
    {
        var form = ShiftForm.New(Berlin, "me", null, now: Now);

        Should.Throw<ShiftFormException>(() => form.ToRequest()).Message.ShouldBe("Choose a section.");
        form.SectionId = "3";
        form.ToRequest().SectionId.ShouldBe(3);
    }

    [Fact]
    public void The_only_section_is_chosen_already()
    {
        ShiftForm.New(Berlin, "me", null, [new SectionDto { Id = 5, Name = "Desk" }], now: Now).SectionId.ShouldBe("5");
    }

    [Fact]
    public void An_unchanged_time_is_sent_back_as_the_same_instant()
    {
        // 02:30 happens twice that night: this shift starts at the second one.
        var second = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);
        var shift = new ShiftDto { UnitId = Unit, SectionId = 2, StartsAt = second, EndsAt = second.AddHours(8), Rota = [] };

        var form = ShiftForm.From(shift, Berlin);

        form.ToRequest().StartsAt.ShouldBe(second);
        form.StartsAt = "2026-10-25T04:00";
        form.ToRequest().StartsAt.ShouldBe(new DateTimeOffset(2026, 10, 25, 3, 0, 0, TimeSpan.Zero));
        form.ToRequest().SectionId.ShouldBe(2);
    }

    [Fact]
    public void Blank_rota_rows_are_left_out_and_a_bad_time_is_refused()
    {
        var form = ShiftForm.New(Berlin, "me", Unit, now: Now);
        form.Rota.Add(new ShiftForm.RotaRow { UserId = "  " });
        form.ToRequest().Rota!.Count.ShouldBe(1);

        form.EndsAt = "soon";
        Should.Throw<ShiftFormException>(() => form.ToRequest()).Message.ShouldBe("Enter the shift's end as a date and time.");
    }
}

public class TemplateFormTests
{
    private static readonly Guid Unit = Guid.NewGuid();

    [Fact]
    public void A_template_round_trips()
    {
        var template = new ShiftTemplateDto
        {
            Id = 3, UnitId = Unit, Name = "Morning", StartLocal = new TimeSpan(6, 30, 0), Duration = "08:15:00",
            Slots = [new TemplateSlotDto { RoleKey = "desk", Count = 2 }],
            DefaultMembers = [new TemplateMemberDto { UserId = "u1", RoleKey = "manager" }],
        };

        var request = TemplateForm.From(template).ToRequest();

        (request.Name, request.StartLocal, request.Duration, request.UnitId).ShouldBe(("Morning", new TimeSpan(6, 30, 0), "08:15:00", Unit));
        request.Slots!.Single().Count.ShouldBe(2);
        request.DefaultMembers!.Single().UserId.ShouldBe("u1");
    }

    [Fact]
    public void A_whole_day_is_written_with_its_day()
    {
        var form = TemplateForm.New(Unit);
        form.Name = "On call";
        form.Hours = 24;

        form.ToRequest().Duration.ShouldBe("1.00:00:00");
        TemplateForm.ParseDuration("1.00:00:00").ShouldBe(TimeSpan.FromHours(24));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(24, 1)]
    [InlineData(1, 60)]
    public void A_template_lasts_between_nothing_and_a_day(int hours, int minutes)
    {
        var form = TemplateForm.New(Unit);
        (form.Name, form.Hours, form.Minutes) = ("x", hours, minutes);

        Should.Throw<ShiftFormException>(() => form.ToRequest());
    }

    [Fact]
    public void A_clone_sends_the_date_alone()
    {
        var json = JsonSerializer.Serialize(new TemplateCloneRequest { Date = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero) });

        json.ShouldContain("\"2026-12-01\"");
    }
}

public class SectionMoveTests
{
    private static readonly Guid Department = Guid.NewGuid();
    private static readonly Guid Linked = Guid.NewGuid();

    [Fact]
    public void Sections_without_a_chosen_unit_need_a_department()
    {
        SectionMove.Request([1, 2], new Dictionary<int, Guid> { [1] = Linked }, null).ShouldBeNull();

        var request = SectionMove.Request([1, 2], new Dictionary<int, Guid> { [1] = Linked }, Department)!;
        request.ParentUnitId.ShouldBe(Department);
        request.Links!.Select(l => (l.SectionId, l.UnitId)).ShouldBe([(1, Linked)]);
    }

    [Fact]
    public void With_a_unit_for_every_section_no_department_is_sent()
    {
        var request = SectionMove.Request([1], new Dictionary<int, Guid> { [1] = Linked, [9] = Department }, Department)!;

        request.ParentUnitId.ShouldBeNull();
        request.Links!.Single().SectionId.ShouldBe(1);
    }

    [Fact]
    public void The_export_keeps_arabic_readable()
    {
        var json = SectionMove.ToJson(new SectionsExport
        {
            Sections = [new SectionExportItem { SectionId = 1, Name = "السياسة", Kind = SectionKind.Department, Categories = [] }],
        });

        json.ShouldContain("\"name\": \"السياسة\"");
        json.ShouldContain("\"Department\"");
    }
}
