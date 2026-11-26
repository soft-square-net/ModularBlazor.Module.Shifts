using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class OrganizationUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                schema: "shifts",
                table: "shifts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "integration_inbox",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_inbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "integration_outbox",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Envelope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "section_unit_map",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MovedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_section_unit_map", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "unit_cache",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TypeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Categories = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Removed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_cache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_UnitId_StartsAt",
                schema: "shifts",
                table: "shifts",
                columns: new[] { "UnitId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_inbox_ProcessedAt",
                schema: "shifts",
                table: "integration_inbox",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_SentAt_OccurredAt",
                schema: "shifts",
                table: "integration_outbox",
                columns: new[] { "SentAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_section_unit_map_SectionId",
                schema: "shifts",
                table: "section_unit_map",
                column: "SectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_section_unit_map_TenantId",
                schema: "shifts",
                table: "section_unit_map",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_section_unit_map_UnitId",
                schema: "shifts",
                table: "section_unit_map",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_unit_cache_ParentId",
                schema: "shifts",
                table: "unit_cache",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_unit_cache_TenantId",
                schema: "shifts",
                table: "unit_cache",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_inbox",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "integration_outbox",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "section_unit_map",
                schema: "shifts");

            migrationBuilder.DropTable(
                name: "unit_cache",
                schema: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_shifts_UnitId_StartsAt",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "UnitId",
                schema: "shifts",
                table: "shifts");
        }
    }
}
