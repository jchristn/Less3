# Change Log

## Current Version

v3.0.0

- Added the v3 tenant and RBAC foundation, including tenant, role, permission, role assignment, session, authorization audit, and request context contracts
- Switched new identifier generation to PrettyID K-sortable string IDs with stable prefixes and a 32-character maximum
- Added tenant-aware schema setup and index definitions for SQLite, MySQL, PostgreSQL, and SQL Server
- Added default v3 bootstrap values: tenant `default`, user `admin@less3`, password `password`, access key `default`, and secret key `default`
- Added dashboard navigation and management pages for tenants, roles, and permissions
- Added `S3_API.md`, `REST_API.md`, and `MIGRATING_V2_TO_V3.md`
- Added shared Touchstone descriptors and CLI, xUnit, and NUnit runners for v3 coverage expansion

## Previous Versions

v2.2.0

- Updated to `S3Server v7.0.3`
- Added broad native `AWSSDK.S3` integration coverage for bucket APIs, object APIs, ACLs, tagging, versioning, multipart upload, protocol/error shapes, and signature validation
- Fixed unversioned object overwrite behavior for both standard uploads and multipart completion
- Fixed version enumeration so `ListObjectVersions` returns the full object history
- Tightened range-read handling and validation against native AWS SDK behavior
- Expanded the dashboard with object upload/view/edit workflows, row-click detail modals, centered/full-screen content viewers, standardized copy-to-clipboard controls, and request/response pretty-print tools
- Added credential selection in API Explorer, improved request validation, and aligned dashboard bucket management with admin APIs and signed S3 object requests
- Added admin statistics APIs and dashboard summary cards for total buckets, total objects, total storage, plus per-bucket object count and total size in the Buckets table
- Added admin-side user and credential edit flows backed by update endpoints, with clearer dashboard error reporting during connectivity and admin operations

v2.1.x

- Dependency update and changes to improve compatibility with AWS CLI
- Testing with key AWS CLI capabilities, see AWSCLI.md

v2.0.0

- Dependency updates, internal refactor

v1.5.0

- Breaking change; signatures no longer being validated
- Dependency updates
- Folder fixes
- Owner information included in enumeration
- Better alerts on startup about request requirements (virtual hosting vs path style URLs)

v1.4.0

- Minor refactor
- Fixes to enumeration including folder support
- Request signature authentication

v1.3.0.1

- Migrate database layer to ORM
- Improved usability and console log messages
- Simplification of objects
- Centralized authentication and authorization
- Virtualized storage layer to support new backend storage options
- Updated Postman collection
- Dockerfile for containerized deployments

v1.2.0.2

- Minor cleanup, version from assembly, dependency update, XML documentation, Postman collection

v1.2.0

- Support for bucket in hostname or bucket in URL
- Dependency update

v1.1.0
 
- Dependency update with performance improvements, better async behavior
- Better support for large objects using streams instead of memory-intensive byte arrays
- Better support for chunked transfer-encoding
- Bugfixes
 
v1.0.x

- Added bucket location API
- Changed serializer to remove pretty print for Cyberduck compatibility (S3 Java SDK compatibility)
- Added ACL APIs
- Authentication header support for both v2 and v4
- Chunked transfer support
- Initial release; please see supported APIs below.
