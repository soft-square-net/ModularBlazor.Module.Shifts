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
        shift.Property(s => s.HandoverNote).HasMaxLength(Shift.HandoverNoteLength);
        shift.Property(s => s.EndedBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        shift.HasIndex(s => s.EndedAt);
        shift.Property(s => s.HandoverBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        shift.Ignore(s => s.GrantSource);
        shift.Ignore(s => s.Ended);
        shift.Ignore(s => s.Rota);
        // The clock's query: published shifts that have not ended, by start.
        shift.HasIndex(s => new { s.Published, s.CheckedOutAt, s.StartsAt });
        shift.HasIndex(s => new { s.SectionId, s.StartsAt });
        shift.HasIndex(s => new { s.UnitId, s.StartsAt });
        shift.HasMany(s => s.Members).WithOne().HasForeignKey("ShiftId").OnDelete(DeleteBehavior.Cascade);
        shift.Navigation(s => s.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
