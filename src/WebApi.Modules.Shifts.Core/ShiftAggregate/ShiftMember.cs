namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>
/// One person in one role on a shift's rota. <see cref="RoleKey"/> names a role definition (resolved for the shift's
/// unit); <see cref="IsLead"/> is the definition's lead flag when the person was put on the rota.
/// </summary>
public sealed class ShiftMember : EntityBase<int>
{
    private ShiftMember() { } // EF Core

    public ShiftMember(string userId, string roleKey, bool isLead)
    {
        UserId = userId;
        RoleKey = roleKey;
        IsLead = isLead;
    }

    public string UserId { get; private set; } = "";
    public string RoleKey { get; private set; } = "";
    public bool IsLead { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }

    internal void CheckOut(DateTimeOffset at) => CheckedOutAt = at;
}

/// <summary>One entry of a rota: a person, the key of their role, and whether that role is the shift's lead.</summary>
public readonly record struct RotaAssignment(string UserId, string RoleKey, bool IsLead);
