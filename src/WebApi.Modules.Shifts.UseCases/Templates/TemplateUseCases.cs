using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

/// <summary>How many people a template's shift needs in one role.</summary>
public sealed record TemplateSlotDto(string RoleKey, int Count);

/// <summary>A person a clone puts on the rota, in a role.</summary>
public sealed record TemplateMemberDto(string UserId, string RoleKey);

/// <summary>
/// A shift template: <paramref name="StartLocal"/> is wall-clock time in the unit's time zone, <paramref name="Duration"/>
/// elapsed time (at most 24 hours).
/// </summary>
public sealed record ShiftTemplateDto(int Id, Guid UnitId, string Name, TimeOnly StartLocal, TimeSpan Duration,
    IReadOnlyList<TemplateSlotDto> Slots, IReadOnlyList<TemplateMemberDto> DefaultMembers)
{
    public static ShiftTemplateDto From(ShiftTemplate t) => new(t.Id, t.UnitId, t.Name, t.StartLocal, t.Duration,
        [.. t.Slots.Select(s => new TemplateSlotDto(s.RoleKey, s.Count))],
        [.. t.DefaultMembers.Select(m => new TemplateMemberDto(m.UserId, m.RoleKey))]);

    public static Result<T> NotFound<T>(int id) => Result<T>.NotFound($"Template {id} was not found.");
}

/// <summary>The templates, optionally of one unit.</summary>
public sealed record ListTemplatesQuery(Guid? UnitId) : IQuery<Result<IReadOnlyList<ShiftTemplateDto>>>;

public sealed class ListTemplatesHandler(IReadRepository<ShiftTemplate> templates) : IQueryHandler<ListTemplatesQuery, Result<IReadOnlyList<ShiftTemplateDto>>>
{
    public async ValueTask<Result<IReadOnlyList<ShiftTemplateDto>>> Handle(ListTemplatesQuery query, CancellationToken ct) =>
        (await templates.ListAsync(new TemplatesSpec(query.UnitId), ct)).Select(ShiftTemplateDto.From).ToArray();
}

public sealed record GetTemplateQuery(int Id) : IQuery<Result<ShiftTemplateDto>>;

public sealed class GetTemplateHandler(IReadRepository<ShiftTemplate> templates) : IQueryHandler<GetTemplateQuery, Result<ShiftTemplateDto>>
{
    public async ValueTask<Result<ShiftTemplateDto>> Handle(GetTemplateQuery query, CancellationToken ct) =>
        await templates.FirstOrDefaultAsync(new TemplateByIdSpec(query.Id), ct) is { } template
            ? ShiftTemplateDto.From(template)
            : ShiftTemplateDto.NotFound<ShiftTemplateDto>(query.Id);
}

/// <summary>Add a template to a unit (needs <c>shifts.manage</c> on the unit).</summary>
public sealed record AddTemplateCommand(Guid UnitId, string Name, TimeOnly StartLocal, TimeSpan Duration, IReadOnlyList<TemplateSlotDto>? Slots,
    IReadOnlyList<TemplateMemberDto>? DefaultMembers) : ICommand<Result<ShiftTemplateDto>>;

public sealed class AddTemplateHandler(IRepository<ShiftTemplate> templates, TemplateRules rules) : ICommandHandler<AddTemplateCommand, Result<ShiftTemplateDto>>
{
    public async ValueTask<Result<ShiftTemplateDto>> Handle(AddTemplateCommand command, CancellationToken ct)
    {
        var checkedTemplate = await rules.CheckAsync(command.UnitId, command.Slots ?? [], command.DefaultMembers ?? [], ct);
        if (!checkedTemplate.IsSuccess) return checkedTemplate.As<TemplateParts, ShiftTemplateDto>();
        var parts = checkedTemplate.Value;
        var template = ShiftTemplate.Create(command.UnitId, command.Name, command.StartLocal, command.Duration, parts.Slots, parts.Members);
        await templates.AddAsync(template, ct);
        return ShiftTemplateDto.From(template);
    }
}

/// <summary>Change a template (it stays on its unit).</summary>
public sealed record UpdateTemplateCommand(int Id, string Name, TimeOnly StartLocal, TimeSpan Duration, IReadOnlyList<TemplateSlotDto>? Slots,
    IReadOnlyList<TemplateMemberDto>? DefaultMembers) : ICommand<Result<ShiftTemplateDto>>;

