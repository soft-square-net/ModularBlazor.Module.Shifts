namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Serving;

/// <summary>
/// Which sections serve category <paramref name="CategoryId"/>: the category's own sections first, then every
/// department (each group by name). The workflow routes a content with it: text to the category's section, images to
/// the Multimedia department, SEO values to SEO.
/// </summary>
public sealed record ListServingSectionsQuery(string CategoryId) : IQuery<Result<IReadOnlyList<SectionDto>>>;
