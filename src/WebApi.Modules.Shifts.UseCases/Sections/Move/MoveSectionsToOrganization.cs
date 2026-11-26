using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;
using OrgApi = soft.square.net.WebApi.Modules.Organization.Client;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Move;

/// <summary>
/// Move the tenant's sections to Organization. Each section that has no unit yet becomes a Section unit under
/// <paramref name="ParentUnitId"/> (a cached Department unit), created through the central API as the caller; or it is
/// linked to a unit the admin created by hand (<paramref name="Links"/>, from the JSON export). Idempotent: a moved
/// section is reported as already mapped, and its shifts are brought to its unit again if needed.
/// </summary>
public sealed record MoveSectionsToOrganizationCommand(Guid? ParentUnitId, IReadOnlyList<SectionUnitLink>? Links = null)
    : ICommand<Result<SectionMoveReport>>;

/// <summary>A section and the existing unit it becomes (the manual move).</summary>
public sealed record SectionUnitLink(int SectionId, Guid UnitId);

[JsonConverter(typeof(JsonStringEnumConverter<SectionMoveStatus>))]
public enum SectionMoveStatus
{
    /// <summary>A Section unit was created for it (or one it had left behind by an interrupted run was reused).</summary>
    Created,
    /// <summary>It had moved already; nothing was created.</summary>
    AlreadyMapped,
    /// <summary>It was linked to the unit named in the request.</summary>
    Linked,
    /// <summary>It was not moved; <see cref="SectionMoveItem.Detail"/> says why. Run the move again once fixed.</summary>
    Failed
}

/// <summary>The outcome for one section.</summary>
public sealed record SectionMoveItem(int SectionId, string Name, SectionKind Kind, SectionMoveStatus Status, Guid? UnitId, int ShiftsMoved, string? Detail);

/// <summary>The outcome of a move, one item per section.</summary>
public sealed record SectionMoveReport(Guid? ParentUnitId, IReadOnlyList<SectionMoveItem> Sections);

