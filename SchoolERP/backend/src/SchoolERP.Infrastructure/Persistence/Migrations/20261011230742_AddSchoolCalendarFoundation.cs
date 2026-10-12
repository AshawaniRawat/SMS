using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolCalendarFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "school_calendar_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    is_working_day_override = table.Column<bool>(type: "boolean", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recurrence = table.Column<int>(type: "integer", nullable: false),
                    recurrence_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_calendar_events", x => x.id);
                    table.CheckConstraint("ck_calendar_dates", "end_date >= start_date");
                    table.CheckConstraint("ck_calendar_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_school_calendar_events_academic_years_tenant_id_school_id_a~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id },
                        principalTable: "academic_years",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_calendar_events_campuses_tenant_id_school_id_campus_~",
                        columns: x => new { x.tenant_id, x.school_id, x.campus_id },
                        principalTable: "campuses",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_calendar_events_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_calendar_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_academic_year_id",
                table: "school_calendar_events",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_campus_id",
                table: "school_calendar_events",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_school_id",
                table: "school_calendar_events",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_school_id_start_date_end_date",
                table: "school_calendar_events",
                columns: new[] { "school_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_school_id_status",
                table: "school_calendar_events",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_school_id_type",
                table: "school_calendar_events",
                columns: new[] { "school_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_tenant_id",
                table: "school_calendar_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_tenant_id_school_id_academic_year_id",
                table: "school_calendar_events",
                columns: new[] { "tenant_id", "school_id", "academic_year_id" });

            migrationBuilder.CreateIndex(
                name: "IX_school_calendar_events_tenant_id_school_id_campus_id",
                table: "school_calendar_events",
                columns: new[] { "tenant_id", "school_id", "campus_id" });

            migrationBuilder.CreateIndex(
                name: "ux_calendar_school_code",
                table: "school_calendar_events",
                columns: new[] { "school_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "school_calendar_events");
        }
    }
}
