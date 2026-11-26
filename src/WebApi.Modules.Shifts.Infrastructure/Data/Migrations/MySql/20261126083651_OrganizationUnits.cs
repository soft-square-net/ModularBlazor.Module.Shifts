using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.MySql
{
    /// <inheritdoc />
    public partial class OrganizationUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "shifts_shifts",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shifts_integration_inbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Type = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_integration_inbox", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shifts_integration_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Type = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Envelope = table.Column<string>(type: "longtext", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_integration_outbox", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shifts_section_unit_map",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<Guid>(type: "char(36)", nullable: false),
                    MovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MovedBy = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_section_unit_map", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shifts_unit_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenantId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    OrgId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true),
                    TypeKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    NameEn = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Tags = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    TimeZone = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    Categories = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Removed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_unit_cache", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shifts_UnitId_StartsAt",
                table: "shifts_shifts",
                columns: new[] { "UnitId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_integration_inbox_ProcessedAt",
                table: "shifts_integration_inbox",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_integration_outbox_SentAt_OccurredAt",
                table: "shifts_integration_outbox",
                columns: new[] { "SentAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_section_unit_map_SectionId",
                table: "shifts_section_unit_map",
                column: "SectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shifts_section_unit_map_TenantId",
                table: "shifts_section_unit_map",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_section_unit_map_UnitId",
                table: "shifts_section_unit_map",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_unit_cache_ParentId",
                table: "shifts_unit_cache",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_unit_cache_TenantId",
                table: "shifts_unit_cache",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shifts_integration_inbox");

            migrationBuilder.DropTable(
                name: "shifts_integration_outbox");

            migrationBuilder.DropTable(
                name: "shifts_section_unit_map");

            migrationBuilder.DropTable(
                name: "shifts_unit_cache");

            migrationBuilder.DropIndex(
                name: "IX_shifts_shifts_UnitId_StartsAt",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "shifts_shifts");
        }
    }
}
