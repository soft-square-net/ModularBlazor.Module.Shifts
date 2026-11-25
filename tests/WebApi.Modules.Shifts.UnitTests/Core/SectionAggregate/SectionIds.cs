using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;

/// <summary>A saved section's id, for tests that need one (the database sets it; the setter is protected).</summary>
public static class SectionIds
{
    public static Section WithId(this Section section, int id)
    {
        typeof(Section).GetProperty(nameof(Section.Id))!.SetValue(section, id);
        return section;
    }
}
