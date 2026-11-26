using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using soft.square.net.Shifts.Contracts;
using soft.square.net.WebApi.Contracts.Security;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Data.Messaging;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckIn;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListEnded;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.IntegrationTests.Data;

/// <summary>
/// Attendance through the real context and the platform's outbox: every event is an outbox row saved with its change,
/// and the clock's second tick writes nothing more.
/// </summary>
public sealed class AttendanceOutbox : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private readonly ShiftsDatabase _database = new();
    private readonly Clock _clock = new(Start);
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>The module's services over one context (one request), as <paramref name="userId"/>.</summary>
    private sealed class Scope : IAsyncDisposable
    {
        public Scope(ShiftsDbContext db, TimeProvider clock, string? userId)
        {
            Db = db;
            var access = Substitute.For<IAccessScopes>();
            access.UserId.Returns(userId);
            access.HasPermissionAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns(true);
            Caller = new ShiftCaller(access, clock);
            var units = new UnitCache(new EfUnitCacheStore(db));
            var thresholds = new ShiftThresholds(new EfRepository<UnitAttendance>(db), units, Options.Create(new ShiftOptions()));
            Shifts = new EfRepository<Shift>(db);
            var sections = new EfRepository<Section>(db);
            Grants = new ShiftGrants(Substitute.For<IGrantStore>(), units, new ShiftRoles(new EfRepository<ShiftRoleDefinition>(db), units), thresholds);
            var events = new IntegrationEvents<ShiftsDbContext>(db, new IntegrationEventContext(),
                new IntegrationEventsSettings<ShiftsDbContext>(new IntegrationEventsOptions { Source = "shifts" }));
            Notices = new ShiftNotices(events, Substitute.For<IShiftLive>(), units, thresholds, Caller);
            var details = new ShiftDetails(Shifts, sections, units, Caller, thresholds);
            Changes = new ShiftChanges(Shifts, sections, Grants, details, Notices);
            Authority = new ShiftAuthority(access, units);
            Thresholds = thresholds;
            ClockRun = new ShiftClockRun(Shifts, sections, thresholds, Grants, Notices, Caller, NullLogger<ShiftClockRun>.Instance);
        }

        public ShiftsDbContext Db { get; }
        public ShiftCaller Caller { get; }
        public EfRepository<Shift> Shifts { get; }
        public ShiftGrants Grants { get; }
        public ShiftNotices Notices { get; }
        public ShiftChanges Changes { get; }
        public ShiftAuthority Authority { get; }
        public ShiftThresholds Thresholds { get; }
        public ShiftClockRun ClockRun { get; }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private Scope As(string? userId) => new(_database.For("acme"), _clock, userId);

    private async Task<int> SeedAsync()
    {
        await using var db = _database.For("acme");
        await new EfRepository<ShiftRoleDefinition>(db).AddRangeAsync(DefaultShiftRoles.Create(), Ct);
        var section = await new EfRepository<Section>(db).AddAsync(Section.Create("Politics desk", ["politics"], null, null), Ct);
        var shift = await new EfRepository<Shift>(db).AddAsync(Shift.Create(section.Id, Start, Start.AddHours(8),
            [new("manager-1", "manager", true), new("desk-1", "desk", false), new("editor-1", "editor", false)]), Ct);
        return shift.Id;
    }

    private async Task<IReadOnlyList<(string Type, JsonElement Envelope)>> OutboxAsync()
    {
        await using var db = _database.For("acme");
        return [.. (await db.Set<OutboxMessage>().AsNoTracking().ToListAsync(Ct)).OrderBy(m => m.OccurredAt).ThenBy(m => m.Id)
            .Select(m => (m.Type, JsonDocument.Parse(m.Envelope).RootElement.Clone()))];
    }

    [Fact]
    public async Task EveryAttendanceEventIsAnOutboxRowAndTheClockEndsTheShiftOnce()
    {
        var id = await SeedAsync();

        await using (var admin = As("admin-1"))
            (await new PublishShiftHandler(admin.Changes, admin.Authority, admin.Notices).Handle(new PublishShiftCommand(id), Ct)).IsSuccess.ShouldBeTrue();

        _clock.Now = Start.AddMinutes(-5);
        await using (var desk = As("desk-1"))
            (await new CheckInHandler(desk.Changes, desk.Caller, desk.Authority, desk.Thresholds, desk.Notices).Handle(new CheckInCommand(id), Ct))
                .IsSuccess.ShouldBeTrue();

        _clock.Now = Start.AddHours(8).AddMinutes(16);
        await using (var tick = As(null))
            (await tick.ClockRun.TickAsync(Ct)).ShouldBe(new ClockTick(2, 1, 1));
        await using (var again = As(null))
            (await again.ClockRun.TickAsync(Ct)).Changed.ShouldBeFalse();

        var outbox = await OutboxAsync();
        outbox.Select(o => o.Type).ShouldBe([ShiftsEvents.Published, ShiftsEvents.CheckedIn, ShiftsEvents.CheckedOut, ShiftsEvents.Ended]);
        var checkedIn = outbox[1].Envelope;
        checkedIn.GetProperty("subject").GetString().ShouldBe($"shift:{id}");
        checkedIn.GetProperty("source").GetString().ShouldBe("shifts");
        var data = checkedIn.GetProperty("data");
        data.GetProperty("userId").GetString().ShouldBe("desk-1");
        data.GetProperty("status").GetString().ShouldBe(AttendanceStatuses.Present);
        var ended = outbox[3].Envelope.GetProperty("data");
        ended.GetProperty("by").GetString().ShouldBe(ShiftsEvents.ClockActor);
        ended.GetProperty("shift").GetProperty("members").EnumerateArray()
            .Count(m => m.GetProperty("status").GetString() == AttendanceStatuses.Absent).ShouldBe(2);

        await using var reader = As("admin-1");
        var stored = await reader.Shifts.FirstOrDefaultAsync(new ShiftByIdSpec(id), Ct);
        (stored!.EndedAt, stored.EndedBy, stored.CheckedOutAt).ShouldBe((Start.AddHours(8).AddMinutes(16), ShiftsEvents.ClockActor, Start.AddHours(8).AddMinutes(15)));
        var desk1 = stored.Members.Single(m => m.UserId == "desk-1");
        (desk1.CheckedInAt, desk1.CheckedOutAt, desk1.CheckedOutAutomatically).ShouldBe((Start.AddMinutes(-5), Start.AddHours(8).AddMinutes(15), true));
        stored.Members.Where(m => m.UserId != "desk-1").ShouldAllBe(m => m.AbsentAt == Start.AddHours(1));

        // The workflow's handover list: by the time the shift ended.
        var list = await new ListEndedShiftsHandler(reader.Shifts).Handle(new ListEndedShiftsQuery(Start.AddHours(8)), Ct);
        list.Value.Select(s => (s.Id, s.EndedBy)).ShouldBe([(id, ShiftsEvents.ClockActor)]);
        (await new ListEndedShiftsHandler(reader.Shifts).Handle(new ListEndedShiftsQuery(Start.AddHours(9)), Ct)).Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task AUnitsAttendanceOverridesAreStored()
    {
        var unitId = Guid.NewGuid();
        await using (var db = _database.For("acme"))
        {
            var row = new UnitAttendance(unitId);
            row.Set(45, null, 90, null, 20, "admin-1", Start);
            await new EfRepository<UnitAttendance>(db).AddAsync(row, Ct);
        }

        await using var reader = _database.For("acme");
        var stored = await new EfRepository<UnitAttendance>(reader).GetByIdAsync(unitId, Ct);
        (stored!.EarlyCheckInMinutes, stored.LateMinutes, stored.AbsentMinutes, stored.CheckoutGraceMinutes, stored.TenantId)
            .ShouldBe((45, (int?)null, 90, 20, "acme"));
    }
}
