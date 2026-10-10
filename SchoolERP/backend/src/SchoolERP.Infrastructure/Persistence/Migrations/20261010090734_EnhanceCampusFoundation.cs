using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceCampusFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_campuses_school_id",
                table: "campuses");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "campuses",
                newName: "address_line1");

            migrationBuilder.AlterColumn<string>(
                name: "address_line1", table: "campuses", type: "character varying(250)", maxLength: 250,
                nullable: true, oldClrType: typeof(string), oldType: "character varying(1000)", oldMaxLength: 1000, oldNullable: true);

            migrationBuilder.RenameIndex(
                name: "IX_campuses_tenant_id_school_id",
                table: "campuses",
                newName: "ix_campuses_tenant_id_school_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at",
                table: "campuses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_main_campus",
                table: "campuses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                table: "campuses",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alternate_phone",
                table: "campuses",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "campuses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "campuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "campuses",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "campuses",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "campuses",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "campuses",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "campuses",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "campuses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_name",
                table: "campuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "campuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "campuses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "campuses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                table: "campuses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("UPDATE campuses SET status = CASE WHEN is_deleted THEN 4 WHEN is_active THEN 2 ELSE 1 END;");

            migrationBuilder.DropColumn(name: "is_active", table: "campuses");

            migrationBuilder.CreateIndex(
                name: "ux_campuses_school_id_main",
                table: "campuses",
                column: "school_id",
                unique: true,
                filter: "is_main_campus = true AND is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campuses_latitude",
                table: "campuses",
                sql: "latitude IS NULL OR latitude BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campuses_longitude",
                table: "campuses",
                sql: "longitude IS NULL OR longitude BETWEEN -180 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campuses_status",
                table: "campuses",
                sql: "status BETWEEN 1 AND 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_campuses_school_id_main",
                table: "campuses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campuses_latitude",
                table: "campuses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campuses_longitude",
                table: "campuses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campuses_status",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "address_line2",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "alternate_phone",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "city",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "email",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "short_name",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "state",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "campuses");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                table: "campuses");

            migrationBuilder.AddColumn<bool>(name: "is_active", table: "campuses", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.Sql("UPDATE campuses SET is_active = status = 2;");
            migrationBuilder.DropColumn(name: "status", table: "campuses");
            migrationBuilder.DropColumn(name: "is_main_campus", table: "campuses");

            migrationBuilder.RenameIndex(
                name: "ix_campuses_tenant_id_school_id",
                table: "campuses",
                newName: "IX_campuses_tenant_id_school_id");

            migrationBuilder.AlterColumn<string>(name: "address_line1", table: "campuses", type: "character varying(1000)",
                maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "character varying(250)", oldMaxLength: 250, oldNullable: true);
            migrationBuilder.RenameColumn(name: "address_line1", table: "campuses", newName: "address");

            migrationBuilder.CreateIndex(
                name: "ix_campuses_school_id",
                table: "campuses",
                column: "school_id");
        }
    }
}
