using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class RolesAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "shifts_shifts",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AddColumn<bool>(
                name: "IsLead",
                table: "shifts_shift_members",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoleKey",
                table: "shifts_shift_members",
                type: "NVARCHAR2(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "desk"); // not "": Oracle stores an empty string as NULL

            // The rota's roles become role keys (Manager, Desk, Editor are the seeded definitions manager, desk, editor);
            // the manager was the shift's lead.
            migrationBuilder.Sql(@"UPDATE ""shifts_shift_members"" SET ""RoleKey"" = LOWER(""Role""), ""IsLead"" = CASE WHEN ""Role"" = 'Manager' THEN TRUE ELSE FALSE END");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "shifts_shift_members");

            migrationBuilder.CreateTable(
                name: "shifts_shift_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TenantId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    NameEn = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Permissions = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    IsLead = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    UnitId = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_shift_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shifts_shift_templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TenantId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    UnitId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    StartLocal = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Duration = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_shift_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shifts_shift_template_members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    RoleKey = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    TemplateId = table.Column<int>(type: "NUMBER(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_shift_template_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shifts_shift_template_members_shifts_shift_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "shifts_shift_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shifts_shift_template_slots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    RoleKey = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TemplateId = table.Column<int>(type: "NUMBER(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_shift_template_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shifts_shift_template_slots_shifts_shift_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "shifts_shift_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_roles_TenantId",
                table: "shifts_shift_roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_roles_TenantId_UnitId_Key",
                table: "shifts_shift_roles",
                columns: new[] { "TenantId", "UnitId", "Key" },
                unique: true,
                filter: "\"UnitId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_template_members_TemplateId",
                table: "shifts_shift_template_members",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_template_slots_TemplateId",
                table: "shifts_shift_template_slots",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_templates_TenantId",
                table: "shifts_shift_templates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shift_templates_UnitId",
                table: "shifts_shift_templates",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "shifts_shift_members",
                type: "NVARCHAR2(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Desk"); // not "": Oracle stores an empty string as NULL

            migrationBuilder.Sql(@"UPDATE ""shifts_shift_members"" SET ""Role"" = CASE ""RoleKey"" WHEN N'manager' THEN N'Manager' WHEN N'editor' THEN N'Editor' ELSE N'Desk' END");

            migrationBuilder.Sql(@"DELETE FROM ""shifts_shifts"" WHERE ""SectionId"" IS NULL");

            migrationBuilder.DropTable(
                name: "shifts_shift_roles");

            migrationBuilder.DropTable(
                name: "shifts_shift_template_members");

            migrationBuilder.DropTable(
                name: "shifts_shift_template_slots");

            migrationBuilder.DropTable(
                name: "shifts_shift_templates");

            migrationBuilder.DropColumn(
                name: "IsLead",
                table: "shifts_shift_members");

            migrationBuilder.DropColumn(
                name: "RoleKey",
                table: "shifts_shift_members");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "shifts_shifts",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);
        }
    }
}
