using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>One unit of the picker with its children, in tree order.</summary>
public sealed class UnitNode(UnitDto unit, int level)
{
    public UnitDto Unit { get; } = unit;

    /// <summary>0 for a root (or for a unit whose parent is not cached).</summary>
    public int Level { get; } = level;

    public List<UnitNode> Children { get; } = [];
}

/// <summary>
/// The units this module caches (GET /api/shifts/units) as a tree, the way the Organization module's explorer builds
/// its own: parents before children by the path's depth, siblings by name.
/// </summary>
public sealed class UnitTree
{
    public const string DepartmentType = "department";
    public const string SectionType = "section";

    private readonly Dictionary<Guid, UnitNode> _nodes;

    private UnitTree(List<UnitNode> roots, Dictionary<Guid, UnitNode> nodes)
    {
        Roots = roots;
        _nodes = nodes;
    }

    public static UnitTree Empty { get; } = new([], []);

    public IReadOnlyList<UnitNode> Roots { get; }

    public int Count => _nodes.Count;

    public static UnitTree Build(IEnumerable<UnitDto> units)
    {
        var nodes = new Dictionary<Guid, UnitNode>();
        var roots = new List<UnitNode>();
        foreach (var unit in units.OrderBy(u => Depth(u.Path)))
        {
            if (unit.ParentId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                var node = new UnitNode(unit, parent.Level + 1);
                parent.Children.Add(node);
                nodes[unit.Id] = node;
            }
            else
            {
                var node = new UnitNode(unit, 0);
                roots.Add(node);
                nodes[unit.Id] = node;
            }
        }
        foreach (var node in nodes.Values) node.Children.Sort(Compare);
        roots.Sort(Compare);
        return new UnitTree(roots, nodes);
    }

    private static int Compare(UnitNode a, UnitNode b) => string.Compare(a.Unit.NameEn, b.Unit.NameEn, StringComparison.OrdinalIgnoreCase);

    private static int Depth(string? path) => string.IsNullOrEmpty(path) ? 0 : path.Count(c => c == '/');

    public UnitDto? Find(Guid? id) => id is { } known && _nodes.TryGetValue(known, out var node) ? node.Unit : null;

    /// <summary>Every unit, depth first in tree order (for pickers).</summary>
    public IEnumerable<UnitNode> All()
    {
        var stack = new Stack<UnitNode>(Roots.Reverse());
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--) stack.Push(node.Children[i]);
        }
    }

    /// <summary>The unit and every unit below it; only the id itself when the unit is not cached.</summary>
    public IReadOnlySet<Guid> SelfAndBelow(Guid id)
    {
        var set = new HashSet<Guid> { id };
        if (!_nodes.TryGetValue(id, out var start)) return set;
        var queue = new Queue<UnitNode>([start]);
        while (queue.Count > 0)
            foreach (var child in queue.Dequeue().Children)
            {
                set.Add(child.Unit.Id);
                queue.Enqueue(child);
            }
        return set;
    }

    /// <summary>
    /// The time zone of a unit, as the API's template clone takes it: the unit's own, else the nearest ancestor's; null
    /// when none of them has one.
    /// </summary>
    public string? TimeZoneIdOf(Guid? id)
    {
        for (var unit = Find(id); unit is not null; unit = Find(unit.ParentId))
            if (!string.IsNullOrWhiteSpace(unit.TimeZone)) return unit.TimeZone;
        return null;
    }

    /// <summary>A unit's name in Arabic or English (the English one when there is no Arabic one).</summary>
    public static string NameOf(UnitDto unit, bool arabic) => arabic && !string.IsNullOrWhiteSpace(unit.NameAr) ? unit.NameAr : unit.NameEn;
}
