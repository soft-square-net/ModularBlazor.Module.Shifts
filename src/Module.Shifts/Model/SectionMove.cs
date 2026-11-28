using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>What the "Move sections" page sends and saves.</summary>
public static class SectionMove
{
    private static readonly JsonSerializerOptions Export = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Arabic names stay readable in the file
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ToJson(SectionsExport export) => JsonSerializer.Serialize(export, Export);

    /// <summary>
    /// The request for the sections still to move: those with a chosen unit are linked to it, the rest get a new unit
    /// under the Department. Null when some section has neither: the API takes no partial move without a Department.
    /// </summary>
    public static MoveSectionsRequest? Request(IReadOnlyCollection<int> unmoved, IReadOnlyDictionary<int, Guid> links, Guid? department)
    {
        var chosen = unmoved.Where(links.ContainsKey).Select(id => new SectionUnitLink { SectionId = id, UnitId = links[id] }).ToList();
        if (chosen.Count < unmoved.Count && department is null) return null;
        return new MoveSectionsRequest
        {
            ParentUnitId = chosen.Count < unmoved.Count ? department : null,
            Links = chosen.Count > 0 ? chosen : null,
        };
    }
}
