using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>Shifts as <see cref="ShiftDto"/>s, with their section's name and categories, as of now.</summary>
public sealed class ShiftDetails(IReadRepository<Shift> shifts, IReadRepository<Section> sections, ShiftCaller caller)
{
    public static Result<T> NotFound<T>(int id) => Result<T>.NotFound($"Shift {id} was not found.");

    public async Task<Result<ShiftDto>> GetAsync(int id, CancellationToken ct) =>
        await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(id), ct) is { } shift
            ? (await ToDtosAsync([shift], ct))[0]
            : NotFound<ShiftDto>(id);

    public async Task<IReadOnlyList<ShiftDto>> ListAsync(ISpecification<Shift> spec, CancellationToken ct) =>
        await ToDtosAsync(await shifts.ListAsync(spec, ct), ct);

    public async Task<IReadOnlyList<ShiftDto>> ToDtosAsync(IReadOnlyList<Shift> list, CancellationToken ct)
    {
        if (list.Count == 0) return [];
        var bySection = (await sections.ListAsync(new SectionsWithCategoriesSpec(), ct)).ToDictionary(s => s.Id);
        var now = caller.Now;
        return list.Select(shift =>
        {
            var section = bySection.GetValueOrDefault(shift.SectionId);
            return new ShiftDto(shift.Id, shift.SectionId, section?.Name ?? "?", section?.Categories.Select(c => c.CategoryId).ToArray() ?? [],
                shift.StartsAt, shift.EndsAt, shift.Published, shift.IsRunning(now), shift.CheckedOutAt, shift.CheckedOutBy, shift.GrantSource,
                shift.Members.OrderBy(m => m.Role).ThenBy(m => m.UserId)
                    .Select(m => new RotaMemberDto(m.UserId, m.Role, m.CheckedOutAt, shift.EndOf(m))).ToArray(),
                shift.UnitId);
        }).ToArray();
    }
}
