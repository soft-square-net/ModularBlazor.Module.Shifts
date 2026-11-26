using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

/// <summary><see cref="IUnitCacheStore"/> over the scope's <see cref="ShiftsDbContext"/> (tracked rows, saved with the unit of work).</summary>
public sealed class EfUnitCacheStore(ShiftsDbContext db) : IUnitCacheStore
{
    public async Task<CachedUnit?> FindAsync(Guid id, CancellationToken ct) => await db.Units.FindAsync([id], ct);

    public async Task<IReadOnlyList<CachedUnit>> ListAsync(CancellationToken ct) => await db.Units.ToListAsync(ct);

    public async Task<IReadOnlyList<CachedUnit>> BelowAsync(string path, CancellationToken ct)
    {
        var prefix = path + "/";
        return await db.Units.Where(u => u.Path.StartsWith(prefix)).ToListAsync(ct);
    }

    public void Add(CachedUnit unit) => db.Units.Add(unit);

    public void Delete(CachedUnit unit) => db.Units.Remove(unit);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
