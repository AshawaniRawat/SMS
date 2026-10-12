using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "houses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    motto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_houses", x => x.id);
                    table.CheckConstraint("ck_houses_color", "color_hex ~ '^#[0-9A-F]{6}$'");
                    table.CheckConstraint("ck_houses_order", "display_order >= 0");
                    table.CheckConstraint("ck_houses_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_houses_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_houses_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_houses_school_id",
                table: "houses",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_houses_school_id_display_order",
                table: "houses",
                columns: new[] { "school_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_houses_school_id_name",
                table: "houses",
                columns: new[] { "school_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_houses_school_id_status",
                table: "houses",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_houses_tenant_id",
                table: "houses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_houses_tenant_id_school_id",
                table: "houses",
                columns: new[] { "tenant_id", "school_id" });

            migrationBuilder.CreateIndex(
                name: "ux_houses_school_code",
                table: "houses",
                columns: new[] { "school_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "houses");
        }
    }
}
