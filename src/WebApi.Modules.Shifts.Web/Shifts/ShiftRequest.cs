using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>
/// The body of <see cref="Add"/> and <see cref="Update"/>. Its name is the OpenAPI schema name: keep it stable.
/// <paramref name="UnitId"/>: the shift's Organization unit (a cached one); left out, the unit its section moved to.
/// </summary>
public sealed record ShiftRequest(int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota, Guid? UnitId = null);
