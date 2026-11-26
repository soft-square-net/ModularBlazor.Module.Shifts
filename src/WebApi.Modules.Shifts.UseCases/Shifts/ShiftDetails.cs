using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>Shifts as <see cref="ShiftDto"/>s, with their section's name and categories and everyone's attendance, as of now.</summary>
public sealed class ShiftDetails(IReadRepository<Shift> shifts, IReadRepository<Section> sections, UnitCache units, ShiftCaller caller,
    ShiftThresholds thresholds)
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
        var unitNames = new Dictionary<Guid, string>();
        foreach (var unitId in list.Where(s => s.SectionId is null && s.UnitId is not null).Select(s => s.UnitId!.Value).Distinct())
            unitNames[unitId] = (await units.FindAsync(unitId, ct))?.NameEn ?? "?";
        var now = caller.Now;
        var dtos = new List<ShiftDto>(list.Count);
        foreach (var shift in list)
        {
            var section = shift.SectionId is { } sectionId ? bySection.GetValueOrDefault(sectionId) : null;
            var t = await thresholds.ForAsync(shift, section, ct);
            var name = section?.Name ?? (shift.UnitId is { } unit ? unitNames.GetValueOrDefault(unit) : null) ?? "?";
            dtos.Add(new ShiftDto(shift.Id, shift.SectionId, name, section?.Categories.Select(c => c.CategoryId).ToArray() ?? [],
                shift.StartsAt, shift.EndsAt, shift.Published, shift.IsRunning(now), shift.CheckedOutAt, shift.CheckedOutBy, shift.GrantSource,
                shift.Members.OrderByDescending(m => m.IsLead).ThenBy(m => m.RoleKey, StringComparer.Ordinal).ThenBy(m => m.UserId, StringComparer.Ordinal)
                    .Select(m => Member(shift, m, t, now)).ToArray(),
                shift.UnitId, shift.Ended, shift.StartsAt - t.EarlyCheckIn, shift.HandoverNote, shift.HandoverBy, shift.HandoverAt,
                AttendanceThresholdsDto.From(t), shift.EndedAt, shift.EndedBy));
        }
        return dtos;
    }

    private static RotaMemberDto Member(Shift shift, ShiftMember m, AttendanceThresholds t, DateTimeOffset now)
    {
        var access = m.AccessOn(shift, t.CheckoutGrace);
        return new RotaMemberDto(m.UserId, m.RoleKey, m.IsLead, AttendanceNames.Of(m.StatusOn(shift, t, now)), m.CheckedInAt, m.CheckedInBy,
            m.CheckedOutAt, m.CheckedOutBy, m.CheckedOutAutomatically, m.AbsentAt, access?.From, access?.To);
    }
}
