# Less3 S3 API

Less3 v3.0.0 keeps S3 compatibility as the data-plane API. Requests authenticate with S3 access keys, and the access key resolves the tenant. Access keys are globally unique, so S3 clients do not send a tenant header.

## Endpoint

The S3 endpoint is the Less3 server root:

```text
http://localhost:8000
```

The Docker default exposes the same endpoint on port `8000`.

## Default Development Credential

Fresh v3 deployments seed a default tenant and credential for local use:

```text
Tenant ID: default
Access Key: default
Secret Key: default
```

Change or remove this credential before exposing a node outside a trusted development environment.

## Tenant Resolution

S3 requests are tenant-scoped by credential lookup:

1. Less3 extracts the access key from the S3 authorization material.
2. Less3 loads the credential by globally unique access key.
3. The credential identifies its tenant and owning user.
4. The tenant, user, and credential must all be active.
5. Bucket, object, tag, ACL, multipart, and version operations execute only inside that tenant.

Bucket names are unique per tenant. Two tenants may each own a bucket named `photos`, but one tenant cannot create two buckets with the same name.

## Supported Operation Families

Less3 v3.0.0 is expected to document and test these S3 operation families:

- Service operations: list buckets.
- Bucket operations: create, delete, exists, list objects, list versions, read/write versioning, read/write tags, read/write ACLs.
- Object operations: put, get, head, delete, delete many, copy, read/write tags, read/write ACLs.
- Multipart operations: create upload, upload part, complete upload, abort upload, list uploads, list upload parts.
- Versioning operations: retrieve specific versions, list versions, show delete markers, restore or copy a version through dashboard workflows.

## Identifier Policy

Less3 v3 uses PrettyID string identifiers internally and in Less3-owned APIs. S3 protocol fields that require bucket names, object keys, ETags, upload IDs, and version IDs keep their S3 meanings. Less3-owned identifiers are not Ids.

## OpenAPI

Less3 exposes one combined OpenAPI document for S3, Less3 REST, and administrative APIs:

```text
GET /openapi.json
```

The dashboard API Explorer consumes this document.
