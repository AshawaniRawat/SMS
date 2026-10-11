# School profile management

P1-25 adds a current-school profile workflow at `/settings/school`. The feature reuses the `School` aggregate; it does not create a second profile table.

## API and authorization

| Endpoint | Permission | Behavior |
|---|---|---|
| `GET /api/v1/school/profile` | `School.View` | Returns the resolved current school's profile. |
| `PUT /api/v1/school/profile` | `School.Edit` | Updates only editable profile fields. |

Neither endpoint accepts a tenant ID or school ID. A school claim is verified against the trusted current tenant. For a tenant-scoped user without a school claim, exactly one non-archived school is selected automatically; zero schools returns not found and multiple schools return `SchoolSelectionRequired` (409). This is the same resolver used by the school dashboard.

Draft, Active, and Suspended schools may be edited. Archived schools remain readable but updates return `School.ArchivedReadOnly` (409). The API authorization attributes remain authoritative; UI visibility is not a security boundary.

## Fields and validation

School code and status are returned but are read-only. Editable fields cover name/short name, registration and affiliation numbers, `SchoolBoardType` and board code, contact details, HTTP/HTTPS website, address, ISO alpha-2 country code, IANA-style timezone text, ISO alpha-3 currency code, and principal display name.

Text is trimmed by the domain aggregate. Email is lowercased; country and currency codes are uppercased. Name is required (200 characters). Optional field limits follow the database model; profile address lines are limited to 200 characters and board code to 100. Website schemes other than HTTP/HTTPS are rejected. Country and currency codes must contain only letters and have lengths two and three respectively.

Successful updates emit `SchoolProfileUpdatedDomainEvent` and standard persistence auditing records the actor/timestamps without copying contact or address values into event metadata.

## Branding

The DTO and page display the existing `LogoUrl`, with a school-initial fallback. Binary data is not stored in the School table. This repository currently exposes storage options but no functional `IFileStorage`/object-storage abstraction, so P1-25 deliberately does not add provider-specific upload/delete endpoints. Upload controls can be enabled after that shared abstraction supplies safe keys, MIME/signature validation, a configurable 2 MB limit, and atomic replacement semantics.

## Frontend

The responsive React Hook Form + Zod page lives at `/settings/school`. `School.View` exposes a read-only page; `School.Edit` enables editing and saving unless the school is archived. It includes loading, selection-required, validation, saving, success, and safe server-error states. Successful updates invalidate both `school-profile` and `school-dashboard` queries.
