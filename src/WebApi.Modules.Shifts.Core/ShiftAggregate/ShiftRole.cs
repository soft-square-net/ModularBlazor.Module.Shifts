using System.Text.Json.Serialization;

namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>A role on the rota. Each one grants its own content permissions while the shift runs.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ShiftRole>))]
public enum ShiftRole { Manager, Desk, Editor }
