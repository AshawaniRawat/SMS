using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    credit_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    weekly_periods = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_subjects", x => x.id);
                    table.UniqueConstraint("ak_subjects_tenant_school_year_id", x => new { x.tenant_id, x.school_id, x.academic_year_id, x.id });
                    table.CheckConstraint("ck_subjects_credits", "credit_hours IS NULL OR (credit_hours > 0 AND credit_hours <= 100)");
                    table.CheckConstraint("ck_subjects_order", "display_order >= 0");
                    table.CheckConstraint("ck_subjects_periods", "weekly_periods IS NULL OR (weekly_periods BETWEEN 1 AND 50)");
                    table.CheckConstraint("ck_subjects_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_subjects_academic_years_tenant_id_school_id_academic_year_id",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id },
                        principalTable: "academic_years",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subjects_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subjects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_subjects", x => x.id);
                    table.CheckConstraint("ck_class_subjects_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_class_subjects_academic_years_tenant_id_school_id_academic_~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id },
                        principalTable: "academic_years",
                        principalColumns: new[] { "tenant_id", "school_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_subjects_school_classes_tenant_id_school_id_academic_~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id, x.school_class_id },
                        principalTable: "school_classes",
                        principalColumns: new[] { "tenant_id", "school_id", "academic_year_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_subjects_schools_tenant_id_school_id",
                        columns: x => new { x.tenant_id, x.school_id },
                        principalTable: "schools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_subjects_subjects_tenant_id_school_id_academic_year_i~",
                        columns: x => new { x.tenant_id, x.school_id, x.academic_year_id, x.subject_id },
                        principalTable: "subjects",
                        principalColumns: new[] { "tenant_id", "school_id", "academic_year_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_academic_year_id",
                table: "class_subjects",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_school_class_id",
                table: "class_subjects",
                column: "school_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_subject_id",
                table: "class_subjects",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_tenant_id",
                table: "class_subjects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_tenant_id_school_id_academic_year_id_school_~",
                table: "class_subjects",
                columns: new[] { "tenant_id", "school_id", "academic_year_id", "school_class_id" });

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_tenant_id_school_id_academic_year_id_subject~",
                table: "class_subjects",
                columns: new[] { "tenant_id", "school_id", "academic_year_id", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "ux_class_subjects_class_subject",
                table: "class_subjects",
                columns: new[] { "school_class_id", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subjects_academic_year_id",
                table: "subjects",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_school_id",
                table: "subjects",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_school_id_academic_year_id_display_order",
                table: "subjects",
                columns: new[] { "school_id", "academic_year_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_subjects_school_id_academic_year_id_status",
                table: "subjects",
                columns: new[] { "school_id", "academic_year_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_subjects_tenant_id",
                table: "subjects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_subjects_school_year_code",
                table: "subjects",
                columns: new[] { "school_id", "academic_year_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_subjects");

            migrationBuilder.DropTable(
                name: "subjects");
        }
    }
}
