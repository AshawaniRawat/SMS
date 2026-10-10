using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceSchoolFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "address",
                table: "schools",
                newName: "address_line1");

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "schools",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at",
                table: "schools",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "schools",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                table: "schools",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "affiliation_number",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alternate_phone",
                table: "schools",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "schools",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "board_code",
                table: "schools",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "board_type",
                table: "schools",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "schools",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_currency",
                table: "schools",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "schools",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "principal_name",
                table: "schools",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_name",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "schools",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "schools",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                table: "schools",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("UPDATE schools SET status = CASE WHEN is_deleted THEN 4 WHEN is_active THEN 2 ELSE 1 END;");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "schools");

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "schools",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_schools_tenant_id_status",
                table: "schools",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_schools_status",
                table: "schools",
                sql: "status BETWEEN 1 AND 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schools_tenant_id_status",
                table: "schools");

            migrationBuilder.DropCheckConstraint(
                name: "ck_schools_status",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "address_line2",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "affiliation_number",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "alternate_phone",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "board_code",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "board_type",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "city",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "default_currency",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "principal_name",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "short_name",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "state",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "website",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "schools");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "schools",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE schools SET is_active = status = 2;");

            migrationBuilder.DropColumn(
                name: "status",
                table: "schools");

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "schools",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "schools",
                newName: "address");
        }
    }
}
