using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

/// <summary>A section's effective timers: its override, else the module's setting.</summary>
public sealed record SectionSettings(int SectionId, TimeSpan AutoAssignDelay, TimeSpan CheckoutGrace)
{
    public static SectionSettings From(Section section, ShiftOptions options) =>
        new(section.Id, section.AutoAssignDelayOr(options.AutoAssignDelay), section.CheckoutGraceOr(options.CheckoutGrace));
}
