using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolClassManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_academic_years_tenant_school_id",
                table: "academic_years",
                columns: new[] { "tenant_id", "school_id", "id" });

            migrationBuilder.CreateTable(
                name: "school_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    education_level = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_classes", x => x.id);
                    table.CheckConstraint("ck_school_classes_order", "display_order >= 0");
                    table.CheckConstraint("ck_school_classes_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_school_classes_academic_years_tenant_id_school_id_academic_~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id },
                        principalTable: "academic_years",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_classes_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_classes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_academic_year_id",
                table: "school_classes",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_school_id",
                table: "school_classes",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_school_id_academic_year_id_display_order",
                table: "school_classes",
                columns: new[] { "school_id", "academic_year_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_school_id_academic_year_id_status",
                table: "school_classes",
                columns: new[] { "school_id", "academic_year_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_tenant_id",
                table: "school_classes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_tenant_id_school_id_academic_year_id",
                table: "school_classes",
                columns: new[] { "tenant_id", "school_id", "academic_year_id" });

            migrationBuilder.CreateIndex(
                name: "ux_school_classes_school_year_code",
                table: "school_classes",
                columns: new[] { "school_id", "academic_year_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "school_classes");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_academic_years_tenant_school_id",
                table: "academic_years");
        }
    }
}
