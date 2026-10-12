# API versioning

SchoolERP uses URI versioning. Current public application routes are under `/api/v1/...`, with an OpenAPI document named `v1`.

If a breaking contract is required, `/api/v2` and a separate v2 document can coexist with v1. A runtime versioning package is unnecessary for the current single-version API.