public sealed class MoveSectionsToOrganizationHandler(
    IReadRepository<Section> sections,
    IRepository<SectionUnitMap> maps,
    IRepository<Shift> shifts,
    UnitCache units,
    IOrganizationAccess organization,
    IIntegrationEvents events,
    ShiftGrants grants,
    ShiftCaller caller,
    ILogger<MoveSectionsToOrganizationHandler> log) : ICommandHandler<MoveSectionsToOrganizationCommand, Result<SectionMoveReport>>
{
    public async ValueTask<Result<SectionMoveReport>> Handle(MoveSectionsToOrganizationCommand command, CancellationToken ct)
    {
        var me = caller.Me;
        var all = await sections.ListAsync(new SectionsWithCategoriesSpec(), ct);
        var moved = (await maps.ListAsync(new SectionUnitMapsSpec(), ct)).ToDictionary(m => m.SectionId);
        var links = (command.Links ?? []).GroupBy(l => l.SectionId).ToDictionary(g => g.Key, g => g.Last().UnitId);
        var unknown = links.Keys.Where(id => all.All(s => s.Id != id)).ToArray();
        if (unknown.Length > 0) return Result.Invalid(new ValidationError("links", $"Unknown sections: {string.Join(", ", unknown)}."));

        // Sections that need a new unit need a parent Department and the central API.
        CachedUnit? parent = null;
        IReadOnlyList<OrgApi.OrgUnitDto> siblings = [];
        OrgApi.IOrganizationClient? central = null;
        if (all.Any(s => !moved.ContainsKey(s.Id) && !links.ContainsKey(s.Id)))
        {
            if (command.ParentUnitId is not { } parentId)
                return Result.Invalid(new ValidationError("parentUnitId", "Pick the Department unit the sections move under."));
            parent = await units.FindAsync(parentId, ct);
            if (parent is null || parent.TypeKey != CachedUnit.DepartmentType)
                return Result.Invalid(new ValidationError("parentUnitId",
                    $"Unit {parentId} is not a Department unit this module knows. Pick one from GET /units (resync them first if it is missing)."));
            if (!organization.Configured)
                return Result.Unavailable("Shifts:Organization:BaseUrl is not set: the module cannot create units. Use the JSON export and link the units instead.");
            central = organization.CentralAsCaller();
            siblings = await ExistingSectionUnitsAsync(central, parent, ct);
        }

        var report = new List<SectionMoveItem>();
        foreach (var section in all.OrderBy(s => s.Id))
        {
            try
            {
                report.Add(moved.TryGetValue(section.Id, out var map)
                    ? await KeepMovedAsync(section, map, ct)
                    : links.TryGetValue(section.Id, out var linked)
                        ? await LinkAsync(section, linked, me, ct)
                        : await CreateAsync(section, parent!, central!, siblings, me, ct));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Moving section {Section} to Organization failed", section.Id);
                report.Add(Failed(section, $"Not moved: {e.GetType().Name}. Run the move again."));
            }
        }
        return new SectionMoveReport(command.ParentUnitId, report);
    }

    /// <summary>A moved section: its shifts follow its unit again (a no-op unless something wrote shifts without it).</summary>
    private async Task<SectionMoveItem> KeepMovedAsync(Section section, SectionUnitMap map, CancellationToken ct)
    {
        var count = await MoveShiftsAsync(section, map.UnitId, save: true, ct);
        return new SectionMoveItem(section.Id, section.Name, section.Kind, SectionMoveStatus.AlreadyMapped, map.UnitId, count, null);
    }

    private async Task<SectionMoveItem> LinkAsync(Section section, Guid unitId, string me, CancellationToken ct)
    {
        var unit = await units.FindAsync(unitId, ct);
        if (unit is null)
            return Failed(section, $"Unit {unitId} is not known here: resync the units (POST /units/resync) once Organization has it.");
        if (unit.TypeKey != CachedUnit.SectionType) return Failed(section, $"Unit {unitId} is a '{unit.TypeKey}', not a Section unit.");
        var count = await CommitAsync(section, unit, me, ct);
        return new SectionMoveItem(section.Id, section.Name, section.Kind, SectionMoveStatus.Linked, unit.Id, count, null);
    }

    private async Task<SectionMoveItem> CreateAsync(Section section, CachedUnit parent, OrgApi.IOrganizationClient central,
        IReadOnlyList<OrgApi.OrgUnitDto> siblings, string me, CancellationToken ct)
    {
        // A unit left behind by an interrupted run (created, but the map was not saved) is reused, not created twice.
        var existing = siblings.FirstOrDefault(u => string.Equals(u.NameEn, section.Name, StringComparison.Ordinal));
        OrgApi.OrgUnitDto created;
        string? detail = null;
        if (existing is not null)
        {
            created = existing;
            detail = "Reused the Section unit with this name under the parent.";
        }
        else
        {
            try
            {
                created = await central.CreateUnitAsync(parent.OrgId, SectionUnitRequest(section, parent.Id), ct);
            }
            catch (OrgApi.OrganizationApiException e)
            {
                return Failed(section, CreateRefused(section, e));
            }
            catch (HttpRequestException e)
            {
                return Failed(section, $"Organization could not be reached: {e.Message}");
            }
        }

        await units.ApplyAsync(UnitStates.From(created), ct); // usable at once; the unit-created event follows
        var unit = await units.FindAsync(created.Id, ct) ?? throw new InvalidOperationException($"Unit {created.Id} is not in the cache.");
        var count = await CommitAsync(section, unit, me, ct);
        return new SectionMoveItem(section.Id, section.Name, section.Kind, SectionMoveStatus.Created, unit.Id, count, detail);
    }

    /// <summary>
    /// One save: the map row, the shifts' unit, the cached unit and the <c>shifts.section-moved.v1</c> outbox row. Then
    /// the grants of the section's published shifts that have not ended are rewritten on the unit's scopes.
    /// </summary>
    private async Task<int> CommitAsync(Section section, CachedUnit unit, string me, CancellationToken ct)
    {
        var count = await MoveShiftsAsync(section, unit.Id, save: false, ct);
        events.Publish(ShiftsEvents.SectionMoved, new SectionMoved(section.Id, unit.Id), ShiftsEvents.SectionSubject(section.Id),
            unit.Path, UnitPaths.Id(unit.OrgId));
        await maps.AddAsync(new SectionUnitMap(section.Id, unit.Id, caller.Now, me), ct); // saves the whole unit of work
        foreach (var shift in await shifts.ListAsync(new ShiftsInWindowSpec(from: caller.Now, sectionId: section.Id), ct))
            await grants.WriteAsync(shift, section, ct);
        return count;
    }

    private async Task<int> MoveShiftsAsync(Section section, Guid unitId, bool save, CancellationToken ct)
    {
        var stale = (await shifts.ListAsync(new ShiftsOfSectionSpec(section.Id), ct)).Where(s => s.UnitId != unitId).ToArray();
        foreach (var shift in stale) shift.MoveToUnit(unitId);
        if (save && stale.Length > 0)
        {
            await shifts.UpdateRangeAsync(stale, ct);
            foreach (var shift in stale.Where(s => s.EndsAt > caller.Now)) await grants.WriteAsync(shift, section, ct);
        }
        return stale.Length;
    }

    /// <summary>
    /// The Section unit of a section: its name in both languages and its categories. A department section (it serves
    /// every category) gets the tag <c>department</c> and the wildcard category <c>*</c> (decision D1).
    /// </summary>
    public static OrgApi.CreateUnitRequest SectionUnitRequest(Section section, Guid parentId) => new()
    {
        ParentId = parentId,
        TypeKey = CachedUnit.SectionType,
        NameEn = section.Name,
        NameAr = section.Name,
        Tags = section.IsDepartment ? [CachedUnit.DepartmentTag] : [],
        Categories = section.IsDepartment ? [CachedUnit.AnyCategory] : [.. section.Categories.Select(c => c.CategoryId)]
    };

    private static async Task<IReadOnlyList<OrgApi.OrgUnitDto>> ExistingSectionUnitsAsync(OrgApi.IOrganizationClient central, CachedUnit parent, CancellationToken ct)
    {
        try
        {
            return [.. (await central.ListUnitsAsync(parent.OrgId, ct)).Where(u => u.ParentId == parent.Id && u.TypeKey == CachedUnit.SectionType)];
        }
        catch (Exception e) when (e is OrgApi.OrganizationApiException or HttpRequestException)
        {
            return []; // only used to reuse units of an interrupted run; creating reports the real problem
        }
    }

    private static string CreateRefused(Section section, OrgApi.OrganizationApiException e)
    {
        var detail = $"Organization refused the Section unit ({e.StatusCode}): {Trim(e.Response)}";
        return section.IsDepartment && e.StatusCode is 400 or 409 or 422
            ? detail + " This is a department section, sent with the wildcard category '*' (decision D1): this Organization version may not accept it yet."
            : detail;
    }

    private static string Trim(string? text) => string.IsNullOrWhiteSpace(text) ? "(no detail)" : text.Length > 500 ? text[..500] : text;

    private static SectionMoveItem Failed(Section section, string detail) =>
        new(section.Id, section.Name, section.Kind, SectionMoveStatus.Failed, null, 0, detail);
}
