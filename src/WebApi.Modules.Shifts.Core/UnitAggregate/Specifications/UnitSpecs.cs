namespace soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate.Specifications;

/// <summary>Every cached unit (removed ones included), by path.</summary>
public sealed class AllCachedUnitsSpec : Specification<CachedUnit>
{
    public AllCachedUnitsSpec(bool includeRemoved = true)
    {
        if (!includeRemoved) Query.Where(u => !u.Removed);
        Query.OrderBy(u => u.Path);
    }
}

/// <summary>The cached units strictly below a path (the descendants of the unit at <paramref name="path"/>).</summary>
public sealed class UnitsBelowSpec : Specification<CachedUnit>
{
    public UnitsBelowSpec(string path)
    {
        var prefix = path + "/";
        Query.Where(u => u.Path.StartsWith(prefix));
    }
}

/// <summary>The attendance overrides of these units (any order).</summary>
public sealed class UnitAttendanceOfSpec : Specification<UnitAttendance>
{
    public UnitAttendanceOfSpec(IReadOnlyCollection<Guid> unitIds) => Query.Where(u => unitIds.Contains(u.Id));
}
