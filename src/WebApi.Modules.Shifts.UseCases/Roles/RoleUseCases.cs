using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>A role definition as callers see it. <paramref name="UnitId"/> null: it holds for the whole tenant.</summary>
public sealed record RoleDefinitionDto(int Id, string Key, string NameEn, string NameAr, IReadOnlyList<string> Permissions, bool IsLead, Guid? UnitId)
{
    public static RoleDefinitionDto From(ShiftRoleDefinition d) => new(d.Id, d.Key, d.NameEn, d.NameAr, d.Permissions, d.IsLead, d.UnitId);

    public static Result<T> NotFound<T>(int id) => Result<T>.NotFound($"Role definition {id} was not found.");
}

/// <summary>Every role definition of the tenant: the tenant-wide ones and every unit's overrides.</summary>
public sealed record ListRoleDefinitionsQuery : IQuery<Result<IReadOnlyList<RoleDefinitionDto>>>;

public sealed class ListRoleDefinitionsHandler(IReadRepository<ShiftRoleDefinition> roles)
    : IQueryHandler<ListRoleDefinitionsQuery, Result<IReadOnlyList<RoleDefinitionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<RoleDefinitionDto>>> Handle(ListRoleDefinitionsQuery query, CancellationToken ct) =>
        (await roles.ListAsync(new AllRoleDefinitionsSpec(), ct)).Select(RoleDefinitionDto.From).ToArray();
}

/// <summary>The roles that hold for a unit (the nearest definition of each key up its path), or the tenant's without a unit.</summary>
public sealed record ListEffectiveRolesQuery(Guid? UnitId) : IQuery<Result<IReadOnlyList<RoleDefinitionDto>>>;

public sealed class ListEffectiveRolesHandler(ShiftRoles roles, UnitCache units) : IQueryHandler<ListEffectiveRolesQuery, Result<IReadOnlyList<RoleDefinitionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<RoleDefinitionDto>>> Handle(ListEffectiveRolesQuery query, CancellationToken ct)
    {
        if (query.UnitId is { } id && await units.FindAsync(id, ct) is null) return Result.NotFound($"Unit {id} is not known here.");
        var set = await roles.ForUnitAsync(query.UnitId, ct);
        return set.ByKey.Values.OrderByDescending(d => d.IsLead).ThenBy(d => d.Key, StringComparer.Ordinal).Select(RoleDefinitionDto.From).ToArray();
    }
}

/// <summary>
/// Add a role definition: for the whole tenant (<paramref name="UnitId"/> null; needs <c>shifts.manage</c> tenant-wide) or
/// for a unit and the units below it (needs it on the unit). Its permissions must be in the platform's permission catalog.
/// </summary>
public sealed record AddRoleDefinitionCommand(string Key, string NameEn, string? NameAr, IReadOnlyList<string> Permissions, bool IsLead, Guid? UnitId)
    : ICommand<Result<RoleDefinitionDto>>;

public sealed class AddRoleDefinitionHandler(IRepository<ShiftRoleDefinition> roles, UnitCache units, ShiftAuthority authority, RolePermissions catalog)
    : ICommandHandler<AddRoleDefinitionCommand, Result<RoleDefinitionDto>>
{
    public async ValueTask<Result<RoleDefinitionDto>> Handle(AddRoleDefinitionCommand command, CancellationToken ct)
    {
        if (command.UnitId is { } unitId && await units.FindAsync(unitId, ct) is null)
            return Result.Invalid(new ValidationError("unitId", $"Unit {unitId} is not known here. Pick a unit from GET /units."));
        var allowed = await authority.RequireManageAsync(command.UnitId, ct);
        if (!allowed.IsSuccess) return allowed.As<RoleDefinitionDto>();
        if (!ShiftRoleDefinition.IsValidKey((command.Key ?? "").Trim().ToLowerInvariant()))
            return Result.Invalid(new ValidationError("key", "A role key is lowercase letters, digits and dashes, starting with a letter (at most 64)."));
        var known = await catalog.CheckAsync(command.Permissions, ct);
        if (!known.IsSuccess) return known.As<RoleDefinitionDto>();

        var definition = ShiftRoleDefinition.Create(command.Key!, command.NameEn, command.NameAr ?? "", command.Permissions, command.IsLead, command.UnitId);
        if ((await roles.ListAsync(new AllRoleDefinitionsSpec(), ct)).Any(d => d.Key == definition.Key && d.UnitId == definition.UnitId))
            return Result.Conflict(command.UnitId is null
                ? $"The tenant already has a role '{definition.Key}'."
                : $"Unit {command.UnitId} already has a role '{definition.Key}'.");
        await roles.AddAsync(definition, ct);
        return RoleDefinitionDto.From(definition);
    }
}

/// <summary>
/// Change a role definition's names, permissions and lead flag (its key and unit stay). The published shifts that use it
/// and have not ended get their grants rewritten. The lead flag cannot change while a shift or a template uses it.
/// </summary>
public sealed record UpdateRoleDefinitionCommand(int Id, string NameEn, string? NameAr, IReadOnlyList<string> Permissions, bool IsLead)
    : ICommand<Result<RoleDefinitionDto>>;

