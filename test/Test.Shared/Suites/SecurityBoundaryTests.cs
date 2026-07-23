namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Security boundary tests for tenant isolation and RBAC enforcement across S3 and REST APIs.
    /// </summary>
    public class SecurityBoundaryTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Security Boundary Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBoundaryTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public SecurityBoundaryTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all security boundary tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await RunTest(
                "SecurityBoundary_S3_TenantCredentialsOnlySeeOwnBucketsAndObjects",
                () => SecurityBoundaryTestCases.S3TenantCredentialsOnlySeeOwnBucketsAndObjectsAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_S3_NoRoleCredentialDeniedServiceBucketAndObjectAccess",
                () => SecurityBoundaryTestCases.S3NoRoleCredentialDeniedServiceBucketAndObjectAccessAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_S3_ObjectScopedRbacPermitsOnlyAssignedObject",
                () => SecurityBoundaryTestCases.S3ObjectScopedRbacPermitsOnlyAssignedObjectAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_NoRoleAndTenantMemberRbacBoundaries",
                () => SecurityBoundaryTestCases.RestNoRoleAndTenantMemberRbacBoundariesAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_ResourceScopedRbacPermitsOnlyAssignedResource",
                () => SecurityBoundaryTestCases.RestResourceScopedRbacPermitsOnlyAssignedResourceAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_TenantIdQuerySpoofingDenied",
                () => SecurityBoundaryTestCases.RestTenantIdQuerySpoofingDeniedAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_TenantIdBodySpoofingDenied",
                () => SecurityBoundaryTestCases.RestTenantIdBodySpoofingDeniedAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_AdminApiKeyCanManageAcrossTenants",
                () => SecurityBoundaryTestCases.RestAdminApiKeyCanManageAcrossTenantsAsync(_Server, CancellationToken.None)).ConfigureAwait(false);
        }

        #endregion
    }

    /// <summary>
    /// Reusable security boundary cases shared by suite runners and Touchstone descriptors.
    /// </summary>
    internal static class SecurityBoundaryTestCases
    {
        #region Public-Methods

        internal static async Task S3TenantCredentialsOnlySeeOwnBucketsAndObjectsAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantA = await CreateTenantPrincipalAsync(server, "s3a", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantB = await CreateTenantPrincipalAsync(server, "s3b", true, cancellationToken).ConfigureAwait(false);

            string sharedBucket = "shared-" + TestIds.Suffix().Substring(0, 8);
            string tenantAOnlyBucket = "ta-only-" + TestIds.Suffix().Substring(0, 8);
            string tenantAKey = "tenant-a-only.txt";
            string tenantBKey = "tenant-b-only.txt";

            using IAmazonS3 clientA = server.CreateS3Client(tenantA.AccessKey, tenantA.SecretKey);
            using IAmazonS3 clientB = server.CreateS3Client(tenantB.AccessKey, tenantB.SecretKey);

            await PutBucketAsync(clientA, sharedBucket, cancellationToken).ConfigureAwait(false);
            await PutBucketAsync(clientB, sharedBucket, cancellationToken).ConfigureAwait(false);
            await PutBucketAsync(clientA, tenantAOnlyBucket, cancellationToken).ConfigureAwait(false);

            await PutTextObjectAsync(clientA, sharedBucket, tenantAKey, "tenant-a-secret", cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(clientB, sharedBucket, tenantBKey, "tenant-b-secret", cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(clientA, tenantAOnlyBucket, tenantAKey, "tenant-a-unique-secret", cancellationToken).ConfigureAwait(false);

            ListBucketsResponse bucketsA = await clientA.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, bucketsA.HttpStatusCode, "tenant A list buckets");
            EnsureEqual(1, CountBucket(bucketsA, sharedBucket), "tenant A shared bucket count");
            EnsureEqual(1, CountBucket(bucketsA, tenantAOnlyBucket), "tenant A unique bucket count");

            ListBucketsResponse bucketsB = await clientB.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, bucketsB.HttpStatusCode, "tenant B list buckets");
            EnsureEqual(1, CountBucket(bucketsB, sharedBucket), "tenant B shared bucket count");
            EnsureEqual(0, CountBucket(bucketsB, tenantAOnlyBucket), "tenant B must not see tenant A unique bucket");

            EnsureEqual(
                "tenant-a-secret",
                await ReadObjectBodyAsync(clientA, sharedBucket, tenantAKey, cancellationToken).ConfigureAwait(false),
                "tenant A reads tenant A object");
            EnsureEqual(
                "tenant-b-secret",
                await ReadObjectBodyAsync(clientB, sharedBucket, tenantBKey, cancellationToken).ConfigureAwait(false),
                "tenant B reads tenant B object");

            await EnsureS3FailureAsync(
                () => clientB.GetObjectAsync(sharedBucket, tenantAKey, cancellationToken),
                "tenant B read of tenant A key in same bucket name").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => clientA.GetObjectAsync(sharedBucket, tenantBKey, cancellationToken),
                "tenant A read of tenant B key in same bucket name").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => clientB.GetObjectAsync(tenantAOnlyBucket, tenantAKey, cancellationToken),
                "tenant B read of tenant A unique bucket").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => clientB.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = tenantAOnlyBucket,
                    Key = "overwrite.txt",
                    ContentBody = "blocked"
                }, cancellationToken),
                "tenant B write to tenant A unique bucket").ConfigureAwait(false);
        }

        internal static async Task S3NoRoleCredentialDeniedServiceBucketAndObjectAccessAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "s3deny-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal noRole = await CreateTenantPrincipalAsync(server, "s3deny-norole", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            string bucket = "deny-" + TestIds.Suffix().Substring(0, 8);
            string key = "private.txt";

            using IAmazonS3 adminClient = server.CreateS3Client(admin.AccessKey, admin.SecretKey);
            using IAmazonS3 deniedClient = server.CreateS3Client(noRole.AccessKey, noRole.SecretKey);

            await PutBucketAsync(adminClient, bucket, cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(adminClient, bucket, key, "private", cancellationToken).ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => deniedClient.ListBucketsAsync(cancellationToken),
                "no-role ListBuckets").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => deniedClient.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = "norole-" + TestIds.Suffix().Substring(0, 8)
                }, cancellationToken),
                "no-role CreateBucket").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => deniedClient.GetObjectAsync(bucket, key, cancellationToken),
                "no-role GetObject").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => deniedClient.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = "blocked.txt",
                    ContentBody = "blocked"
                }, cancellationToken),
                "no-role PutObject").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => deniedClient.DeleteObjectAsync(bucket, key, cancellationToken),
                "no-role DeleteObject").ConfigureAwait(false);
        }

        internal static async Task S3ObjectScopedRbacPermitsOnlyAssignedObjectAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "s3obj-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal scoped = await CreateTenantPrincipalAsync(server, "s3obj-scoped", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            string bucket = "objscope-" + TestIds.Suffix().Substring(0, 8);
            string allowedKey = "allowed.txt";
            string deniedKey = "denied.txt";

            using IAmazonS3 adminClient = server.CreateS3Client(admin.AccessKey, admin.SecretKey);
            using IAmazonS3 scopedClient = server.CreateS3Client(scoped.AccessKey, scoped.SecretKey);

            await PutBucketAsync(adminClient, bucket, cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(adminClient, bucket, allowedKey, "allowed-object", cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(adminClient, bucket, deniedKey, "denied-object", cancellationToken).ConfigureAwait(false);

            string bucketId = await ReadBucketIdByNameAsync(server, admin.TenantId, bucket, cancellationToken).ConfigureAwait(false);
            string allowedObjectId = await ReadObjectIdByKeyAsync(server, admin.TenantId, bucketId, allowedKey, cancellationToken).ConfigureAwait(false);

            await GrantCustomRoleAsync(
                server,
                admin.TenantId,
                scoped.CredentialId,
                "Credential",
                "Object",
                "Read",
                true,
                "Object",
                allowedObjectId,
                cancellationToken).ConfigureAwait(false);

            EnsureEqual(
                "allowed-object",
                await ReadObjectBodyAsync(scopedClient, bucket, allowedKey, cancellationToken).ConfigureAwait(false),
                "object-scoped credential reads assigned object");

            await EnsureS3FailureAsync(
                () => scopedClient.GetObjectAsync(bucket, deniedKey, cancellationToken),
                "object-scoped credential read of unassigned object").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => scopedClient.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucket
                }, cancellationToken),
                "object-scoped credential bucket enumeration").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => scopedClient.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = allowedKey,
                    ContentBody = "blocked update"
                }, cancellationToken),
                "read-only object-scoped credential object update").ConfigureAwait(false);
        }

        internal static async Task RestNoRoleAndTenantMemberRbacBoundariesAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal noRole = await CreateTenantPrincipalAsync(server, "rest-norole", false, cancellationToken).ConfigureAwait(false);
            string noRoleToken = await LoginAsync(server, noRole.TenantId, noRole.Email, "password", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage noRoleTenantRead = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/" + noRole.TenantId, noRoleToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, noRoleTenantRead.StatusCode, "no-role REST tenant read");

            HttpResponseMessage noRoleUserCreate = await SendBearerRestAsync(server, HttpMethod.Post, "users", noRoleToken, JsonSerializer.Serialize(new
            {
                Id = TestIds.User(),
                TenantId = noRole.TenantId,
                Name = "Blocked no-role user",
                Email = "blocked-" + TestIds.Suffix() + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, noRoleUserCreate.StatusCode, "no-role REST user create");

            await GrantBuiltInRoleAsync(
                server,
                noRole.TenantId,
                "rol_builtin_tenantmember",
                "User",
                noRole.UserId,
                "Tenant",
                noRole.TenantId,
                cancellationToken).ConfigureAwait(false);

            HttpResponseMessage memberTenantRead = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/" + noRole.TenantId, noRoleToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, memberTenantRead.StatusCode, "tenant member REST tenant read");

            HttpResponseMessage memberUserCreate = await SendBearerRestAsync(server, HttpMethod.Post, "users", noRoleToken, JsonSerializer.Serialize(new
            {
                Id = TestIds.User(),
                TenantId = noRole.TenantId,
                Name = "Blocked tenant member user",
                Email = "blocked-member-" + TestIds.Suffix() + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, memberUserCreate.StatusCode, "tenant member REST user create denied");
        }

        internal static async Task RestResourceScopedRbacPermitsOnlyAssignedResourceAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "rest-scope-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal scoped = await CreateTenantPrincipalAsync(server, "rest-scope-user", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            string targetUserId = TestIds.User();
            string deniedUserId = TestIds.User();

            await CreateUserAsync(server, admin.TenantId, targetUserId, "target-" + targetUserId + "@example.com", cancellationToken).ConfigureAwait(false);
            await CreateUserAsync(server, admin.TenantId, deniedUserId, "denied-" + deniedUserId + "@example.com", cancellationToken).ConfigureAwait(false);

            await GrantCustomRoleAsync(
                server,
                admin.TenantId,
                scoped.UserId,
                "User",
                "User",
                "Read",
                true,
                "User",
                targetUserId,
                cancellationToken).ConfigureAwait(false);

            string scopedToken = await LoginAsync(server, admin.TenantId, scoped.Email, "password", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage targetRead = await SendBearerRestAsync(server, HttpMethod.Get, "users/" + targetUserId, scopedToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, targetRead.StatusCode, "resource-scoped REST user read");
            EnsureContains(await targetRead.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), targetUserId, "resource-scoped REST read body");

            HttpResponseMessage deniedRead = await SendBearerRestAsync(server, HttpMethod.Get, "users/" + deniedUserId, scopedToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, deniedRead.StatusCode, "resource-scoped REST other user read denied");

            HttpResponseMessage enumerate = await SendBearerRestAsync(server, HttpMethod.Get, "users", scopedToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, enumerate.StatusCode, "resource-scoped REST collection enumerate denied");
        }

        internal static async Task RestTenantIdQuerySpoofingDeniedAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantA = await CreateTenantPrincipalAsync(server, "rest-query-a", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantB = await CreateTenantPrincipalAsync(server, "rest-query-b", true, cancellationToken).ConfigureAwait(false);

            string tokenA = await LoginAsync(server, tenantA.TenantId, tenantA.Email, "password", cancellationToken).ConfigureAwait(false);
            string tokenB = await LoginAsync(server, tenantB.TenantId, tenantB.Email, "password", cancellationToken).ConfigureAwait(false);

            string tenantBBucketName = "rest-b-" + TestIds.Suffix().Substring(0, 8);
            string tenantBObjectKey = "tenant-b-rest-object.txt";
            using (IAmazonS3 tenantBClient = server.CreateS3Client(tenantB.AccessKey, tenantB.SecretKey))
            {
                await PutBucketAsync(tenantBClient, tenantBBucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(tenantBClient, tenantBBucketName, tenantBObjectKey, "tenant-b-rest-secret", cancellationToken).ConfigureAwait(false);
            }

            string tenantBBucketId = await ReadBucketIdByNameAsync(server, tenantB.TenantId, tenantBBucketName, cancellationToken).ConfigureAwait(false);
            string tenantBObjectId = await ReadObjectIdByKeyAsync(server, tenantB.TenantId, tenantBBucketId, tenantBObjectKey, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage sameTenantBucketRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "buckets/" + tenantBBucketId,
                tokenB,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, sameTenantBucketRead.StatusCode, "tenant B token read tenant B bucket through REST");

            HttpResponseMessage sameTenantObjectRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "objects/" + tenantBObjectId + "?bucketId=" + tenantBBucketId,
                tokenB,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, sameTenantObjectRead.StatusCode, "tenant B token read tenant B object metadata through REST");

            HttpResponseMessage crossTenantRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "tenants/" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, crossTenantRead.StatusCode, "tenant A token read tenant B tenant record");

            HttpResponseMessage querySpoofUserRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "users/" + tenantB.UserId + "?tenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofUserRead.StatusCode, "tenant A token read tenant B user through tenantId query");

            HttpResponseMessage querySpoofUserReadCased = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "users/" + tenantB.UserId + "?TenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofUserReadCased.StatusCode, "tenant A token read tenant B user through cased TenantId query");

            HttpResponseMessage querySpoofUserEnumerate = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "users?tenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofUserEnumerate.StatusCode, "tenant A token enumerate tenant B users through tenantId query");

            HttpResponseMessage querySpoofBucketRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "buckets/" + tenantBBucketId + "?tenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofBucketRead.StatusCode, "tenant A token read tenant B bucket through tenantId query");

            HttpResponseMessage querySpoofBucketEnumerate = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "buckets?tenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofBucketEnumerate.StatusCode, "tenant A token enumerate tenant B buckets through tenantId query");

            HttpResponseMessage querySpoofObjectRead = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "objects/" + tenantBObjectId + "?tenantId=" + tenantB.TenantId + "&bucketId=" + tenantBBucketId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofObjectRead.StatusCode, "tenant A token read tenant B object through tenantId query");

            HttpResponseMessage querySpoofExists = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "users/" + tenantB.UserId + "/exists?tenantId=" + tenantB.TenantId,
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, querySpoofExists.StatusCode, "tenant A token exists tenant B user through tenantId query");

            HttpResponseMessage tenantCollection = await SendBearerRestAsync(
                server,
                HttpMethod.Get,
                "tenants",
                tokenA,
                null,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, tenantCollection.StatusCode, "tenant A token enumerate tenants collection");

            HttpResponseMessage tenantPostEnumerate = await SendBearerRestAsync(
                server,
                HttpMethod.Post,
                "tenants/enumerate",
                tokenA,
                "{}",
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, tenantPostEnumerate.StatusCode, "tenant A token POST enumerate tenants collection");
        }

        internal static async Task RestTenantIdBodySpoofingDeniedAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantA = await CreateTenantPrincipalAsync(server, "rest-body-a", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantB = await CreateTenantPrincipalAsync(server, "rest-body-b", true, cancellationToken).ConfigureAwait(false);

            string tokenA = await LoginAsync(server, tenantA.TenantId, tenantA.Email, "password", cancellationToken).ConfigureAwait(false);
            string spoofedUserId = TestIds.User();
            string spoofedTenantId = TestIds.Tenant();

            HttpResponseMessage tenantCreate = await SendBearerRestAsync(server, HttpMethod.Post, "tenants", tokenA, JsonSerializer.Serialize(new
            {
                Id = spoofedTenantId,
                Name = "Body spoofed tenant",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, tenantCreate.StatusCode, "tenant A token create another tenant through body Id");

            HttpResponseMessage bodySpoofCreate = await SendBearerRestAsync(server, HttpMethod.Post, "users", tokenA, JsonSerializer.Serialize(new
            {
                Id = spoofedUserId,
                TenantId = tenantB.TenantId,
                Name = "Body spoofed user",
                Email = "body-spoof-" + TestIds.Suffix() + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, bodySpoofCreate.StatusCode, "tenant A token create tenant B user through body TenantId");

            HttpResponseMessage mixedSpoofCreate = await SendBearerRestAsync(server, HttpMethod.Post, "users?tenantId=" + tenantA.TenantId, tokenA, JsonSerializer.Serialize(new
            {
                Id = TestIds.User(),
                TenantId = tenantB.TenantId,
                Name = "Mixed spoofed user",
                Email = "mixed-spoof-" + TestIds.Suffix() + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, mixedSpoofCreate.StatusCode, "tenant A token create tenant B user through body TenantId despite own query TenantId");

            HttpResponseMessage verifyNotCreated = await server.RestGetAsync("users/" + spoofedUserId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyNotCreated.StatusCode, "body-spoofed user must not exist in tenant B");

            HttpResponseMessage verifyTenantNotCreated = await server.RestGetAsync("tenants/" + spoofedTenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyTenantNotCreated.StatusCode, "body-spoofed tenant must not exist");

            HttpResponseMessage bodySpoofEnumerate = await SendBearerRestAsync(server, HttpMethod.Post, "users/enumerate", tokenA, JsonSerializer.Serialize(new
            {
                TenantId = tenantB.TenantId,
                Limit = 100,
                Offset = 0
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, bodySpoofEnumerate.StatusCode, "tenant A token enumerate tenant B users through body TenantId");
        }

        internal static async Task RestAdminApiKeyCanManageAcrossTenantsAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantA = await CreateTenantPrincipalAsync(server, "rest-admin-a", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantB = await CreateTenantPrincipalAsync(server, "rest-admin-b", true, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage tenantAUser = await server.RestGetAsync("users/" + tenantA.UserId + "?tenantId=" + tenantA.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, tenantAUser.StatusCode, "admin API key read tenant A user");
            EnsureContains(await tenantAUser.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), tenantA.UserId, "admin API key tenant A user body");

            HttpResponseMessage tenantBUser = await server.RestGetAsync("users/" + tenantB.UserId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, tenantBUser.StatusCode, "admin API key read tenant B user");
            EnsureContains(await tenantBUser.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), tenantB.UserId, "admin API key tenant B user body");

            HttpResponseMessage tenants = await server.RestGetAsync("tenants", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, tenants.StatusCode, "admin API key enumerate tenants");
            string tenantsBody = await tenants.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(tenantsBody, tenantA.TenantId, "admin API key tenant A enumerate body");
            EnsureContains(tenantsBody, tenantB.TenantId, "admin API key tenant B enumerate body");
        }

        #endregion

        #region Private-Methods

        private static async Task<TenantPrincipal> CreateTenantPrincipalAsync(
            Less3TestServer server,
            string prefix,
            bool tenantAdmin,
            CancellationToken cancellationToken,
            string? tenantId = null)
        {
            bool createTenant = String.IsNullOrEmpty(tenantId);
            tenantId ??= TestIds.Tenant();

            TenantPrincipal principal = new TenantPrincipal
            {
                TenantId = tenantId,
                UserId = TestIds.User(),
                CredentialId = TestIds.Credential(),
                AccessKey = prefix + "-" + TestIds.Suffix(),
                SecretKey = "secret-" + TestIds.Suffix(),
            };
            principal.Email = principal.UserId + "@example.com";

            if (createTenant)
            {
                HttpResponseMessage tenantResponse = await server.RestPostAsync("tenants", JsonSerializer.Serialize(new
                {
                    Id = principal.TenantId,
                    Name = prefix + " tenant",
                    Active = true
                }), cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.Created, tenantResponse.StatusCode, prefix + " create tenant");
            }

            await CreateUserAsync(server, principal.TenantId, principal.UserId, principal.Email, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage credentialResponse = await server.RestPostAsync("credentials?tenantId=" + principal.TenantId, JsonSerializer.Serialize(new
            {
                Id = principal.CredentialId,
                TenantId = principal.TenantId,
                UserId = principal.UserId,
                Description = prefix + " credential",
                AccessKey = principal.AccessKey,
                SecretKey = principal.SecretKey,
                IsBase64 = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialResponse.StatusCode, prefix + " create credential");

            if (tenantAdmin)
            {
                await GrantBuiltInRoleAsync(
                    server,
                    principal.TenantId,
                    "rol_builtin_tenantadmin",
                    "User",
                    principal.UserId,
                    "Tenant",
                    principal.TenantId,
                    cancellationToken).ConfigureAwait(false);
                await GrantBuiltInRoleAsync(
                    server,
                    principal.TenantId,
                    "rol_builtin_tenantadmin",
                    "Credential",
                    principal.CredentialId,
                    "Tenant",
                    principal.TenantId,
                    cancellationToken).ConfigureAwait(false);
            }

            return principal;
        }

        private static async Task CreateUserAsync(
            Less3TestServer server,
            string tenantId,
            string userId,
            string email,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage userResponse = await server.RestPostAsync("users?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = tenantId,
                Name = "Security test user " + userId,
                Email = email,
                PasswordHash = "password",
                IsAdmin = false,
                IsTenantAdmin = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userResponse.StatusCode, "create user " + userId);
        }

        private static async Task GrantBuiltInRoleAsync(
            Less3TestServer server,
            string tenantId,
            string roleId,
            string principalType,
            string principalId,
            string assignmentResourceType,
            string assignmentResourceId,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage assignmentResponse = await server.RestPostAsync("roleassignments?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = TestIds.Assignment(),
                TenantId = tenantId,
                RoleId = roleId,
                PrincipalType = principalType,
                PrincipalId = principalId,
                ResourceType = assignmentResourceType,
                ResourceId = assignmentResourceId,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, assignmentResponse.StatusCode, "grant " + roleId + " to " + principalType + " " + principalId);
        }

        private static async Task GrantCustomRoleAsync(
            Less3TestServer server,
            string tenantId,
            string principalId,
            string principalType,
            string permissionResourceType,
            string permissionOperation,
            bool permit,
            string assignmentResourceType,
            string assignmentResourceId,
            CancellationToken cancellationToken)
        {
            string roleId = TestIds.Role();

            HttpResponseMessage roleResponse = await server.RestPostAsync("roles?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = roleId,
                TenantId = tenantId,
                Name = "Security scoped role " + roleId,
                Description = "Security boundary test role",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, roleResponse.StatusCode, "create custom security role");

            HttpResponseMessage permissionResponse = await server.RestPostAsync("permissions?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = TestIds.Permission(),
                TenantId = tenantId,
                RoleId = roleId,
                ResourceType = permissionResourceType,
                Operation = permissionOperation,
                Permit = permit,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, permissionResponse.StatusCode, "create custom security permission");

            await GrantBuiltInRoleAsync(
                server,
                tenantId,
                roleId,
                principalType,
                principalId,
                assignmentResourceType,
                assignmentResourceId,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> LoginAsync(
            Less3TestServer server,
            string tenantId,
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await server.RestPostUnauthenticatedAsync("authsessions/login", JsonSerializer.Serialize(new
            {
                TenantId = tenantId,
                Email = email,
                Password = password,
                ExpirationMinutes = 30
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, response.StatusCode, "login " + email);

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            string? token = document.RootElement.GetProperty("Token").GetString();
            if (String.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("login response did not contain a token.");
            }

            return token;
        }

        private static async Task<HttpResponseMessage> SendBearerRestAsync(
            Less3TestServer server,
            HttpMethod method,
            string path,
            string token,
            string? body,
            CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, server.BaseUrl + "/api/v1/" + path);
            request.Headers.TryAddWithoutValidation("x-less3-session-token", token);
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return await server.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static async Task PutBucketAsync(IAmazonS3 client, string bucketName, CancellationToken cancellationToken)
        {
            PutBucketResponse response = await client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "create bucket " + bucketName);
        }

        private static async Task PutTextObjectAsync(
            IAmazonS3 client,
            string bucketName,
            string key,
            string body,
            CancellationToken cancellationToken)
        {
            PutObjectResponse response = await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = body,
                ContentType = "text/plain"
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put object " + bucketName + "/" + key);
        }

        private static async Task<string> ReadObjectBodyAsync(
            IAmazonS3 client,
            string bucketName,
            string key,
            CancellationToken cancellationToken)
        {
            using GetObjectResponse response = await client.GetObjectAsync(bucketName, key, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get object " + bucketName + "/" + key);
            using StreamReader reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        private static async Task<string> ReadBucketIdByNameAsync(
            Less3TestServer server,
            string tenantId,
            string bucketName,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await server.RestGetAsync("buckets?tenantId=" + tenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.StatusCode, "enumerate buckets for id lookup");
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            foreach (JsonElement item in document.RootElement.GetProperty("Items").EnumerateArray())
            {
                if (String.Equals(item.GetProperty("Name").GetString(), bucketName, StringComparison.Ordinal))
                {
                    string? id = item.GetProperty("Id").GetString();
                    if (!String.IsNullOrEmpty(id)) return id;
                }
            }

            throw new InvalidOperationException("Unable to find bucket id for " + bucketName + ".");
        }

        private static async Task<string> ReadObjectIdByKeyAsync(
            Less3TestServer server,
            string tenantId,
            string bucketId,
            string key,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await server.RestGetAsync(
                "objects?tenantId=" + tenantId + "&bucketId=" + bucketId,
                cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.StatusCode, "enumerate objects for id lookup");
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            foreach (JsonElement item in document.RootElement.GetProperty("Items").EnumerateArray())
            {
                if (String.Equals(item.GetProperty("Key").GetString(), key, StringComparison.Ordinal))
                {
                    string? id = item.GetProperty("Id").GetString();
                    if (!String.IsNullOrEmpty(id)) return id;
                }
            }

            throw new InvalidOperationException("Unable to find object id for " + key + ".");
        }

        private static async Task EnsureS3FailureAsync(Func<Task> action, string operation)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden
                || ex.StatusCode == HttpStatusCode.NotFound
                || ex.StatusCode == HttpStatusCode.BadRequest
                || ex.StatusCode == HttpStatusCode.Unauthorized
                || ex.StatusCode == HttpStatusCode.Conflict)
            {
                return;
            }

            throw new InvalidOperationException(operation + " unexpectedly succeeded.");
        }

        private static int CountBucket(ListBucketsResponse response, string bucketName)
        {
            if (response.Buckets == null) return 0;
            return response.Buckets.Count(bucket => String.Equals(bucket.BucketName, bucketName, StringComparison.Ordinal));
        }

        private static void EnsureStatus(HttpStatusCode expected, HttpStatusCode actual, string operation)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException(operation + " expected HTTP " + (int)expected + " but received HTTP " + (int)actual + ".");
            }
        }

        private static void EnsureEqual<T>(T expected, T actual, string operation)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(operation + " expected [" + expected + "] but received [" + actual + "].");
            }
        }

        private static void EnsureContains(string haystack, string needle, string operation)
        {
            if (haystack == null || !haystack.Contains(needle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " expected body to contain [" + needle + "].");
            }
        }

        #endregion

        #region Private-Classes

        private sealed class TenantPrincipal
        {
            internal string TenantId { get; set; } = String.Empty;
            internal string UserId { get; set; } = String.Empty;
            internal string CredentialId { get; set; } = String.Empty;
            internal string AccessKey { get; set; } = String.Empty;
            internal string SecretKey { get; set; } = String.Empty;
            internal string Email { get; set; } = String.Empty;
        }

        #endregion
    }
}
