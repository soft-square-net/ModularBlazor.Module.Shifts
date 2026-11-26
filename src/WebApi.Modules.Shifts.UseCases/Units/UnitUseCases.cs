using Microsoft.Extensions.Logging;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

/// <summary>A cached Organization unit as callers see it.</summary>
public sealed record UnitDto(Guid Id, Guid OrgId, Guid? ParentId, string TypeKey, string Path, string NameEn, string NameAr,
    IReadOnlyList<string> Tags, string? TimeZone, IReadOnlyList<string> Categories, int Version)
{
    public static UnitDto From(CachedUnit u) =>
        new(u.Id, u.OrgId, u.ParentId, u.TypeKey, u.Path, u.NameEn, u.NameAr, u.Tags, u.TimeZone, u.Categories, u.Version);
}

/// <summary>The units this module knows (its cache of the tenant's Organization units), by path.</summary>
public sealed record ListUnitsQuery : IQuery<Result<IReadOnlyList<UnitDto>>>;

public sealed class ListUnitsHandler(UnitCache cache) : IQueryHandler<ListUnitsQuery, Result<IReadOnlyList<UnitDto>>>
{
    public async ValueTask<Result<IReadOnlyList<UnitDto>>> Handle(ListUnitsQuery query, CancellationToken ct) =>
        (await cache.ListAsync(ct)).Select(UnitDto.From).ToArray();
}

/// <summary>Refill the unit cache from the deployment's unit directory, as the signed-in admin.</summary>
public sealed record ResyncUnitsCommand : ICommand<Result<UnitResyncResult>>;

public sealed class ResyncUnitsHandler(UnitCache cache, IOrganizationAccess organization, ShiftAuthority authority, ILogger<ResyncUnitsHandler> log)
    : ICommandHandler<ResyncUnitsCommand, Result<UnitResyncResult>>
{
    public async ValueTask<Result<UnitResyncResult>> Handle(ResyncUnitsCommand command, CancellationToken ct)
    {
        var allowed = await authority.RequireTenantManageAsync(ct);
        if (!allowed.IsSuccess) return allowed.As<UnitResyncResult>();
        if (!organization.Configured) return Result.Unavailable("Shifts:Organization:BaseUrl is not set: the module cannot reach Organization.");
        IReadOnlyList<soft.square.net.Organization.Contracts.DirectoryUnitDto> units;
        try { units = await organization.DirectoryAsCaller().ListAsync(ct); }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "Reading the Organization unit directory failed");
            return Result.Unavailable($"The Organization unit directory could not be read: {e.Message}");
        }
        var result = await cache.ResyncAsync(units, ct);
        await cache.SaveAsync(ct);
        return result;
    }
}
