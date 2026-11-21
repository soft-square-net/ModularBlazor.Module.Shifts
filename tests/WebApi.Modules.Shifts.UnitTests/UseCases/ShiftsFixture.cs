using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases;

/// <summary>The shared shift services (ShiftCaller, ShiftGrants, ShiftDetails, ShiftChanges) over substituted platform contracts.</summary>
public sealed class ShiftsFixture
{
    public IAccessScopes Access { get; } = Substitute.For<IAccessScopes>();
    public IGrantStore Grants { get; } = Substitute.For<IGrantStore>();
    public IRepository<Shift> Shifts { get; } = Substitute.For<IRepository<Shift>>();
    public IReadRepository<Shift> ShiftReader { get; } = Substitute.For<IReadRepository<Shift>>();
    public IReadRepository<Section> SectionReader { get; } = Substitute.For<IReadRepository<Section>>();
    public TestClock Clock { get; } = new();

    public ShiftCaller Caller { get; }
    public ShiftGrants ShiftGrants { get; }
    public ShiftDetails Details { get; }
    public ShiftChanges Changes { get; }

    /// <summary>The section every stored shift belongs to: two categories.</summary>
    public Section Section { get; } = Section.Create("Politics desk", ["politics", "economy"], null, null);

    public ShiftsFixture(string userId)
    {
        Access.UserId.Returns(userId);
        Caller = new ShiftCaller(Access, Clock);
        ShiftGrants = new ShiftGrants(Grants);
        Details = new ShiftDetails(ShiftReader, SectionReader, Caller);
        Changes = new ShiftChanges(Shifts, SectionReader, ShiftGrants, Details);

        SectionReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(Section);
        SectionReader.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns([Section]);
    }

    /// <summary>Every single-shift read (for a change, and for the details after it) returns this shift.</summary>
    public void Stored(Shift shift)
    {
        Shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);
        ShiftReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);
    }

    public void ShiftAdmin() => Access.HasPermissionAsync(ShiftPermissions.Manage, Arg.Any<CancellationToken>()).Returns(true);

    /// <summary>The grants last written for the shift's source.</summary>
    public IReadOnlyCollection<NewGrant> WrittenGrants(Shift shift) =>
        (IReadOnlyCollection<NewGrant>)Grants.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGrantStore.ReplaceSourceAsync) && (string)c.GetArguments()[0]! == shift.GrantSource)
            .Last().GetArguments()[1]!;
}
