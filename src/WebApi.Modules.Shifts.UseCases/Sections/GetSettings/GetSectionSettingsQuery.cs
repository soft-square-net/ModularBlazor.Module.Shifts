namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.GetSettings;

/// <summary>A section's effective AutoAssignDelay and CheckoutGrace: its override, else the module's settings.</summary>
public sealed record GetSectionSettingsQuery(int SectionId) : IQuery<Result<SectionSettings>>;
