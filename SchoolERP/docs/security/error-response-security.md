# Error-response security

API responses never contain stack traces, exception/runtime type names, SQL, schema or constraint names, database hosts, file paths, credentials, cookies, authorization headers, or tokens. Unexpected exception messages remain server-side.

Cross-tenant and cross-school lookups deliberately return the same not-found response as missing resources. Correlation IDs are safe support references and connect a client response to structured server logs without revealing ownership or internals.
