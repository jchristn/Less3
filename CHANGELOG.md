# Change Log

## Current Version

v2.1.x

- Dependency update and changes to improve compatibility with AWS CLI
- Testing with key AWS CLI capabilities, see AWSCLI.md

## Previous Versions

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
