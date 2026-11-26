namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>
/// The body of <see cref="Add"/>. Its name is the OpenAPI schema name: keep it stable. <paramref name="UnitId"/> null: the
/// role holds for the whole tenant; otherwise for the unit and the units below it (overriding the same key above).
/// </summary>
public sealed record RoleRequest(string Key, string NameEn, string? NameAr, IReadOnlyList<string> Permissions, bool IsLead, Guid? UnitId = null);

/// <summary>The body of <see cref="Update"/>: a role's key and unit never change.</summary>
public sealed record RoleUpdateRequest(string NameEn, string? NameAr, IReadOnlyList<string> Permissions, bool IsLead);
