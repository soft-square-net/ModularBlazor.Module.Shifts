using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// A published rota as grants in the host. Every change to a published shift (its rota, a check-out, or its section's
/// categories) rewrites the shift's grants in one go: <see cref="IGrantStore.ReplaceSourceAsync"/> with source
/// <c>shift:{id}</c>. Access then starts and ends on the clock: the host evaluates each grant's window per request.
/// </summary>
public sealed class ShiftGrants(IGrantStore grants)
{
    /// <summary>Each member's role permissions × the section's categories, valid from the start to the member's end.</summary>
    public Task WriteAsync(Shift shift, Section section, CancellationToken ct)
    {
        if (!shift.Published) return Task.CompletedTask;
        var rows = shift.Members
            .Where(m => shift.EndOf(m) > shift.StartsAt)
            .SelectMany(m => RoleTemplates.Permissions[m.Role].SelectMany(permission => section.Scopes.Select(scope =>
                new NewGrant(m.UserId, permission, scope, shift.StartsAt, shift.EndOf(m)))))
            .Distinct()
            .ToArray();
        return grants.ReplaceSourceAsync(shift.GrantSource, rows, ct);
    }

    /// <summary>Removes every grant of the shift.</summary>
    public Task ClearAsync(Shift shift, CancellationToken ct) => grants.ReplaceSourceAsync(shift.GrantSource, [], ct);
}
