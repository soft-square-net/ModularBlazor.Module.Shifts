using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class SectionUnitMapConfiguration : IEntityTypeConfiguration<SectionUnitMap>
{
    public void Configure(EntityTypeBuilder<SectionUnitMap> map)
    {
        map.ToTable("section_unit_map");
        map.Property(m => m.MovedBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        map.HasIndex(m => m.SectionId).IsUnique(); // a section moves once
        map.HasIndex(m => m.UnitId);
    }
}
