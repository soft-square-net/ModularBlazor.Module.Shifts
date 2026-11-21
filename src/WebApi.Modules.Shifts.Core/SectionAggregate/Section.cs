namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

/// <summary>
/// A shift section: the desk that manages a set of categories (possibly of several presses). Its categories become
/// the scopes of the rota's grants. Optional overrides of the module's settings apply to its shifts.
/// </summary>
public sealed class Section : AggregateRoot<int>, ITenantOwned
{
    private readonly List<SectionCategory> _categories = [];

    private Section() { } // EF Core

    public string TenantId { get; set; } = "";
    public string Name { get; private set; } = "";
    /// <summary>Override of the manager's assignment delay before the workflow assigns on its own (minutes).</summary>
    public int? AutoAssignDelayMinutes { get; private set; }
    /// <summary>Override of the grace period before an un-checked-out shift is checked out automatically (minutes).</summary>
    public int? CheckoutGraceMinutes { get; private set; }

    public IReadOnlyList<SectionCategory> Categories => _categories;

    public static Section Create(string name, IEnumerable<string> categoryIds, int? autoAssignDelayMinutes, int? checkoutGraceMinutes)
    {
        var section = new Section();
        section.Update(name, categoryIds, autoAssignDelayMinutes, checkoutGraceMinutes);
        return section;
    }

    public void Update(string name, IEnumerable<string> categoryIds, int? autoAssignDelayMinutes, int? checkoutGraceMinutes)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) throw new DomainException("A section needs a name (at most 200 characters).");
        var ids = categoryIds.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToArray();
        if (ids.Length == 0) throw new DomainException("A section covers at least one category.");
        if (autoAssignDelayMinutes is < 0 || checkoutGraceMinutes is < 0) throw new DomainException("Delays cannot be negative.");

        Name = name.Trim();
        AutoAssignDelayMinutes = autoAssignDelayMinutes;
        CheckoutGraceMinutes = checkoutGraceMinutes;
        _categories.RemoveAll(c => !ids.Contains(c.CategoryId));
        foreach (var id in ids.Where(id => _categories.All(c => c.CategoryId != id))) _categories.Add(new SectionCategory(id));
    }

    /// <summary>The grant scopes the section covers (<c>category:{id}</c>).</summary>
    public IReadOnlyList<string> Scopes => _categories.Select(c => $"category:{c.CategoryId}").ToArray();

    /// <summary>The section's override of the assignment delay, else <paramref name="fallback"/> (the module's setting).</summary>
    public TimeSpan AutoAssignDelayOr(TimeSpan fallback) =>
        AutoAssignDelayMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : fallback;

    /// <summary>The section's override of the check-out grace period, else <paramref name="fallback"/> (the module's setting).</summary>
    public TimeSpan CheckoutGraceOr(TimeSpan fallback) =>
        CheckoutGraceMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : fallback;
}
