namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.List;

/// <summary>Shifts that overlap [From, To), optionally of one section. Defaults: from a day ago to a week ahead.</summary>
public sealed record ListShiftsQuery(int? SectionId, DateTimeOffset? From, DateTimeOffset? To) : IQuery<Result<IReadOnlyList<ShiftDto>>>;
