using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> shift)
    {
        shift.ToTable("shifts");
        shift.Property(s => s.CheckedOutBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        shift.Ignore(s => s.GrantSource);
        shift.HasIndex(s => new { s.SectionId, s.StartsAt });
        shift.HasIndex(s => new { s.UnitId, s.StartsAt });
        shift.HasMany(s => s.Members).WithOne().HasForeignKey("ShiftId").OnDelete(DeleteBehavior.Cascade);
        shift.Navigation(s => s.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
