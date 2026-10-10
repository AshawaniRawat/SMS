using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicYearFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    is_admission_open = table.Column<bool>(type: "boolean", nullable: false),
                    admission_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    admission_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    lock_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_years", x => x.id);
                    table.CheckConstraint("ck_academic_years_admission_dates", "admission_start_date IS NULL OR admission_end_date IS NULL OR admission_end_date >= admission_start_date");
                    table.CheckConstraint("ck_academic_years_code_not_blank", "length(btrim(code)) > 0");
                    table.CheckConstraint("ck_academic_years_dates", "end_date > start_date");
                    table.CheckConstraint("ck_academic_years_lock_date", "lock_date IS NULL OR lock_date BETWEEN start_date AND end_date");
                    table.CheckConstraint("ck_academic_years_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_academic_years_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_academic_years_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_academic_years_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_school_id_start_date",
                table: "academic_years",
                columns: new[] { "school_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_school_id_status",
                table: "academic_years",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_tenant_id",
                table: "academic_years",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_tenant_id_school_id",
                table: "academic_years",
                columns: new[] { "tenant_id", "school_id" });

            migrationBuilder.CreateIndex(
                name: "ux_academic_years_school_id_code",
                table: "academic_years",
                columns: new[] { "school_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_academic_years_school_id_current",
                table: "academic_years",
                column: "school_id",
                unique: true,
                filter: "is_current = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "academic_years");
        }
    }
}