public sealed class UpdateTemplateHandler(IRepository<ShiftTemplate> templates, TemplateRules rules) : ICommandHandler<UpdateTemplateCommand, Result<ShiftTemplateDto>>
{
    public async ValueTask<Result<ShiftTemplateDto>> Handle(UpdateTemplateCommand command, CancellationToken ct)
    {
        var template = await templates.FirstOrDefaultAsync(new TemplateByIdSpec(command.Id), ct);
        if (template is null) return ShiftTemplateDto.NotFound<ShiftTemplateDto>(command.Id);
        var checkedTemplate = await rules.CheckAsync(template.UnitId, command.Slots ?? [], command.DefaultMembers ?? [], ct);
        if (!checkedTemplate.IsSuccess) return checkedTemplate.As<TemplateParts, ShiftTemplateDto>();
        var parts = checkedTemplate.Value;
        template.Update(command.Name, command.StartLocal, command.Duration, parts.Slots, parts.Members);
        await templates.UpdateAsync(template, ct);
        return ShiftTemplateDto.From(template);
    }
}

public sealed record DeleteTemplateCommand(int Id) : ICommand<Result>;

public sealed class DeleteTemplateHandler(IRepository<ShiftTemplate> templates, ShiftAuthority authority) : ICommandHandler<DeleteTemplateCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTemplateCommand command, CancellationToken ct)
    {
        var template = await templates.FirstOrDefaultAsync(new TemplateByIdSpec(command.Id), ct);
        if (template is null) return Result.NotFound($"Template {command.Id} was not found.");
        var allowed = await authority.RequireManageAsync(template.UnitId, ct);
        if (!allowed.IsSuccess) return allowed;
        await templates.DeleteAsync(template, ct);
        return Result.Success();
    }
}

/// <summary>A template's slots and default members, checked for its unit.</summary>
public sealed record TemplateParts(IReadOnlyList<(string RoleKey, int Count)> Slots, IReadOnlyList<(string UserId, string RoleKey)> Members);

/// <summary>
/// The rules a template's contents follow, on save: the caller manages the unit; every role key holds for the unit;
/// at most one default member is in a lead role; every default member belongs to the unit or a unit above it (D6).
/// </summary>
public sealed class TemplateRules(UnitCache units, ShiftAuthority authority, ShiftRota rota, ShiftCaller caller)
{
    public async Task<Result<TemplateParts>> CheckAsync(Guid unitId, IReadOnlyList<TemplateSlotDto> slots, IReadOnlyList<TemplateMemberDto> members,
        CancellationToken ct)
    {
        if (await units.FindAsync(unitId, ct) is null)
            return Result.Invalid(new ValidationError("unitId", $"Unit {unitId} is not known here. Pick a unit from GET /units."));
        var allowed = await authority.RequireManageAsync(unitId, ct);
        if (!allowed.IsSuccess) return allowed.As<TemplateParts>();

        var resolvedMembers = await rota.ResolveAsync(unitId, members.Select(m => new RotaEntry(m.UserId, m.RoleKey)), caller.Now, ct);
        if (!resolvedMembers.IsSuccess) return resolvedMembers.As<IReadOnlyList<RotaAssignment>, TemplateParts>();
        if (resolvedMembers.Value.Count(m => m.IsLead) > 1)
            return Result.Invalid(new ValidationError("defaultMembers", "A template has at most one default member in a lead role."));
        var slotKeys = await rota.ResolveKeysAsync(unitId, slots.Select(s => s.RoleKey), ct);
        if (!slotKeys.IsSuccess) return slotKeys.As<IReadOnlyList<string>, TemplateParts>();

        return new TemplateParts(
            [.. slots.Zip(slotKeys.Value, (slot, key) => (key, slot.Count))],
            [.. resolvedMembers.Value.Select(m => (m.UserId, m.RoleKey))]);
    }
}

/// <summary>
/// Clone a template into a Draft shift on <paramref name="Date"/> (a calendar day in the unit's time zone). The rota is
/// the template's default members; unfilled slots, a missing lead and default members who may no longer be on the
/// rota come back as warnings, not errors.
/// </summary>
public sealed record CloneTemplateCommand(int Id, DateOnly Date) : ICommand<Result<TemplateCloneResult>>;

