# API authentication

Call `POST /api/v1/auth/login`, then send the returned access token as `Authorization: Bearer <access-token>`. In Swagger UI, select **Authorize** and paste only the access token; the UI adds the prefix.

Use `/api/v1/auth/refresh` when access expires. Never enter a refresh token into the Bearer dialog. Public authentication operations are anonymous in OpenAPI. Protected operations show Bearer and their `x-required-permissions`; a token never bypasses server-side permission mappings.

Document examples are nonfunctional placeholders, not production credentials or signed tokens.
