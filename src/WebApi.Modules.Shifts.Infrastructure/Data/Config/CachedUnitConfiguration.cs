using Microsoft.EntityFrameworkCore;
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
        unit.Property(u => u.Tags).HasConversion(StringListConversion.Converter, StringListConversion.Comparer).HasMaxLength(DataSchemaConstants.ListLength);
        unit.Property(u => u.Categories).HasConversion(StringListConversion.Converter, StringListConversion.Comparer).HasMaxLength(DataSchemaConstants.ListLength);
        unit.Ignore(u => u.IsDepartmentSection);
        unit.HasIndex(u => u.ParentId);
    }
}
