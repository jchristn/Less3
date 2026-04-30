# S3Server Bugs And Improvements Found While Expanding Less3 Coverage

This file captures issues that appear to belong in `S3Server` itself or in its default protocol handling, rather than in Less3-specific business logic.

Context:

- Less3 was moved to native `AWSSDK.S3`-driven integration tests.
- The expanded suite now exercises bucket ACLs, object ACLs, version listing, multipart, raw XML/error shapes, and negative signature-validation paths.
- Some failures were fixed in Less3 directly. Those are not listed here.
- The items below are the ones that should be considered upstream `S3Server` improvements.

## 1. Signature validation can fail open when `EnableSignatures = true`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- In `RequestHandler`, signature enforcement is effectively gated by both:
  - `_Settings.EnableSignatures`
  - `Service.GetSecretKey != null`

Current behavior:

- If an embedding application sets `EnableSignatures = true` but forgets to wire `Service.GetSecretKey`, `S3Server` does not reject requests.
- It silently skips the entire signature-validation block and continues processing the request.
- This is a fail-open configuration path.

Why this matters:

- An integrator can believe signed requests are being enforced when they are not.
- The failure mode is quiet.
- Negative tests like "wrong secret key must be rejected" will unexpectedly pass unless the embedding app separately notices the missing callback.
- This is a security footgun more than a simple ergonomics issue.

How it surfaced in Less3:

- Less3 had `ValidateSignatures` enabled in settings, but had not wired `S3Server.Service.GetSecretKey`.
- As a result, intentionally bad AWS signatures were accepted until Less3 added the missing callback.

Recommended fix:

- Fail fast at startup if `EnableSignatures` is `true` and `Service.GetSecretKey` is `null`.
- If startup validation is not desirable, then fail closed on request processing instead:
  - log a clear configuration error
  - reject the request with a deterministic server/configuration failure
- At minimum, emit a prominent warning once instead of silently bypassing validation.

Recommended upstream tests:

- `EnableSignatures = true` + no `GetSecretKey` callback should not allow requests through.
- Wrong secret should produce `SignatureDoesNotMatch`.
- Unknown access key should be rejected deterministically.
- Signature V2 should be rejected when signatures are enabled.

## 2. `GET` with `Range` is framed as `200 OK` instead of `206 Partial Content`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- `RequestHandler`, `S3RequestType.ObjectReadRange`

Current behavior:

- After the callback returns an object for a range read, `S3Server` sets:
  - `StatusCode = 200`
  - `ContentType`
  - `ContentLength`
- It does not set `206 Partial Content`.
- It also does not mirror the richer header framing used in the normal object-read path.

Why this matters:

- Raw S3 clients expect successful range reads to return `206 Partial Content`.
- Returning `200` for a partial response is protocol-inaccurate and can break stricter clients, proxies, caches, or test harnesses.
- The embedding application cannot fully fix this from the callback because `S3Server` overwrites the response status after the callback returns.

Observed consequence:

- Less3 can set `Content-Range` in its callback, but `S3Server` still forces the final status code to `200`.
- That leaves the response internally inconsistent.

Recommended fix:

- Change the `ObjectReadRange` success path to send `206 Partial Content`.
- Preserve or add the standard headers expected on a partial object response:
  - `Content-Range`
  - `Accept-Ranges`
  - `ETag`
  - `Last-Modified`
  - version header when applicable
- Avoid hardcoding a success shape that the callback cannot refine.

Recommended upstream tests:

- Successful byte-range request returns `206`.
- `Content-Range` is present and matches the requested slice.
- `Accept-Ranges: bytes` is present.
- Range responses still include ETag and last-modified metadata.

## 3. Recognized but unwired operations fall through to generic `InvalidRequest`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- Switch cases in `RequestHandler`

Current behavior:

- `S3Server` correctly recognizes many request types.
- If the matching callback is `null`, execution falls through the switch.
- The request eventually lands in:
  - `_Settings.DefaultRequestHandler`, if present, or
  - a generic `InvalidRequest` response

Why this matters:

- This makes supported-but-unimplemented operations hard to diagnose.
- A feature gap looks the same as a malformed request.
- It is especially confusing for operations like:
  - bucket website configuration
  - bucket logging configuration
  - object retention
  - legal hold
  - other optional S3 APIs that are recognized at the routing layer

Why this is worth fixing upstream:

- Once `S3Server` has already parsed the request into a specific `S3RequestType`, it has more information than `InvalidRequest` communicates.
- The library should distinguish:
  - malformed/unknown request
  - recognized request type with no callback implementation

Recommended fix:

- For recognized request types with no registered callback, return a more precise failure.
- A dedicated error such as `NotImplemented`, `UnsupportedOperation`, or a configurable "feature disabled" response would be better than `InvalidRequest`.
- Log the missing callback name when this path is taken.

Recommended upstream tests:

- A recognized request type with no callback should not return the same error as an unparseable request.
- The response body and status should clearly indicate missing implementation.

## 4. Range-response protocol details are split awkwardly between callback code and core server code

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- The object read and object range response paths are inconsistent.

Current behavior:

- `ObjectRead` adds several protocol headers in `S3Server` itself.
- `ObjectReadRange` leaves important response details to the embedding application callback, but still finalizes the response in the core server.
- This creates an awkward contract:
  - the callback must know protocol details
  - but the core server still owns the final status code and body send

Why this matters:

- It is easy for embedders to produce incomplete or inconsistent range responses.
- The library already knows it is handling `ObjectReadRange`, so it should own the S3 framing of that response just as it does for standard object reads.
- Today the contract is neither fully callback-driven nor fully server-driven.

Recommended fix:

- Normalize object read and object range handling so the protocol framing is owned in one place.
- Either:
  - let the callback fully control the response, or
  - have `S3Server` consistently add the correct S3 headers/status for both full and partial reads.

## Recommended S3Server Test Additions

Even if the implementation changes above are deferred, `S3Server` should add first-class integration coverage for these cases because they are easy to regress:

- Signature validation enabled but callback missing.
- Wrong secret key on a valid access key.
- Unknown access key.
- Signature V2 request rejection.
- Successful byte-range read returns `206`.
- Range response contains `Content-Range` and `Accept-Ranges`.
- Recognized but unwired request types do not collapse into generic malformed-request errors.

