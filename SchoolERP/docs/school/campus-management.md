# Campus management

P1-26 manages school branches through the existing `Campus` aggregate and the React routes `/campuses`, `/campuses/new`, `/campuses/{id}`, and `/campuses/{id}/edit`.

## Scope and security

Normal campus endpoints never accept a tenant ID or school ID. `ICurrentSchoolResolver` derives the school from authenticated claims or the single non-archived school in the tenant. Multiple schools without a selected school return `SchoolSelectionRequired`. All detail and mutation lookups combine the resolved school with the campus ID, so cross-tenant and cross-school IDs return 404. A campus-scoped user sees only their campus and is denied sibling creation by resource scope as well as permission checks.

## API

| Endpoint | Permission |
|---|---|
| `GET /api/v1/campuses` and `GET /api/v1/campuses/{id}` | `Campus.View` |
| `POST /api/v1/campuses` | `Campus.Create` |
| `PUT /api/v1/campuses/{id}` and `PUT /api/v1/campuses/{id}/code` | `Campus.Edit` |
| `POST .../activate` and `POST .../reactivate` | `Campus.Activate` |
| `POST .../suspend` | `Campus.Suspend` |
| `POST .../archive` | `Campus.Archive` |
| `POST .../set-main` | `Campus.SetMain` |

The list supports `pageNumber`, `pageSize` (maximum 100), `search`, `status`, `isMain`, `city`, `sortBy`, and `sortDirection`. Sort fields are explicitly limited to name, code, status, created date, and city.

## Validation and lifecycle

Names are required and limited to 200 characters. Codes are trimmed, uppercased, limited to 50 characters, and unique per school. Contact/address lengths follow the domain foundation. Country codes are two letters and normalized uppercase. Latitude is -90 to 90 and longitude -180 to 180; both must be supplied together.

Creation is allowed under Draft or Active schools and operational tenants. The first campus is automatically main. Requesting a new main campus removes the previous designation in the same persistence unit; the database partial unique index remains the final invariant.

Activation is Draft to Active and requires an Active school. Suspension is Active to Suspended; reactivation is Suspended to Active. Archive is terminal. Archived campuses are readable but cannot be edited, recoded, set main, or transitioned. A main campus cannot be suspended or archived while another non-archived campus is available; assign that campus as main first. The only campus may be archived, leaving the school with no main campus for dashboard attention.

No hard-delete endpoint exists.
