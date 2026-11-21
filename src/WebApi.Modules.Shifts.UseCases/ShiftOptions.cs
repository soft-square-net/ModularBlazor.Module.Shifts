namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>Module settings (section <c>Shifts</c>); sections may override them.</summary>
public sealed class ShiftOptions
{
    /// <summary>How long the manager has to assign an accepted article before the workflow assigns it. Default 5 minutes.</summary>
    public TimeSpan AutoAssignDelay { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>How long after its scheduled end a shift that nobody checked out is checked out automatically. Default 15 minutes.</summary>
    public TimeSpan CheckoutGrace { get; set; } = TimeSpan.FromMinutes(15);
}
