using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class ShiftMemberConfiguration : IEntityTypeConfiguration<ShiftMember>
{
    public void Configure(EntityTypeBuilder<ShiftMember> member)
    {
        member.ToTable("shift_members");
        member.Property(m => m.UserId).HasMaxLength(DataSchemaConstants.UserIdLength);
        member.Property(m => m.RoleKey).HasMaxLength(ShiftRoleDefinition.KeyLength);
        member.Property(m => m.CheckedInBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        member.Property(m => m.CheckedOutBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        member.Ignore(m => m.IsIn);
        member.HasIndex(m => m.UserId);
    }
}
