using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    is_system_permission = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.CheckConstraint("ck_permissions_action_not_blank", "length(btrim(action)) > 0");
                    table.CheckConstraint("ck_permissions_code_not_blank", "length(btrim(code)) > 0");
                    table.CheckConstraint("ck_permissions_module_not_blank", "length(btrim(module)) > 0");
                    table.CheckConstraint("ck_permissions_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_permissions_scope", "scope BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_permissions_sort_order", "sort_order >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_is_active",
                table: "permissions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_module",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_module_is_active",
                table: "permissions",
                columns: new[] { "module", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_scope",
                table: "permissions",
                column: "scope");

            migrationBuilder.CreateIndex(
                name: "ux_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permissions");
        }
    }
}
