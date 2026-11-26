namespace soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

/// <summary>
/// A unit of the Organization tree as this module last heard of it (table <c>unit_cache</c>). Fed by the
/// <c>org.unit-*</c> events and filled at start from the deployment's unit directory. Organization owns the data: the
/// cache only follows it, by <see cref="Version"/>, so an event that arrives late never overwrites a newer state.
/// A removed unit stays as a tombstone (<see cref="Removed"/>), so a late event cannot bring it back.
/// </summary>
public sealed class CachedUnit : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>The tag of a Section unit that serves every category (a department section, decision D1).</summary>
    public const string DepartmentTag = "department";

    /// <summary>The wildcard category of a department section: it matches every category (decision D1).</summary>
    public const string AnyCategory = "*";

    public const string SectionType = "section";
    public const string DepartmentType = "department";

    private CachedUnit() { } // EF Core

    public string TenantId { get; set; } = "";
    public Guid OrgId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string TypeKey { get; private set; } = "";
    /// <summary>The ids from the root to this unit, each after a slash, in the 32-digit form (Organization's UnitPaths).</summary>
    public string Path { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public IReadOnlyList<string> Tags { get; private set; } = [];
    public string? TimeZone { get; private set; }
    /// <summary>The unit's own Content post categories (a Section's); <see cref="AnyCategory"/> for a department section.</summary>
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public int Version { get; private set; }
    public bool Removed { get; private set; }

    public bool IsDepartmentSection => Tags.Contains(DepartmentTag);

    public static CachedUnit From(UnitState state)
    {
        var unit = new CachedUnit { Id = state.Id };
        unit.Set(state);
        return unit;
    }

    /// <summary>
    /// Takes a newer state of the unit. Returns false, and changes nothing, when the state is not newer than the cached
    /// one (a duplicate or a late event) or when the unit was removed.
    /// </summary>
    public bool Apply(UnitState state)
    {
        if (state.Id != Id) throw new DomainException($"State of unit {state.Id} applied to unit {Id}.");
        if (Removed || state.Version <= Version) return false;
        Set(state);
        return true;
    }

    /// <summary>The unit moved under <paramref name="newParentId"/>. Same rules as <see cref="Apply"/>.</summary>
    public bool Move(Guid newParentId, string newPath, int version)
    {
        if (Removed || version <= Version) return false;
        ParentId = newParentId;
        Path = newPath;
        Version = version;
        return true;
    }

    /// <summary>An ancestor moved: the part of the path under <paramref name="oldPrefix"/> now sits under <paramref name="newPrefix"/>.</summary>
    public bool Rebase(string oldPrefix, string newPrefix)
    {
        if (!IsSelfOrBelow(Path, oldPrefix)) return false;
        Path = newPrefix + Path[oldPrefix.Length..];
        return true;
    }

    /// <summary>Organization removed the unit (terminal: later events for it are ignored).</summary>
    public bool Remove()
    {
        if (Removed) return false;
        Removed = true;
        return true;
    }

    /// <summary>True when <paramref name="path"/> is <paramref name="ancestorPath"/> or below it.</summary>
    public static bool IsSelfOrBelow(string path, string ancestorPath) =>
        string.Equals(path, ancestorPath, StringComparison.Ordinal) || path.StartsWith(ancestorPath + "/", StringComparison.Ordinal);

    private void Set(UnitState s)
    {
        if (string.IsNullOrWhiteSpace(s.Path)) throw new DomainException($"Unit {s.Id} has no path.");
        OrgId = s.OrgId;
        ParentId = s.ParentId;
        TypeKey = s.TypeKey;
        Path = s.Path;
        NameEn = s.NameEn;
        NameAr = s.NameAr;
        Tags = [.. s.Tags.Distinct()];
        TimeZone = s.TimeZone;
        Categories = [.. s.Categories.Distinct()];
        Version = s.Version;
    }
}

/// <summary>A unit's state as Organization sends it (an event snapshot, a directory row or an API response).</summary>
public sealed record UnitState(
    Guid Id, Guid OrgId, Guid? ParentId, string TypeKey, string Path, string NameEn, string NameAr, IReadOnlyList<string> Tags,
    string? TimeZone, IReadOnlyList<string> Categories, int Version);
