namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

/// <summary>
/// A shift section. A <see cref="SectionKind.Category"/> section is the desk that manages a set of categories (possibly
/// of several presses); its categories become the scopes of the rota's grants. A <see cref="SectionKind.Department"/>
/// (Multimedia, SEO) serves every category: it has no category rows and its rota is granted on the section itself
/// (<c>section:{id}</c>). Optional overrides of the module's settings apply to its shifts.
/// </summary>
public sealed class Section : AggregateRoot<int>, ITenantOwned
{
    private readonly List<SectionCategory> _categories = [];

    private Section() { } // EF Core

    public string TenantId { get; set; } = "";
    public string Name { get; private set; } = "";
    public SectionKind Kind { get; private set; } = SectionKind.Category;
    public bool IsDepartment => Kind == SectionKind.Department;
    /// <summary>Override of the manager's assignment delay before the workflow assigns on its own (minutes).</summary>
    public int? AutoAssignDelayMinutes { get; private set; }
    /// <summary>Override of the grace period before an un-checked-out shift is checked out automatically (minutes).</summary>
    public int? CheckoutGraceMinutes { get; private set; }

    public IReadOnlyList<SectionCategory> Categories => _categories;

    public static Section Create(string name, IEnumerable<string> categoryIds, int? autoAssignDelayMinutes, int? checkoutGraceMinutes,
        SectionKind kind = SectionKind.Category)
    {
        var section = new Section();
        section.Update(name, categoryIds, autoAssignDelayMinutes, checkoutGraceMinutes, kind);
        return section;
    }

    /// <summary>A department: serves every category, so it takes no categories.</summary>
    public static Section CreateDepartment(string name, int? autoAssignDelayMinutes = null, int? checkoutGraceMinutes = null) =>
        Create(name, [], autoAssignDelayMinutes, checkoutGraceMinutes, SectionKind.Department);

    /// <summary>
    /// Changes the section. A category section covers at least one category; a department covers none (it serves them
    /// all), so turning a category section into a department needs its categories removed in the same call.
    /// </summary>
    public void Update(string name, IEnumerable<string> categoryIds, int? autoAssignDelayMinutes, int? checkoutGraceMinutes,
        SectionKind kind = SectionKind.Category)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) throw new DomainException("A section needs a name (at most 200 characters).");
        if (!Enum.IsDefined(kind)) throw new DomainException($"Unknown section kind {kind}.");
        var ids = categoryIds.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToArray();
        if (kind == SectionKind.Department && ids.Length > 0)
            throw new DomainException("A department serves every category: it takes no categories.");
        if (kind == SectionKind.Category && ids.Length == 0) throw new DomainException("A section covers at least one category.");
        if (autoAssignDelayMinutes is < 0 || checkoutGraceMinutes is < 0) throw new DomainException("Delays cannot be negative.");

        Name = name.Trim();
        Kind = kind;
        AutoAssignDelayMinutes = autoAssignDelayMinutes;
        CheckoutGraceMinutes = checkoutGraceMinutes;
        _categories.RemoveAll(c => !ids.Contains(c.CategoryId));
        foreach (var id in ids.Where(id => _categories.All(c => c.CategoryId != id))) _categories.Add(new SectionCategory(id));
    }

    /// <summary>Whether the section serves this category: a department serves every one, a category section its own.</summary>
    public bool Serves(string categoryId) => IsDepartment || _categories.Any(c => c.CategoryId == categoryId);

    /// <summary>
    /// The grant scopes the section covers: <c>category:{id}</c> for each of a category section's categories, and
    /// <c>section:{id}</c> for a department (the contents routed to it, in any category). Read it once the section is
    /// saved, since a department's scope needs its id.
    /// </summary>
    public IReadOnlyList<string> Scopes => IsDepartment
        ? [$"section:{Id}"]
        : _categories.Select(c => $"category:{c.CategoryId}").ToArray();

    /// <summary>The section's override of the assignment delay, else <paramref name="fallback"/> (the module's setting).</summary>
    public TimeSpan AutoAssignDelayOr(TimeSpan fallback) =>
        AutoAssignDelayMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : fallback;

    /// <summary>The section's override of the check-out grace period, else <paramref name="fallback"/> (the module's setting).</summary>
    public TimeSpan CheckoutGraceOr(TimeSpan fallback) =>
        CheckoutGraceMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : fallback;
}
