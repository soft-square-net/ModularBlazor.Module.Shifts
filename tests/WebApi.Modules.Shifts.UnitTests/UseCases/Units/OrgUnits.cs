using System.Text.Json;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units;

/// <summary>A small org tree (org → department → two sections) and its org.* events, as Organization sends them.</summary>
public static class OrgUnits
{
    public static readonly Guid Org = Guid.Parse("0a000000-0000-0000-0000-000000000001");
    public static readonly Guid Root = Guid.Parse("0b000000-0000-0000-0000-000000000001");
    public static readonly Guid Department = Guid.Parse("0d000000-0000-0000-0000-000000000001");
    public static readonly Guid Sport = Guid.Parse("05000000-0000-0000-0000-000000000001");
    public static readonly Guid Seo = Guid.Parse("05000000-0000-0000-0000-000000000002");
    public static readonly Guid OtherDepartment = Guid.Parse("0d000000-0000-0000-0000-000000000002");

    public static string PathOf(params Guid[] ids) => string.Concat(ids.Select(id => "/" + UnitPaths.Id(id)));

    public static OrgUnitSnapshot Snapshot(Guid id, string path, Guid? parent, string type, int version, string name = "unit",
        IReadOnlyList<string>? tags = null, IReadOnlyList<string>? categories = null) =>
        new(id, Org, parent, type, path, name, name + " (ar)", tags ?? [], "Africa/Cairo", categories ?? [], TenantBindingKind.Inherit, null, version);

    public static OrgUnitSnapshot RootUnit(int version = 1) => Snapshot(Root, PathOf(Root), null, "org", version, "Newsroom");

    public static OrgUnitSnapshot DepartmentUnit(int version = 1, string name = "News") =>
        Snapshot(Department, PathOf(Root, Department), Root, CachedUnit.DepartmentType, version, name);

    public static OrgUnitSnapshot SportUnit(int version = 1, string name = "Sport", params string[] categories) =>
        Snapshot(Sport, PathOf(Root, Department, Sport), Department, CachedUnit.SectionType, version, name,
            categories: categories.Length > 0 ? categories : ["football", "tennis"]);

    public static OrgUnitSnapshot SeoUnit(int version = 1) =>
        Snapshot(Seo, PathOf(Root, Department, Seo), Department, CachedUnit.SectionType, version, "SEO", [CachedUnit.DepartmentTag], [CachedUnit.AnyCategory]);

    public static UnitState State(OrgUnitSnapshot s) => soft.square.net.WebApi.Modules.Shifts.UseCases.Units.UnitStates.From(s);

    public static IntegrationEnvelope Envelope(string type, object data, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), type, "acme", "unit:x", DateTimeOffset.UtcNow, "corr", "organization",
            JsonSerializer.SerializeToElement(data, data.GetType(), IntegrationEnvelope.JsonOptions));

    public static IntegrationEnvelope Created(OrgUnitSnapshot unit) => Envelope(OrgEvents.UnitCreated, new UnitCreated(unit));

    public static IntegrationEnvelope Updated(OrgUnitSnapshot unit) => Envelope(OrgEvents.UnitUpdated, new UnitUpdated(unit));

    public static IntegrationEnvelope Moved(Guid unit, Guid? oldParent, Guid newParent, string oldPath, string newPath, int version) =>
        Envelope(OrgEvents.UnitMoved, new UnitMoved(unit, Org, oldParent, newParent, oldPath, newPath, version));

    public static IntegrationEnvelope Removed(Guid unit, string path) => Envelope(OrgEvents.UnitRemoved, new UnitRemoved(unit, Org, path));

    public static IntegrationEnvelope MembershipOn(Guid unit, string path) =>
        Envelope(OrgEvents.MembershipChanged, new MembershipChanged(Guid.NewGuid(), Org, Guid.NewGuid(), "https://id", "sub-1", unit, path, null,
            "manager", ["shifts.manage"], DateTimeOffset.UtcNow, null, true, false));

    public static DirectoryUnitDto Directory(OrgUnitSnapshot s) =>
        new(s.UnitId, s.OrgId, s.ParentId, s.TypeKey, s.Path, s.NameEn, s.NameAr, s.Tags, s.TimeZone, s.Categories, s.Binding, s.SeedId, s.Version, true);
}
