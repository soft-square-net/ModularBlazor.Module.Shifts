using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

/// <summary>
/// The <c>unit_cache</c> rows, tracked in the scope's unit of work (Infrastructure: the module's DbContext). Changes are
/// written by <see cref="SaveAsync"/> or by the next save of the same unit of work; an integration event handler never
/// saves (the platform saves its changes with the inbox row).
/// </summary>
public interface IUnitCacheStore
{
    Task<CachedUnit?> FindAsync(Guid id, CancellationToken ct);

    /// <summary>Every cached unit, removed ones (tombstones) included.</summary>
    Task<IReadOnlyList<CachedUnit>> ListAsync(CancellationToken ct);

    /// <summary>The cached units strictly below <paramref name="path"/>.</summary>
    Task<IReadOnlyList<CachedUnit>> BelowAsync(string path, CancellationToken ct);

    void Add(CachedUnit unit);

    void Delete(CachedUnit unit);

    Task SaveAsync(CancellationToken ct);
}
