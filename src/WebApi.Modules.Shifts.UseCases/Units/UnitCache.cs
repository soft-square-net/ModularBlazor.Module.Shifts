using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using OrgApi = soft.square.net.WebApi.Modules.Organization.Client;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

/// <summary>
/// The module's copy of the Organization units of the tenant. Every change follows the unit's version, so events can
/// arrive late, twice or out of order: a state that is not newer than the cached one is ignored, and a removed unit
/// stays removed. Nothing here saves; the caller does (or the platform, in an event handler).
/// </summary>
public sealed class UnitCache(IUnitCacheStore store)
{
    /// <summary>A cached unit that Organization has not removed, or null.</summary>
    public async Task<CachedUnit?> FindAsync(Guid id, CancellationToken ct) =>
        await store.FindAsync(id, ct) is { Removed: false } unit ? unit : null;

    /// <summary>A unit was created or changed. True when the cache changed.</summary>
    public async Task<bool> ApplyAsync(UnitState state, CancellationToken ct)
    {
        var unit = await store.FindAsync(state.Id, ct);
        if (unit is null)
        {
            store.Add(CachedUnit.From(state));
            return true;
        }
        var oldPath = unit.Path;
        if (!unit.Apply(state)) return false;
        // A newer state with another path means the unit moved (its moved event may still be on its way): its subtree follows.
        if (!string.Equals(oldPath, unit.Path, StringComparison.Ordinal)) await RebaseBelowAsync(oldPath, unit.Path, ct);
        return true;
    }

    /// <summary>A unit and its subtree moved. True when the cache changed.</summary>
    public async Task<bool> MoveAsync(UnitMoved moved, CancellationToken ct)
    {
        var oldPath = moved.OldPath;
        if (await store.FindAsync(moved.UnitId, ct) is { } unit)
        {
            oldPath = unit.Path;
            // A late move must not drag the subtree back, so the subtree only follows a move that is newer for the unit.
            if (!unit.Move(moved.NewParentId, moved.NewPath, moved.Version)) return false;
        }
        await RebaseBelowAsync(oldPath, moved.NewPath, ct);
        return true;
    }

    /// <summary>Organization removed a unit: it stays as a tombstone, so a late event cannot bring it back.</summary>
    public async Task<bool> RemoveAsync(UnitRemoved removed, CancellationToken ct)
    {
        if (await store.FindAsync(removed.UnitId, ct) is { } unit) return unit.Remove();
        var tombstone = CachedUnit.From(new UnitState(removed.UnitId, removed.OrgId, null, "", removed.Path, "", "", [], null, [], 0));
        tombstone.Remove();
        store.Add(tombstone);
        return true;
    }

    /// <summary>
    /// The directory's current list replaces the cache: each unit is applied (newer versions only), and the cached units
    /// the directory no longer lists are deleted (not tombstoned: a unit created meanwhile comes back with its next event).
    /// </summary>
    public async Task<UnitResyncResult> ResyncAsync(IReadOnlyList<DirectoryUnitDto> units, CancellationToken ct)
    {
        var changed = 0;
        foreach (var unit in units)
            if (await ApplyAsync(UnitStates.From(unit), ct)) changed++;

        var listed = units.Select(u => u.Id).ToHashSet();
        var deleted = 0;
        foreach (var stale in (await store.ListAsync(ct)).Where(u => !u.Removed && !listed.Contains(u.Id)).ToArray())
        {
            store.Delete(stale);
            deleted++;
        }
        return new UnitResyncResult(units.Count, changed, deleted);
    }

    /// <summary>
    /// The Content post categories of a unit, as Organization's <c>CategoriesOf</c> computes them: its own when it has
    /// some (a Section), otherwise those of the cached units below it, each once.
    /// </summary>
    public async Task<IReadOnlyList<string>> CategoriesOfAsync(CachedUnit unit, CancellationToken ct)
    {
        if (unit.Categories.Count > 0) return unit.Categories;
        return [.. (await store.BelowAsync(unit.Path, ct)).Where(u => !u.Removed).SelectMany(u => u.Categories).Distinct().Order(StringComparer.Ordinal)];
    }

    /// <summary>Every cached unit that is not removed, by path.</summary>
    public async Task<IReadOnlyList<CachedUnit>> ListAsync(CancellationToken ct) =>
        [.. (await store.ListAsync(ct)).Where(u => !u.Removed).OrderBy(u => u.Path, StringComparer.Ordinal)];

    public Task SaveAsync(CancellationToken ct) => store.SaveAsync(ct);

    private async Task RebaseBelowAsync(string oldPath, string newPath, CancellationToken ct)
    {
        foreach (var below in await store.BelowAsync(oldPath, ct)) below.Rebase(oldPath, newPath);
    }
}

/// <summary>A resync: how many units the directory listed, how many changed the cache, and how many cached ones were dropped.</summary>
public sealed record UnitResyncResult(int Listed, int Changed, int Deleted);

/// <summary>Organization's unit shapes as a <see cref="UnitState"/>.</summary>
public static class UnitStates
{
    public static UnitState From(OrgUnitSnapshot u) =>
        new(u.UnitId, u.OrgId, u.ParentId, u.TypeKey, u.Path, u.NameEn, u.NameAr, u.Tags ?? [], u.TimeZone, u.Categories ?? [], u.Version);

    public static UnitState From(DirectoryUnitDto u) =>
        new(u.Id, u.OrgId, u.ParentId, u.TypeKey, u.Path, u.NameEn, u.NameAr, u.Tags ?? [], u.TimeZone, u.Categories ?? [], u.Version);

    public static UnitState From(OrgApi.OrgUnitDto u) =>
        new(u.Id, u.OrgId, u.ParentId, u.TypeKey, u.Path, u.NameEn, u.NameAr, u.Tags ?? [], u.TimeZone, u.Categories ?? [], u.Version);
}
