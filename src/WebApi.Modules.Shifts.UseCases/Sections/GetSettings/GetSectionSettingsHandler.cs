using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.GetSettings;

public sealed class GetSectionSettingsHandler(IReadRepository<Section> sections, IOptions<ShiftOptions> options)
    : IQueryHandler<GetSectionSettingsQuery, Result<SectionSettings>>
{
    public async ValueTask<Result<SectionSettings>> Handle(GetSectionSettingsQuery query, CancellationToken ct) =>
        await sections.FirstOrDefaultAsync(new SectionByIdSpec(query.SectionId), ct) is { } section
            ? SectionSettings.From(section, options.Value)
            : SectionDto.NotFound<SectionSettings>(query.SectionId);
}
