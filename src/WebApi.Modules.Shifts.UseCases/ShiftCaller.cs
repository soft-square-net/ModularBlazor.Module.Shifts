namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>Who calls, and when: the signed-in user and the clock (rights: <see cref="Authority.ShiftAuthority"/>).</summary>
public sealed class ShiftCaller(IAccessScopes access, TimeProvider clock)
{
    public DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>The caller's user id; endpoints that use it require a signed-in user.</summary>
    public string Me => access.UserId ?? throw new ShiftAccessDeniedException("Sign in first.");
}
