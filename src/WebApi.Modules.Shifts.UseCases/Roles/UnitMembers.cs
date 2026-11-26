using Microsoft.Extensions.Logging;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>
/// Who may be on a unit's rota (decision D6): the people with a membership of the unit or of one of its ancestors
/// (Organization's <c>MembersOf</c>, read as the caller), active at the shift's start, that have a sign-in identity (a
/// token subject: the rota's user ids are subjects). Lending people between units comes later.
/// </summary>
public sealed class UnitMembers(IOrganizationAccess organization, ILogger<UnitMembers> log)
{
    /// <summary>The subjects that may be on the rota of <paramref name="unit"/> at <paramref name="at"/>, or Unavailable.</summary>
    public async Task<Result<IReadOnlySet<string>>> EligibleAsync(CachedUnit unit, DateTimeOffset at, CancellationToken ct)
    {
        if (!organization.Configured)
            return Result.Unavailable("Shifts:Organization:BaseUrl is not set: the module cannot read the unit's members to check the rota.");
        var directory = organization.DirectoryAsCaller();
        var subjects = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var id in UnitPaths.Ids(unit.Path))
            {
                if (!Guid.TryParseExact(id, "N", out var unitId)) continue;
                foreach (var member in await directory.MembersOfAsync(unitId, ct))
                    if (!string.IsNullOrWhiteSpace(member.Subject) && member.From <= at && (member.To is null || member.To > at))
                        subjects.Add(member.Subject);
            }
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "Reading the members of unit {Unit} from Organization failed", unit.Id);
            return Result.Unavailable($"The members of unit {unit.Id} could not be read from Organization: {e.Message}");
        }
        return subjects;
    }
}
