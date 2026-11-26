using Ardalis.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases;

/// <summary>
/// The shared shift services (ShiftCaller, ShiftAuthority, ShiftRoles, ShiftRota, ShiftGrants, ShiftDetails,
/// ShiftChanges) over substituted platform contracts. The tenant has the default roles (manager, desk, editor); the
/// caller manages nothing until a test says so; Organization's directory knows no members until a test adds them.
/// </summary>
public sealed class ShiftsFixture
{
    public IAccessScopes Access { get; } = Substitute.For<IAccessScopes>();
    public IGrantStore Grants { get; } = Substitute.For<IGrantStore>();
    public IRepository<Shift> Shifts { get; } = Substitute.For<IRepository<Shift>>();
    public IReadRepository<Shift> ShiftReader { get; } = Substitute.For<IReadRepository<Shift>>();
    public IReadRepository<Section> SectionReader { get; } = Substitute.For<IReadRepository<Section>>();
    /// <summary>The section-to-unit rows: none unless a test adds them.</summary>
    public IReadRepository<SectionUnitMap> Moved { get; } = Substitute.For<IReadRepository<SectionUnitMap>>();
    public IReadRepository<ShiftRoleDefinition> RoleReader { get; } = Substitute.For<IReadRepository<ShiftRoleDefinition>>();
    /// <summary>The units' attendance overrides: none unless a test adds them to <see cref="AttendanceRows"/>.</summary>
    public IReadRepository<UnitAttendance> AttendanceStore { get; } = Substitute.For<IReadRepository<UnitAttendance>>();
    public List<UnitAttendance> AttendanceRows { get; } = [];
    /// <summary>The module's settings (D5 defaults).</summary>
    public ShiftOptions Options { get; } = new();
    /// <summary>The outbox: every event a handler published.</summary>
    public IIntegrationEvents Events { get; } = Substitute.For<IIntegrationEvents>();
    public IShiftLive Live { get; } = Substitute.For<IShiftLive>();
    /// <summary>The tenant's role definitions (what <see cref="RoleReader"/> lists): the defaults unless a test changes them.</summary>
    public List<ShiftRoleDefinition> RoleRows { get; } = [.. DefaultShiftRoles.Create()];
    public IOrganizationAccess Organization { get; } = Substitute.For<IOrganizationAccess>();
    public IUnitDirectory Directory { get; } = Substitute.For<IUnitDirectory>();
    public FakeUnitCacheStore UnitStore { get; } = new();
    public TestClock Clock { get; } = new();

    public ShiftCaller Caller { get; }
    public ShiftAuthority Authority { get; }
    public ShiftRoles Roles { get; }
    public UnitMembers UnitMembers { get; }
    public ShiftRota Rota { get; }
    public ShiftGrants ShiftGrants { get; }
    public ShiftDetails Details { get; }
    public ShiftChanges Changes { get; }
    public UnitCache Units { get; }
    public ShiftUnits ShiftUnits { get; }
    public ShiftThresholds Thresholds { get; }
    public ShiftNotices Notices { get; }

    /// <summary>The section every stored shift belongs to: two categories.</summary>
    public Section Section { get; } = Section.Create("Politics desk", ["politics", "economy"], null, null);

    private readonly Dictionary<Guid, List<DirectoryMemberDto>> _members = [];

