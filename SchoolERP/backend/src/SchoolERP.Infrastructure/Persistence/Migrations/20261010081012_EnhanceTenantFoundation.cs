using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations;

public partial class EnhanceTenantFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("activated_at", "tenants", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>("suspended_at", "tenants", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>("archived_at", "tenants", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>("country_code", "tenants", type: "character varying(2)", maxLength: 2, nullable: true);
        migrationBuilder.AddColumn<string>("default_currency", "tenants", type: "character varying(3)", maxLength: 3, nullable: true);
        migrationBuilder.AddColumn<string>("email", "tenants", type: "character varying(320)", maxLength: 320, nullable: true);
        migrationBuilder.AddColumn<string>("legal_name", "tenants", type: "character varying(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>("logo_url", "tenants", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>("phone", "tenants", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>("time_zone_id", "tenants", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("website", "tenants", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<int>("status", "tenants", type: "integer", nullable: false, defaultValue: 1);

        // Preserve the previous lifecycle meaning before removing P1-03 soft-delete columns.
        migrationBuilder.Sql("""
            UPDATE tenants
            SET status = CASE WHEN is_deleted THEN 4 WHEN is_active THEN 2 ELSE 1 END,
                archived_at = CASE WHEN is_deleted THEN deleted_at ELSE NULL END;
            """);

        migrationBuilder.DropColumn("deleted_at", "tenants");
        migrationBuilder.DropColumn("deleted_by", "tenants");
        migrationBuilder.DropColumn("is_active", "tenants");
        migrationBuilder.DropColumn("is_deleted", "tenants");
        migrationBuilder.AddCheckConstraint("ck_tenants_status", "tenants", "status BETWEEN 1 AND 4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("ck_tenants_status", "tenants");
        migrationBuilder.AddColumn<DateTime>("deleted_at", "tenants", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>("deleted_by", "tenants", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<bool>("is_active", "tenants", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("is_deleted", "tenants", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.Sql("""
            UPDATE tenants
            SET is_active = status = 2,
                is_deleted = status = 4,
                deleted_at = CASE WHEN status = 4 THEN archived_at ELSE NULL END;
            """);

        migrationBuilder.DropColumn("activated_at", "tenants");
        migrationBuilder.DropColumn("archived_at", "tenants");
        migrationBuilder.DropColumn("country_code", "tenants");
        migrationBuilder.DropColumn("default_currency", "tenants");
        migrationBuilder.DropColumn("email", "tenants");
        migrationBuilder.DropColumn("legal_name", "tenants");
        migrationBuilder.DropColumn("logo_url", "tenants");
        migrationBuilder.DropColumn("phone", "tenants");
        migrationBuilder.DropColumn("status", "tenants");
        migrationBuilder.DropColumn("suspended_at", "tenants");
        migrationBuilder.DropColumn("time_zone_id", "tenants");
        migrationBuilder.DropColumn("website", "tenants");
    }
}
