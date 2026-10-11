using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSectionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_school_classes_tenant_id_school_id_academic_year_id",
                table: "school_classes");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_school_classes_tenant_school_year_id",
                table: "school_classes",
                columns: new[] { "tenant_id", "school_id", "academic_year_id", "id" });

            migrationBuilder.CreateTable(
                name: "class_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    room_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_class_sections", x => x.id);
                    table.CheckConstraint("ck_class_sections_capacity", "capacity IS NULL OR (capacity BETWEEN 1 AND 500)");
                    table.CheckConstraint("ck_class_sections_order", "display_order >= 0");
                    table.CheckConstraint("ck_class_sections_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_class_sections_academic_years_tenant_id_school_id_academic_~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id },
                        principalTable: "academic_years",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sections_school_classes_tenant_id_school_id_academic_~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id, x.school_class_id },
                        principalTable: "school_classes",
                        principalColumns: new[] { "tenant_id", "school_id", "academic_year_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sections_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sections_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_academic_year_id",
                table: "class_sections",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_school_class_id",
                table: "class_sections",
                column: "school_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_school_class_id_display_order",
                table: "class_sections",
                columns: new[] { "school_class_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_school_class_id_status",
                table: "class_sections",
                columns: new[] { "school_class_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_school_id",
                table: "class_sections",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_tenant_id",
                table: "class_sections",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_tenant_id_school_id_academic_year_id_school_~",
                table: "class_sections",
                columns: new[] { "tenant_id", "school_id", "academic_year_id", "school_class_id" });

            migrationBuilder.CreateIndex(
                name: "ux_class_sections_class_code",
                table: "class_sections",
                columns: new[] { "school_class_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_sections");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_school_classes_tenant_school_year_id",
                table: "school_classes");

            migrationBuilder.CreateIndex(
                name: "IX_school_classes_tenant_id_school_id_academic_year_id",
                table: "school_classes",
                columns: new[] { "tenant_id", "school_id", "academic_year_id" });
        }
    }
}
