using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RolesAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                schema: "shifts",
                table: "shifts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsLead",
                schema: "shifts",
                table: "shift_members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoleKey",
                schema: "shifts",
                table: "shift_members",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // The rota's roles become role keys (Manager, Desk, Editor are the seeded definitions manager, desk, editor);
            // the manager was the shift's lead.
            migrationBuilder.Sql(@"UPDATE [shifts].[shift_members] SET [RoleKey] = LOWER([Role]), [IsLead] = CASE WHEN [Role] = 'Manager' THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.CreateTable(
                name: "shift_roles",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLead = table.Column<bool>(type: "bit", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shift_templates",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartLocal = table.Column<long>(type: "bigint", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shift_template_members",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_template_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shift_template_members_shift_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "shifts",
                        principalTable: "shift_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_template_slots",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_template_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shift_template_slots_shift_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "shifts",
                        principalTable: "shift_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shift_roles_TenantId",
                schema: "shifts",
                table: "shift_roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_roles_TenantId_UnitId_Key",
                schema: "shifts",
                table: "shift_roles",
                columns: new[] { "TenantId", "UnitId", "Key" },
                unique: true,
                filter: "[UnitId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shift_template_members_TemplateId",
                schema: "shifts",
                table: "shift_template_members",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_template_slots_TemplateId",
                schema: "shifts",
                table: "shift_template_slots",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_templates_TenantId",
                schema: "shifts",
                table: "shift_templates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_templates_UnitId",
                schema: "shifts",
                table: "shift_templates",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "shifts",
                table: "shift_members",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE [shifts].[shift_members] SET [Role] = CASE [RoleKey] WHEN 'manager' THEN 'Manager' WHEN 'editor' THEN 'Editor' ELSE 'Desk' END");

            migrationBuilder.Sql(@"DELETE FROM [shifts].[shifts] WHERE [SectionId] IS NULL");

            migrationBuilder.DropTable(
                name: "shift_roles",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "shift_template_members",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "shift_template_slots",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "shift_templates",
                schema: "shifts");

            migrationBuilder.DropColumn(
                name: "IsLead",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.DropColumn(
                name: "RoleKey",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                schema: "shifts",
                table: "shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
