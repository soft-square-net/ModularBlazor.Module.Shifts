using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class SectionCategoryConfiguration : IEntityTypeConfiguration<SectionCategory>
{
    public void Configure(EntityTypeBuilder<SectionCategory> category)
    {
        category.ToTable("section_categories");
        category.Property(c => c.CategoryId).HasMaxLength(DataSchemaConstants.SlugLength);
        category.HasIndex(c => c.CategoryId); // "which sections serve category X"
    }
}
