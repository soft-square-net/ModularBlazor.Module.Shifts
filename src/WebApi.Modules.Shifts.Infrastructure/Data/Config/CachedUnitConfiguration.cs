using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class CachedUnitConfiguration : IEntityTypeConfiguration<CachedUnit>
{
    public void Configure(EntityTypeBuilder<CachedUnit> unit)
    {
        unit.ToTable("unit_cache");
        unit.Property(u => u.Id).ValueGeneratedNever(); // Organization's unit id
        unit.Property(u => u.TypeKey).HasMaxLength(DataSchemaConstants.TypeKeyLength);
        unit.Property(u => u.Path).HasMaxLength(DataSchemaConstants.PathLength);
        unit.Property(u => u.NameEn).HasMaxLength(DataSchemaConstants.NameLength);
        unit.Property(u => u.NameAr).HasMaxLength(DataSchemaConstants.NameLength);
        unit.Property(u => u.TimeZone).HasMaxLength(DataSchemaConstants.TimeZoneLength);
        // Short lists of slugs: a JSON text column on every engine (no provider-specific array or JSON type).
        unit.Property(u => u.Tags).HasConversion(StringList.Converter, StringList.Comparer).HasMaxLength(DataSchemaConstants.ListLength);
        unit.Property(u => u.Categories).HasConversion(StringList.Converter, StringList.Comparer).HasMaxLength(DataSchemaConstants.ListLength);
        unit.Ignore(u => u.IsDepartmentSection);
        unit.HasIndex(u => u.ParentId);
    }

    private static class StringList
    {
        public static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<IReadOnlyList<string>, string> Converter = new(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        public static readonly ValueComparer<IReadOnlyList<string>> Comparer = new(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());
    }
}