/// <summary>The Draft shift a clone made, and what it still lacks.</summary>
public sealed record TemplateCloneResult(ShiftDto Shift, IReadOnlyList<string> Warnings);

public sealed class CloneTemplateHandler(
    IReadRepository<ShiftTemplate> templates,
    IReadRepository<SectionUnitMap> maps,
    IRepository<Shift> shifts,
    UnitCache units,
    ShiftAuthority authority,
    ShiftRoles roles,
    UnitMembers members,
    ShiftDetails details) : ICommandHandler<CloneTemplateCommand, Result<TemplateCloneResult>>
{
    public async ValueTask<Result<TemplateCloneResult>> Handle(CloneTemplateCommand command, CancellationToken ct)
    {
        var template = await templates.FirstOrDefaultAsync(new TemplateByIdSpec(command.Id), ct);
        if (template is null) return ShiftTemplateDto.NotFound<TemplateCloneResult>(command.Id);
        var unit = await units.FindAsync(template.UnitId, ct);
        if (unit is null)
            return Result.Conflict($"The template's unit {template.UnitId} is not known here any more (removed from Organization?). Resync the units.");
        var allowed = await authority.RequireManageAsync(unit.Id, ct);
        if (!allowed.IsSuccess) return allowed.As<TemplateCloneResult>();

        var warnings = new List<string>();
        var zone = await TimeZoneOfAsync(unit, warnings, ct);
        var (startsAt, endsAt) = template.WindowOn(command.Date, zone);

        var set = await roles.ForUnitAsync(unit.Id, ct);
        var eligible = template.DefaultMembers.Count == 0 ? null : await members.EligibleAsync(unit, startsAt, ct);
        if (eligible is { IsSuccess: false }) return eligible.As<IReadOnlySet<string>, TemplateCloneResult>();

        var rota = new List<RotaAssignment>();
        foreach (var member in template.DefaultMembers)
        {
            if (set.Find(member.RoleKey) is not { } role)
                warnings.Add($"{member.UserId} was left out: no role '{member.RoleKey}' holds for this unit any more.");
            else if (eligible?.Value.Contains(member.UserId) != true)
                warnings.Add($"{member.UserId} was left out: not a member of the unit or of a unit above it on {command.Date:yyyy-MM-dd}.");
            else if (role.IsLead && rota.Any(r => r.IsLead))
                warnings.Add($"{member.UserId} was left out: the shift has a lead already.");
            else
                rota.Add(new RotaAssignment(member.UserId, role.Key, role.IsLead));
        }
        foreach (var slot in template.Slots)
        {
            var filled = rota.Count(r => r.RoleKey == slot.RoleKey);
            if (filled < slot.Count) warnings.Add($"Slot '{slot.RoleKey}': {filled} of {slot.Count} filled.");
        }
        if (!rota.Any(r => r.IsLead)) warnings.Add("The shift has no lead yet: put someone in a lead role on the rota before publishing.");

        var map = (await maps.ListAsync(new SectionUnitMapByUnitSpec(unit.Id), ct)).FirstOrDefault();
        var shift = Shift.Create(map?.SectionId, startsAt, endsAt, rota, unit.Id);
        await shifts.AddAsync(shift, ct);
        return new TemplateCloneResult((await details.ToDtosAsync([shift], ct))[0], warnings);
    }

    /// <summary>The unit's time zone, else the nearest one up its path; UTC (with a warning) when none is set or known.</summary>
    private async Task<TimeZoneInfo> TimeZoneOfAsync(CachedUnit unit, List<string> warnings, CancellationToken ct)
    {
        string? id = unit.TimeZone;
        foreach (var ancestor in UnitPaths.Ids(unit.Path).Reverse().Skip(1))
        {
            if (!string.IsNullOrWhiteSpace(id)) break;
            if (Guid.TryParseExact(ancestor, "N", out var ancestorId)) id = (await units.FindAsync(ancestorId, ct))?.TimeZone;
        }
        if (string.IsNullOrWhiteSpace(id))
        {
            warnings.Add("The unit has no time zone (nor any unit above it): the template's times were taken as UTC.");
            return TimeZoneInfo.Utc;
        }
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        warnings.Add($"The time zone '{id}' is not known on this server: the template's times were taken as UTC.");
        return TimeZoneInfo.Utc;
    }
}
