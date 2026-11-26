using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

/// <summary>A unit's own attendance overrides and the thresholds that hold for it (from its ancestors and the module's settings).</summary>
public sealed record GetUnitAttendanceQuery(Guid UnitId) : IQuery<Result<UnitAttendanceDto>>;

/// <summary>
/// Sets a unit's attendance overrides, in minutes (null: inherited from the units above it, or the module's settings).
/// Needs <c>shifts.manage</c> on the unit or one of its ancestors. The grants of the not-ended shifts of the unit and the
/// units below it are written again, since the check-out grace may have changed.
/// </summary>
public sealed record SetUnitAttendanceCommand(Guid UnitId, int? EarlyCheckInMinutes, int? LateMinutes, int? AbsentMinutes, int? OvertimeMinutes,
    int? CheckoutGraceMinutes) : ICommand<Result<UnitAttendanceDto>>;

public sealed class UnitAttendanceHandlers(
    IRepository<UnitAttendance> overrides, UnitCache units, ShiftThresholds thresholds, ShiftAuthority authority, ShiftCaller caller,
    IReadRepository<Shift> shifts, IReadRepository<Section> sections, ShiftGrants grants)
    : IQueryHandler<GetUnitAttendanceQuery, Result<UnitAttendanceDto>>, ICommandHandler<SetUnitAttendanceCommand, Result<UnitAttendanceDto>>
{
    public async ValueTask<Result<UnitAttendanceDto>> Handle(GetUnitAttendanceQuery query, CancellationToken ct)
    {
        if (await units.FindAsync(query.UnitId, ct) is not { Removed: false }) return UnknownUnit(query.UnitId);
        return await DtoAsync(query.UnitId, await overrides.GetByIdAsync(query.UnitId, ct), ct);
    }

    public async ValueTask<Result<UnitAttendanceDto>> Handle(SetUnitAttendanceCommand command, CancellationToken ct)
    {
        if (await units.FindAsync(command.UnitId, ct) is not { Removed: false } unit) return UnknownUnit(command.UnitId);
        var allowed = await authority.RequireManageAsync(unit.Id, ct);
        if (!allowed.IsSuccess) return allowed.As<UnitAttendanceDto>();

        var row = await overrides.GetByIdAsync(unit.Id, ct);
        var isNew = row is null;
        row ??= new UnitAttendance(unit.Id);
        row.Set(command.EarlyCheckInMinutes, command.LateMinutes, command.AbsentMinutes, command.OvertimeMinutes, command.CheckoutGraceMinutes,
            caller.Me, caller.Now);
        if (isNew) await overrides.AddAsync(row, ct);
        else await overrides.UpdateAsync(row, ct);

        await RewriteGrantsBelowAsync(unit.Path, ct);
        return await DtoAsync(unit.Id, row, ct);
    }

    private async Task RewriteGrantsBelowAsync(string path, CancellationToken ct)
    {
        foreach (var shift in await shifts.ListAsync(new PublishedNotEndedShiftsSpec(), ct))
        {
            if (shift.UnitId is not { } id || await units.FindAsync(id, ct) is not { } unit || !CachedUnit.IsSelfOrBelow(unit.Path, path)) continue;
            var section = shift.SectionId is { } sectionId ? await sections.FirstOrDefaultAsync(new SectionByIdSpec(sectionId), ct) : null;
            await grants.WriteAsync(shift, section, ct);
        }
    }

    private async Task<UnitAttendanceDto> DtoAsync(Guid unitId, UnitAttendance? row, CancellationToken ct) =>
        new(unitId, row?.EarlyCheckInMinutes, row?.LateMinutes, row?.AbsentMinutes, row?.OvertimeMinutes, row?.CheckoutGraceMinutes,
            AttendanceThresholdsDto.From(await thresholds.ForUnitAsync(unitId, ct)));

    private static Result<UnitAttendanceDto> UnknownUnit(Guid id) => Result.NotFound($"Unit {id} is not a unit this module knows.");
}
