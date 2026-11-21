using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> section)
    {
        section.ToTable("sections");
        section.Property(s => s.Name).HasMaxLength(DataSchemaConstants.NameLength);
        section.Ignore(s => s.Scopes);
        section.HasMany(s => s.Categories).WithOne().HasForeignKey("SectionId").OnDelete(DeleteBehavior.Cascade);
        section.Navigation(s => s.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
