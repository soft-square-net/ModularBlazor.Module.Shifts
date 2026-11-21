namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.List;

/// <summary>Every section of the tenant, by name.</summary>
public sealed record ListSectionsQuery : IQuery<Result<IReadOnlyList<SectionDto>>>;
