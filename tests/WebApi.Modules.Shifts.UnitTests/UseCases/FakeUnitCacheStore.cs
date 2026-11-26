using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases;

/// <summary>The unit cache's rows in memory: tracked objects, as the DbContext would hand them out. Counts the saves.</summary>
public sealed class FakeUnitCacheStore : IUnitCacheStore
{
    public Dictionary<Guid, CachedUnit> Rows { get; } = [];
    public int Saves { get; private set; }

    public Task<CachedUnit?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(Rows.GetValueOrDefault(id));

    public Task<IReadOnlyList<CachedUnit>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CachedUnit>>([.. Rows.Values]);

    public Task<IReadOnlyList<CachedUnit>> BelowAsync(string path, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CachedUnit>>([.. Rows.Values.Where(u => u.Path.StartsWith(path + "/", StringComparison.Ordinal))]);

    public void Add(CachedUnit unit) => Rows.Add(unit.Id, unit);

    public void Delete(CachedUnit unit) => Rows.Remove(unit.Id);

    public Task SaveAsync(CancellationToken ct)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