    public ShiftsFixture(string userId)
    {
        Access.UserId.Returns(userId);
        Caller = new ShiftCaller(Access, Clock);
        Units = new UnitCache(UnitStore);
        Authority = new ShiftAuthority(Access, Units);
        ShiftUnits = new ShiftUnits(Moved, Units);
        Roles = new ShiftRoles(RoleReader, Units);
        UnitMembers = new UnitMembers(Organization, NullLogger<UnitMembers>.Instance);
        Rota = new ShiftRota(Roles, Units, UnitMembers);
        Thresholds = new ShiftThresholds(AttendanceStore, Units, Microsoft.Extensions.Options.Options.Create(Options));
        ShiftGrants = new ShiftGrants(Grants, Units, Roles, Thresholds);
        Details = new ShiftDetails(ShiftReader, SectionReader, Units, Caller, Thresholds);
        Notices = new ShiftNotices(Events, Live, Units, Thresholds, Caller);
        Changes = new ShiftChanges(Shifts, SectionReader, ShiftGrants, Details, Notices);
        AttendanceStore.ListAsync(Arg.Any<ISpecification<UnitAttendance>>(), Arg.Any<CancellationToken>())
            .Returns(call => AttendanceRows.Where(r => call.Arg<ISpecification<UnitAttendance>>().IsSatisfiedBy(r)).ToList());

        SectionReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(Section);
        SectionReader.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns([Section]);
        RoleReader.ListAsync(Arg.Any<ISpecification<ShiftRoleDefinition>>(), Arg.Any<CancellationToken>()).Returns(_ => RoleRows.ToList());

        Organization.Configured.Returns(true);
        Organization.DirectoryAsCaller().Returns(Directory);
        Directory.MembersOfAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<DirectoryMemberDto>)[.. _members.GetValueOrDefault(call.Arg<Guid>()) ?? []]);
    }

    /// <summary>Every single-shift read (for a change, and for the details after it) returns this shift.</summary>
    public void Stored(Shift shift)
    {
        Shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);
        ShiftReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);
    }

    /// <summary>The caller holds <c>shifts.manage</c> tenant-wide: every scope check passes.</summary>
    public void ShiftAdmin()
    {
        Access.HasPermissionAsync(ShiftPermissions.Manage, Arg.Any<CancellationToken>()).Returns(true);
        Access.HasPermissionAsync(ShiftPermissions.Manage, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    /// <summary>The caller holds <c>shifts.manage</c> on one unit only: checks pass for resources that carry its scope.</summary>
    public void Manages(Guid unitId)
    {
        var scope = UnitScopes.Of(unitId);
        Access.HasPermissionAsync(ShiftPermissions.Manage, Arg.Any<CancellationToken>()).Returns(true);
        Access.HasPermissionAsync(ShiftPermissions.Manage, Arg.Is<IReadOnlyCollection<string>>(s => s.Contains(scope)), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    /// <summary>A cached unit: <paramref name="parent"/>'s child, or a root.</summary>
    public CachedUnit Unit(Guid id, string type, CachedUnit? parent = null, string? timeZone = null, IReadOnlyList<string>? categories = null,
        IReadOnlyList<string>? tags = null)
    {
        var path = parent is null ? UnitPaths.Root(id) : UnitPaths.Child(parent.Path, id);
        var unit = CachedUnit.From(new UnitState(id, Guid.Empty, parent?.Id, type, path, $"{type} {id.ToString()[..4]}", "", tags ?? [], timeZone,
            categories ?? [], 1));
        UnitStore.Add(unit);
        return unit;
    }

    /// <summary>A membership of <paramref name="unitId"/> in Organization's directory (active since long ago, unless given).</summary>
    public void Member(Guid unitId, string subject, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        if (!_members.TryGetValue(unitId, out var list)) _members[unitId] = list = [];
        list.Add(new DirectoryMemberDto(Guid.NewGuid(), Guid.NewGuid(), "https://id.test", subject, subject, unitId, null, null, [],
            from ?? TestClock.Start.AddYears(-1), to, true));
    }

    /// <summary>The events published so far, in order: (type, data, subject, unit path).</summary>
    public IReadOnlyList<(string Type, object Data, string Subject, string? UnitPath)> Published =>
        [.. Events.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IIntegrationEvents.Publish))
            .Select(c => c.GetArguments()).Select(a => ((string)a[0]!, a[1]!, (string)a[2]!, (string?)a[3]))];

    /// <summary>The data of the events of one type.</summary>
    public IReadOnlyList<T> PublishedOf<T>(string type) => [.. Published.Where(e => e.Type == type).Select(e => (T)e.Data)];

    /// <summary>The grants last written for the shift's source.</summary>
    public IReadOnlyCollection<NewGrant> WrittenGrants(Shift shift) =>
        (IReadOnlyCollection<NewGrant>)Grants.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGrantStore.ReplaceSourceAsync) && (string)c.GetArguments()[0]! == shift.GrantSource)
            .Last().GetArguments()[1]!;
}