public sealed class UpdateRoleDefinitionHandler(IRepository<ShiftRoleDefinition> roles, RoleUsage usage, ShiftAuthority authority,
    RolePermissions catalog, RoleGrants grants) : ICommandHandler<UpdateRoleDefinitionCommand, Result<RoleDefinitionDto>>
{
    public async ValueTask<Result<RoleDefinitionDto>> Handle(UpdateRoleDefinitionCommand command, CancellationToken ct)
    {
        var all = await usage.AllAsync(ct);
        var definition = all.FirstOrDefault(d => d.Id == command.Id);
        if (definition is null) return RoleDefinitionDto.NotFound<RoleDefinitionDto>(command.Id);
        var allowed = await authority.RequireManageAsync(definition.UnitId, ct);
        if (!allowed.IsSuccess) return allowed.As<RoleDefinitionDto>();
        var known = await catalog.CheckAsync(command.Permissions, ct);
        if (!known.IsSuccess) return known.As<RoleDefinitionDto>();

        var shifts = await usage.ShiftsAsync(definition, all, ct);
        if (command.IsLead != definition.IsLead && (shifts.Count > 0 || (await usage.TemplatesAsync(definition, all, ct)).Count > 0))
            return Result.Conflict($"Role '{definition.Key}' is on shifts that have not ended or on templates: its lead flag cannot change. Add a new role instead.");

        definition.Update(command.NameEn, command.NameAr ?? "", command.Permissions, command.IsLead);
        await roles.UpdateAsync(definition, ct);
        await grants.RewriteAsync(shifts, ct);
        return RoleDefinitionDto.From(definition);
    }
}

/// <summary>
/// Remove a role definition. Refused while shifts that have not ended or templates use it, unless another definition of
/// the same key (a parent unit's, or the tenant's) with the same lead flag takes over for them; their grants then follow it.
/// </summary>
public sealed record DeleteRoleDefinitionCommand(int Id) : ICommand<Result>;

public sealed class DeleteRoleDefinitionHandler(IRepository<ShiftRoleDefinition> roles, RoleUsage usage, ShiftAuthority authority, RoleGrants grants)
    : ICommandHandler<DeleteRoleDefinitionCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteRoleDefinitionCommand command, CancellationToken ct)
    {
        var all = await usage.AllAsync(ct);
        var definition = all.FirstOrDefault(d => d.Id == command.Id);
        if (definition is null) return Result.NotFound($"Role definition {command.Id} was not found.");
        var allowed = await authority.RequireManageAsync(definition.UnitId, ct);
        if (!allowed.IsSuccess) return allowed;

        var shifts = await usage.ShiftsAsync(definition, all, ct);
        var templates = await usage.TemplatesAsync(definition, all, ct);
        var rest = all.Where(d => !ReferenceEquals(d, definition)).ToArray();
        foreach (var unitId in shifts.Select(s => s.UnitId).Concat(templates.Select(t => (Guid?)t.UnitId)).Distinct())
            if (await usage.ResolveAsync(unitId, definition.Key, rest, ct) is not { } heir || heir.IsLead != definition.IsLead)
                return Result.Conflict($"Role '{definition.Key}' is on shifts that have not ended or on templates, and no other definition takes over: change them first.");

        await roles.DeleteAsync(definition, ct);
        await grants.RewriteAsync(shifts, ct);
        return Result.Success();
    }
}

/// <summary>Permission names checked against the platform's permission catalog (the platform's own and every installed module's).</summary>
public sealed class RolePermissions(IPermissionCatalog catalog)
{
    public async Task<Result> CheckAsync(IEnumerable<string>? permissions, CancellationToken ct)
    {
        var names = (permissions ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0) return Result.Success();
        var known = (await catalog.ListAsync(ct)).SelectMany(m => m.Permissions).Select(p => p.Name)
            .Concat(PlatformPermissions.All.Select(p => p.Name)).ToHashSet(StringComparer.Ordinal);
        var unknown = names.Where(n => !known.Contains(n)).ToArray();
        return unknown.Length == 0
            ? Result.Success()
            : Result.Invalid(new ValidationError("permissions", $"Not in the permission catalog: {string.Join(", ", unknown)}."));
    }
}

/// <summary>Rewrites the grants of published shifts after a role they use changed.</summary>
public sealed class RoleGrants(IReadRepository<Section> sections, ShiftGrants grants)
{
    public async Task RewriteAsync(IEnumerable<Shift> shifts, CancellationToken ct)
    {
        var published = shifts.Where(s => s.Published).ToArray();
        if (published.Length == 0) return;
        var bySection = (await sections.ListAsync(new SectionsWithCategoriesSpec(), ct)).ToDictionary(s => s.Id);
        foreach (var shift in published)
            await grants.WriteAsync(shift, shift.SectionId is { } id ? bySection.GetValueOrDefault(id) : null, ct);
    }
}
