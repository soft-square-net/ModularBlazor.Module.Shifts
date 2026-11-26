using Microsoft.Extensions.Logging;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

/// <summary>
/// Keeps <see cref="UnitCache"/> in step with Organization: <c>org.unit-*.v1</c> and <c>org.membership-changed.v1</c>
/// (the patterns are in <see cref="Patterns"/> and in the manifest's Subscriptions). Runs in the platform's inbox
/// transaction, so it never saves: its changes commit with the inbox row, and a second delivery is dropped.
/// </summary>
public sealed class OrgEventsHandler(UnitCache cache, IOrganizationAccess organization, ILogger<OrgEventsHandler> log) : IIntegrationEventHandler
{
    /// <summary>The subscription patterns of this handler.</summary>
    public static IReadOnlyList<string> Patterns { get; } = [OrgEvents.AnyUnitEvent, OrgEvents.MembershipChanged];

    public async Task HandleAsync(IntegrationEnvelope envelope, CancellationToken ct)
    {
        var changed = envelope.Type switch
        {
            OrgEvents.UnitCreated => await cache.ApplyAsync(UnitStates.From(envelope.DataAs<UnitCreated>().Unit), ct),
            OrgEvents.UnitUpdated => await cache.ApplyAsync(UnitStates.From(envelope.DataAs<UnitUpdated>().Unit), ct),
            OrgEvents.UnitMoved => await cache.MoveAsync(envelope.DataAs<UnitMoved>(), ct),
            OrgEvents.UnitRemoved => await cache.RemoveAsync(envelope.DataAs<UnitRemoved>(), ct),
            OrgEvents.MembershipChanged => await LearnUnitAsync(envelope.DataAs<MembershipChanged>().UnitId, ct),
            _ => false // another org.unit-* event (a later v1 type this module does not know yet)
        };
        if (!changed) log.LogDebug("Org event {Id} ({Type}) left the unit cache as it was", envelope.Id, envelope.Type);
    }

    /// <summary>
    /// A membership names a unit. When the cache has never heard of it (an event missed before the module was installed),
    /// the unit is read from the directory as the module's service client. Without one, the next resync fills it.
    /// </summary>
    private async Task<bool> LearnUnitAsync(Guid unitId, CancellationToken ct)
    {
        if (await cache.FindAsync(unitId, ct) is not null || !organization.CanActAsService) return false;
        try
        {
            return await organization.DirectoryAsService().GetAsync(unitId, ct) is { } unit && await cache.ApplyAsync(UnitStates.From(unit), ct);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "Could not read unit {Unit} from the Organization directory; the next resync fills it", unitId);
            return false;
        }
    }
}
