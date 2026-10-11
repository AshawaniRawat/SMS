using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_campuses_tenant_id_school_id_id",
                table: "campuses",
                columns: new[] { "tenant_id", "school_id", "id" });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    head_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.CheckConstraint("ck_departments_order", "display_order >= 0");
                    table.CheckConstraint("ck_departments_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_departments_campuses_tenant_id_school_id_campus_id",
                        columns: x => new { x.tenant_id, x.school_id, x.campus_id },
                        principalTable: "campuses",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_departments_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_departments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_campus_id",
                table: "departments",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_school_id",
                table: "departments",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_school_id_campus_id",
                table: "departments",
                columns: new[] { "school_id", "campus_id" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_school_id_display_order",
                table: "departments",
                columns: new[] { "school_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_school_id_status",
                table: "departments",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_school_id_type",
                table: "departments",
                columns: new[] { "school_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_tenant_id",
                table: "departments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_tenant_id_school_id_campus_id",
                table: "departments",
                columns: new[] { "tenant_id", "school_id", "campus_id" });

            migrationBuilder.CreateIndex(
                name: "ux_departments_school_code",
                table: "departments",
                columns: new[] { "school_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_campuses_tenant_id_school_id_id",
                table: "campuses");
        }
    }
}
