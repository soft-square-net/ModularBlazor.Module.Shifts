namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;

/// <summary>A saved entity's id, for tests that need one (the database sets it; the setter is protected).</summary>
public static class SectionIds
{
    public static T WithId<T>(this T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
