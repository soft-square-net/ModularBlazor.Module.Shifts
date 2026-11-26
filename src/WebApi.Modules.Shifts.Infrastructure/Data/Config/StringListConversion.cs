using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

/// <summary>A short list of strings as a JSON text column, the same on every engine (no provider-specific array or JSON type).</summary>
internal static class StringListConversion
{
    public static readonly ValueConverter<IReadOnlyList<string>, string> Converter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

    public static readonly ValueComparer<IReadOnlyList<string>> Comparer = new(
        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v.ToList());
}
