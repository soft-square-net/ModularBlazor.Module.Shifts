using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetAutomation;

public sealed class GetShiftAutomationHandler(IReadRepository<Shift> shifts, IReadRepository<Section> sections, IOptions<ShiftOptions> options,
    ShiftThresholds thresholds) : IQueryHandler<GetShiftAutomationQuery, Result<ShiftAutomationInfo>>
{
    public async ValueTask<Result<ShiftAutomationInfo>> Handle(GetShiftAutomationQuery query, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(query.Id), ct);
        if (shift is null) return ShiftDetails.NotFound<ShiftAutomationInfo>(query.Id);

        // A shift with no section (a template's clone) uses the module's assignment delay; the grace is the unit's.
        Section? section = null;
        if (shift.SectionId is { } sectionId)
        {
            section = await sections.FirstOrDefaultAsync(new SectionByIdSpec(sectionId), ct);
            if (section is null) return SectionDto.NotFound<ShiftAutomationInfo>(sectionId);
        }
        var autoAssign = section?.AutoAssignDelayOr(options.Value.AutoAssignDelay) ?? options.Value.AutoAssignDelay;
        var grace = (await thresholds.ForAsync(shift, section, ct)).CheckoutGrace;
        return new ShiftAutomationInfo(shift.Id, shift.SectionId, shift.StartsAt, shift.EndsAt, shift.CheckedOutAt, autoAssign, grace);
    }
}
