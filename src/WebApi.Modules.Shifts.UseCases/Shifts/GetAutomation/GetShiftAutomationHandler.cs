using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetAutomation;

public sealed class GetShiftAutomationHandler(IReadRepository<Shift> shifts, IReadRepository<Section> sections, IOptions<ShiftOptions> options)
    : IQueryHandler<GetShiftAutomationQuery, Result<ShiftAutomationInfo>>
{
    public async ValueTask<Result<ShiftAutomationInfo>> Handle(GetShiftAutomationQuery query, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(query.Id), ct);
        if (shift is null) return ShiftDetails.NotFound<ShiftAutomationInfo>(query.Id);
        var section = await sections.FirstOrDefaultAsync(new SectionByIdSpec(shift.SectionId), ct);
        if (section is null) return SectionDto.NotFound<ShiftAutomationInfo>(shift.SectionId);

        var settings = SectionSettings.From(section, options.Value);
        return new ShiftAutomationInfo(shift.Id, shift.SectionId, shift.StartsAt, shift.EndsAt, shift.CheckedOutAt,
            settings.AutoAssignDelay, settings.CheckoutGrace);
    }
}
