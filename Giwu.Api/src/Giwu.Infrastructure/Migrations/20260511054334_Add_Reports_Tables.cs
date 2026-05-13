using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giwu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Reports_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compliance_deadlines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Agency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FormCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodCovered = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsFiled = table.Column<bool>(type: "boolean", nullable: false),
                    FiledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RelatedReportCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_deadlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LongDescription = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    RequiresDateRange = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresDepartmentFilter = table.Column<bool>(type: "boolean", nullable: false),
                    SupportedFormatsCsv = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ColumnsCsv = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RanByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RanByDisplay = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DepartmentsCsv = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    Cadence = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientsCsv = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_deadlines_DueDate",
                table: "compliance_deadlines",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_deadlines_IsFiled",
                table: "compliance_deadlines",
                column: "IsFiled");

            migrationBuilder.CreateIndex(
                name: "IX_report_definitions_Code",
                table: "report_definitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_runs_DefinitionId",
                table: "report_runs",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_report_runs_QueuedAt",
                table: "report_runs",
                column: "QueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_report_runs_Status",
                table: "report_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_report_schedules_DefinitionId",
                table: "report_schedules",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_report_schedules_IsActive",
                table: "report_schedules",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_deadlines");

            migrationBuilder.DropTable(
                name: "report_definitions");

            migrationBuilder.DropTable(
                name: "report_runs");

            migrationBuilder.DropTable(
                name: "report_schedules");
        }
    }
}
