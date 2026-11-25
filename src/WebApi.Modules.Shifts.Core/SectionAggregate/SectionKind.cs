using System.Text.Json.Serialization;

namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

/// <summary>What a section serves. Stored and sent by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SectionKind>))]
public enum SectionKind
{
    /// <summary>A desk of some categories (Sport, Politics): it serves only its <see cref="SectionCategory"/> rows.</summary>
    Category,

    /// <summary>A department (Multimedia, SEO): it serves every category and has no category rows.</summary>
    Department
}
