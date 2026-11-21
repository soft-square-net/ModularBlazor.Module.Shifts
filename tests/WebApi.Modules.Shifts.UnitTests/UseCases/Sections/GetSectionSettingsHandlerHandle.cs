using Ardalis.Specification;
using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.GetSettings;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Sections;

public class GetSectionSettingsHandlerHandle
{
    private readonly IReadRepository<Section> _sections = Substitute.For<IReadRepository<Section>>();
    private readonly GetSectionSettingsHandler _handler;

    public GetSectionSettingsHandlerHandle() =>
        _handler = new GetSectionSettingsHandler(_sections, Options.Create(new ShiftOptions
        {
            AutoAssignDelay = TimeSpan.FromMinutes(5),
            CheckoutGrace = TimeSpan.FromMinutes(15)
        }));

    private void Stored(Section? section) =>
        _sections.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(section);

    [Fact]
    public async Task IsNotFoundWithoutTheSection()
    {
        Stored(null);

        var result = await _handler.Handle(new GetSectionSettingsQuery(3), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task UsesTheModuleSettingsWhereTheSectionHasNoOverride()
    {
        Stored(Section.Create("Desk", ["politics"], autoAssignDelayMinutes: 2, checkoutGraceMinutes: null));

        var result = await _handler.Handle(new GetSectionSettingsQuery(1), TestContext.Current.CancellationToken);

        result.Value.AutoAssignDelay.ShouldBe(TimeSpan.FromMinutes(2));
        result.Value.CheckoutGrace.ShouldBe(TimeSpan.FromMinutes(15));
    }
}
