using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceRoleManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                table: "roles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "roles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "roles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "concurrency_stamp", "created_at", "created_by", "description", "display_name", "is_active", "is_system_role", "name", "normalized_name", "sort_order", "status", "tenant_id", "type", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("a1170000-0000-0000-0000-000000000001"), "PLATFORM_SUPER_ADMIN", "system-role-a1170000000000000000000000000001", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Platform Super Admin", true, true, "PLATFORM_SUPER_ADMIN", "PLATFORM_SUPER_ADMIN", 10, 1, null, 1, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000002"), "TENANT_OWNER", "system-role-a1170000000000000000000000000002", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Tenant Owner", true, true, "TENANT_OWNER", "TENANT_OWNER", 20, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000003"), "SCHOOL_ADMIN", "system-role-a1170000000000000000000000000003", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "School Admin", true, true, "SCHOOL_ADMIN", "SCHOOL_ADMIN", 30, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000004"), "PRINCIPAL", "system-role-a1170000000000000000000000000004", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Principal", true, true, "PRINCIPAL", "PRINCIPAL", 40, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000005"), "VICE_PRINCIPAL", "system-role-a1170000000000000000000000000005", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Vice Principal", true, true, "VICE_PRINCIPAL", "VICE_PRINCIPAL", 50, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000006"), "ACADEMIC_COORDINATOR", "system-role-a1170000000000000000000000000006", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Academic Coordinator", true, true, "ACADEMIC_COORDINATOR", "ACADEMIC_COORDINATOR", 60, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000007"), "ADMISSION_MANAGER", "system-role-a1170000000000000000000000000007", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Admission Manager", true, true, "ADMISSION_MANAGER", "ADMISSION_MANAGER", 70, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000008"), "RECEPTIONIST", "system-role-a1170000000000000000000000000008", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Receptionist", true, true, "RECEPTIONIST", "RECEPTIONIST", 80, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000009"), "ACCOUNTANT", "system-role-a1170000000000000000000000000009", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Accountant", true, true, "ACCOUNTANT", "ACCOUNTANT", 90, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000010"), "FINANCE_MANAGER", "system-role-a1170000000000000000000000000010", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Finance Manager", true, true, "FINANCE_MANAGER", "FINANCE_MANAGER", 100, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000011"), "HR_MANAGER", "system-role-a1170000000000000000000000000011", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "HR Manager", true, true, "HR_MANAGER", "HR_MANAGER", 110, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000012"), "TEACHER", "system-role-a1170000000000000000000000000012", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Teacher", true, true, "TEACHER", "TEACHER", 120, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000013"), "CLASS_TEACHER", "system-role-a1170000000000000000000000000013", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Class Teacher", true, true, "CLASS_TEACHER", "CLASS_TEACHER", 130, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000014"), "SUBJECT_TEACHER", "system-role-a1170000000000000000000000000014", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Subject Teacher", true, true, "SUBJECT_TEACHER", "SUBJECT_TEACHER", 140, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000015"), "EXAM_CONTROLLER", "system-role-a1170000000000000000000000000015", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Exam Controller", true, true, "EXAM_CONTROLLER", "EXAM_CONTROLLER", 150, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000016"), "LIBRARIAN", "system-role-a1170000000000000000000000000016", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Librarian", true, true, "LIBRARIAN", "LIBRARIAN", 160, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000017"), "TRANSPORT_MANAGER", "system-role-a1170000000000000000000000000017", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Transport Manager", true, true, "TRANSPORT_MANAGER", "TRANSPORT_MANAGER", 170, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000018"), "DRIVER", "system-role-a1170000000000000000000000000018", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Driver", true, true, "DRIVER", "DRIVER", 180, 1, null, 4, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000019"), "HOSTEL_WARDEN", "system-role-a1170000000000000000000000000019", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Hostel Warden", true, true, "HOSTEL_WARDEN", "HOSTEL_WARDEN", 190, 1, null, 4, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000020"), "INVENTORY_MANAGER", "system-role-a1170000000000000000000000000020", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Inventory Manager", true, true, "INVENTORY_MANAGER", "INVENTORY_MANAGER", 200, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000021"), "IT_ADMIN", "system-role-a1170000000000000000000000000021", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "IT Admin", true, true, "IT_ADMIN", "IT_ADMIN", 210, 1, null, 2, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000022"), "COUNSELLOR", "system-role-a1170000000000000000000000000022", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Counsellor", true, true, "COUNSELLOR", "COUNSELLOR", 220, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000023"), "NURSE", "system-role-a1170000000000000000000000000023", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Nurse", true, true, "NURSE", "NURSE", 230, 1, null, 4, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000024"), "SECURITY_GUARD", "system-role-a1170000000000000000000000000024", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Security Guard", true, true, "SECURITY_GUARD", "SECURITY_GUARD", 240, 1, null, 4, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000025"), "PARENT", "system-role-a1170000000000000000000000000025", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Parent", true, true, "PARENT", "PARENT", 250, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000026"), "STUDENT", "system-role-a1170000000000000000000000000026", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Student", true, true, "STUDENT", "STUDENT", 260, 1, null, 3, null, null },
                    { new Guid("a1170000-0000-0000-0000-000000000027"), "AUDITOR", "system-role-a1170000000000000000000000000027", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Auditor", true, true, "AUDITOR", "AUDITOR", 270, 1, null, 2, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_is_system_role",
                table: "roles",
                column: "is_system_role");

            migrationBuilder.CreateIndex(
                name: "ix_roles_status",
                table: "roles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                table: "roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id_status",
                table: "roles",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_roles_type",
                table: "roles",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ux_roles_global_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_roles_tenant_id_code",
                table: "roles",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_roles_scope",
                table: "roles",
                sql: "(is_system_role AND tenant_id IS NULL) OR (NOT is_system_role AND tenant_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_roles_status",
                table: "roles",
                sql: "status BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_roles_type",
                table: "roles",
                sql: "type BETWEEN 1 AND 4");

            migrationBuilder.AddForeignKey(
                name: "FK_roles_tenants_tenant_id",
                table: "roles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roles_tenants_tenant_id",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_is_system_role",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_status",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_tenant_id",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_tenant_id_status",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_type",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ux_roles_global_code",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ux_roles_tenant_id_code",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_roles_scope",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_roles_status",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_roles_type",
                table: "roles");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("a1170000-0000-0000-0000-000000000027"));

            migrationBuilder.DropColumn(
                name: "code",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "status",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "type",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
