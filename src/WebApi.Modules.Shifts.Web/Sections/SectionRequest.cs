namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>The body of <see cref="Add"/> and <see cref="Update"/>. Its name is the OpenAPI schema name: keep it stable.</summary>
public sealed record SectionRequest(string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes);
