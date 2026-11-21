namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>One person in one role on a shift's rota.</summary>
public sealed class ShiftMember : EntityBase<int>
{
    private ShiftMember() { } // EF Core

    public ShiftMember(string userId, ShiftRole role)
    {
        UserId = userId;
        Role = role;
    }

    public string UserId { get; private set; } = "";
    public ShiftRole Role { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }

    internal void CheckOut(DateTimeOffset at) => CheckedOutAt = at;
}
