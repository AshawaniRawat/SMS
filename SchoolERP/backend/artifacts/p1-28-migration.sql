START TRANSACTION;
ALTER TABLE academic_years ADD CONSTRAINT ak_academic_years_tenant_school_id UNIQUE (tenant_id, school_id, id);

CREATE TABLE school_classes (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    school_id uuid NOT NULL,
    academic_year_id uuid NOT NULL,
    name character varying(150) NOT NULL,
    code character varying(50) NOT NULL,
    short_name character varying(50),
    display_order integer NOT NULL,
    education_level integer,
    status integer NOT NULL,
    description character varying(500),
    activated_at timestamp with time zone,
    deactivated_at timestamp with time zone,
    archived_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone,
    created_by uuid,
    updated_by uuid,
    CONSTRAINT "PK_school_classes" PRIMARY KEY (id),
    CONSTRAINT ck_school_classes_order CHECK (display_order >= 0),
    CONSTRAINT ck_school_classes_status CHECK (status BETWEEN 1 AND 4),
    CONSTRAINT "FK_school_classes_academic_years_tenant_id_school_id_academic_~" FOREIGN KEY (tenant_id, school_id, academic_year_id) REFERENCES academic_years (tenant_id, school_id, id) ON DELETE RESTRICT,
    CONSTRAINT "FK_school_classes_schools_tenant_id_school_id" FOREIGN KEY (tenant_id, school_id) REFERENCES schools (tenant_id, id) ON DELETE RESTRICT,
    CONSTRAINT "FK_school_classes_tenants_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_school_classes_academic_year_id" ON school_classes (academic_year_id);

CREATE INDEX "IX_school_classes_school_id" ON school_classes (school_id);

CREATE INDEX "IX_school_classes_school_id_academic_year_id_display_order" ON school_classes (school_id, academic_year_id, display_order);

CREATE INDEX "IX_school_classes_school_id_academic_year_id_status" ON school_classes (school_id, academic_year_id, status);

CREATE INDEX "IX_school_classes_tenant_id" ON school_classes (tenant_id);

CREATE INDEX "IX_school_classes_tenant_id_school_id_academic_year_id" ON school_classes (tenant_id, school_id, academic_year_id);

CREATE UNIQUE INDEX ux_school_classes_school_year_code ON school_classes (school_id, academic_year_id, code);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261011164809_AddSchoolClassManagement', '9.0.12');

COMMIT;

