using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class ShiftMemberConfiguration : IEntityTypeConfiguration<ShiftMember>
{
    public void Configure(EntityTypeBuilder<ShiftMember> member)
    {
        member.ToTable("shift_members");
        member.Property(m => m.UserId).HasMaxLength(DataSchemaConstants.UserIdLength);
        member.Property(m => m.Role).HasConversion<string>().HasMaxLength(DataSchemaConstants.RoleLength);
        member.HasIndex(m => m.UserId);
    }
}
