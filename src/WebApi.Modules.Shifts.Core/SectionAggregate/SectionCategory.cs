namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

/// <summary>A category the section covers: an id of the content module's taxonomy, kept as a plain string.</summary>
public sealed class SectionCategory : EntityBase<int>
{
    private SectionCategory() { } // EF Core

    public SectionCategory(string categoryId) => CategoryId = categoryId;

    public string CategoryId { get; private set; } = "";
}
