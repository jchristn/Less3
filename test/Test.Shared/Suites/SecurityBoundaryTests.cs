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
                "SecurityBoundary_S3_ListBucketsMatchesAwsTenantAccountScope",
                () => SecurityBoundaryTestCases.S3ListBucketsMatchesAwsTenantAccountScopeAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_S3_NoRoleCredentialDeniedServiceBucketAndObjectAccess",
                () => SecurityBoundaryTestCases.S3NoRoleCredentialDeniedServiceBucketAndObjectAccessAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_S3_ObjectScopedRbacPermitsOnlyAssignedObject",
                () => SecurityBoundaryTestCases.S3ObjectScopedRbacPermitsOnlyAssignedObjectAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_S3_BuiltInCredentialRoleMatrix",
                () => SecurityBoundaryTestCases.S3BuiltInCredentialRoleMatrixAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_NoRoleAndTenantMemberRbacBoundaries",
                () => SecurityBoundaryTestCases.RestNoRoleAndTenantMemberRbacBoundariesAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_BuiltInRolePermissionMatrix",
                () => SecurityBoundaryTestCases.RestBuiltInRolePermissionMatrixAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_ResourceScopedRbacPermitsOnlyAssignedResource",
                () => SecurityBoundaryTestCases.RestResourceScopedRbacPermitsOnlyAssignedResourceAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_EffectivePermissionsBuiltInRoleMatrix",
                () => SecurityBoundaryTestCases.RestEffectivePermissionsBuiltInRoleMatrixAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_TenantIdQuerySpoofingDenied",
                () => SecurityBoundaryTestCases.RestTenantIdQuerySpoofingDeniedAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_TenantIdBodySpoofingDenied",
                () => SecurityBoundaryTestCases.RestTenantIdBodySpoofingDeniedAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_CrossTenantSecurityMutationSpoofingDenied",
                () => SecurityBoundaryTestCases.RestCrossTenantSecurityMutationSpoofingDeniedAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_REST_AdminApiKeyCanManageAcrossTenants",
                () => SecurityBoundaryTestCases.RestAdminApiKeyCanManageAcrossTenantsAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_RBAC_InactiveRolePermissionAssignmentIgnored",
                () => SecurityBoundaryTestCases.RbacInactiveRolePermissionAssignmentIgnoredAsync(_Server, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "SecurityBoundary_RBAC_CredentialExplicitDenyOverridesPermit",
                () => SecurityBoundaryTestCases.RbacCredentialExplicitDenyOverridesPermitAsync(_Server, CancellationToken.None)).ConfigureAwait(false);
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

        internal static async Task S3ListBucketsMatchesAwsTenantAccountScopeAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantAdmin = await CreateTenantPrincipalAsync(server, "s3-list-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantReader = await CreateTenantPrincipalAsync(server, "s3-list-reader", false, cancellationToken, tenantAdmin.TenantId).ConfigureAwait(false);
            TenantPrincipal otherTenantAdmin = await CreateTenantPrincipalAsync(server, "s3-list-other", true, cancellationToken).ConfigureAwait(false);

            await GrantCustomRoleAsync(
                server,
                tenantAdmin.TenantId,
                tenantReader.CredentialId,
                "Credential",
                "Storage",
                "Read",
                true,
                "Tenant",
                tenantAdmin.TenantId,
                cancellationToken).ConfigureAwait(false);

            string adminBucketA = "list-a-" + TestIds.Suffix().Substring(0, 8);
            string adminBucketB = "list-b-" + TestIds.Suffix().Substring(0, 8);
            string otherTenantBucket = "list-o-" + TestIds.Suffix().Substring(0, 8);

            using IAmazonS3 tenantAdminClient = server.CreateS3Client(tenantAdmin.AccessKey, tenantAdmin.SecretKey);
            using IAmazonS3 tenantReaderClient = server.CreateS3Client(tenantReader.AccessKey, tenantReader.SecretKey);
            using IAmazonS3 otherTenantClient = server.CreateS3Client(otherTenantAdmin.AccessKey, otherTenantAdmin.SecretKey);

            await PutBucketAsync(tenantAdminClient, adminBucketA, cancellationToken).ConfigureAwait(false);
            await PutBucketAsync(tenantAdminClient, adminBucketB, cancellationToken).ConfigureAwait(false);
            await PutBucketAsync(otherTenantClient, otherTenantBucket, cancellationToken).ConfigureAwait(false);

            ListBucketsResponse readerBuckets = await tenantReaderClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readerBuckets.HttpStatusCode, "same-tenant reader ListBuckets");
            EnsureEqual(tenantAdmin.TenantId, readerBuckets.Owner?.Id, "same-tenant reader ListBuckets owner id");
            EnsureEqual(2, CountBuckets(readerBuckets), "same-tenant reader bucket count");
            EnsureEqual(1, CountBucket(readerBuckets, adminBucketA), "same-tenant reader sees first tenant bucket");
            EnsureEqual(1, CountBucket(readerBuckets, adminBucketB), "same-tenant reader sees second tenant bucket");
            EnsureEqual(0, CountBucket(readerBuckets, otherTenantBucket), "same-tenant reader must not see other tenant bucket");
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

        internal static async Task S3BuiltInCredentialRoleMatrixAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "s3-role-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal member = await CreateTenantPrincipalAsync(server, "s3-role-member", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal securityAdmin = await CreateTenantPrincipalAsync(server, "s3-role-security", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal auditor = await CreateTenantPrincipalAsync(server, "s3-role-auditor", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal operatorPrincipal = await CreateTenantPrincipalAsync(server, "s3-role-operator", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal noRole = await CreateTenantPrincipalAsync(server, "s3-role-norole", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_tenantmember", "Credential", member.CredentialId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_securityadmin", "Credential", securityAdmin.CredentialId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_auditor", "Credential", auditor.CredentialId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_operator", "Credential", operatorPrincipal.CredentialId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);

            string bucketName = "s3role-" + TestIds.Suffix().Substring(0, 8);
            string adminKey = "admin-owned.txt";
            string operatorBucket = "oprole-" + TestIds.Suffix().Substring(0, 8);

            using IAmazonS3 adminClient = server.CreateS3Client(admin.AccessKey, admin.SecretKey);
            using IAmazonS3 memberClient = server.CreateS3Client(member.AccessKey, member.SecretKey);
            using IAmazonS3 securityClient = server.CreateS3Client(securityAdmin.AccessKey, securityAdmin.SecretKey);
            using IAmazonS3 auditorClient = server.CreateS3Client(auditor.AccessKey, auditor.SecretKey);
            using IAmazonS3 operatorClient = server.CreateS3Client(operatorPrincipal.AccessKey, operatorPrincipal.SecretKey);
            using IAmazonS3 noRoleClient = server.CreateS3Client(noRole.AccessKey, noRole.SecretKey);

            await PutBucketAsync(adminClient, bucketName, cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(adminClient, bucketName, adminKey, "admin-owned", cancellationToken).ConfigureAwait(false);

            ListBucketsResponse auditorBuckets = await auditorClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, auditorBuckets.HttpStatusCode, "auditor credential ListBuckets");
            EnsureEqual(1, CountBucket(auditorBuckets, bucketName), "auditor credential sees tenant admin bucket");
            EnsureEqual(
                "admin-owned",
                await ReadObjectBodyAsync(auditorClient, bucketName, adminKey, cancellationToken).ConfigureAwait(false),
                "auditor reads tenant object");
            await EnsureS3FailureAsync(
                () => auditorClient.PutBucketAsync(new PutBucketRequest { BucketName = "auditor-create-" + TestIds.Suffix().Substring(0, 8) }, cancellationToken),
                "auditor credential CreateBucket").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => auditorClient.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "auditor-write.txt",
                    ContentBody = "blocked"
                }, cancellationToken),
                "auditor credential PutObject").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => auditorClient.DeleteObjectAsync(bucketName, adminKey, cancellationToken),
                "auditor credential DeleteObject").ConfigureAwait(false);

            await PutBucketAsync(operatorClient, operatorBucket, cancellationToken).ConfigureAwait(false);
            await PutTextObjectAsync(operatorClient, bucketName, "operator-write.txt", "operator-write", cancellationToken).ConfigureAwait(false);
            ListBucketsResponse auditorBucketsAfterOperatorCreate = await auditorClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, auditorBucketsAfterOperatorCreate.HttpStatusCode, "auditor credential ListBuckets after operator bucket create");
            EnsureEqual(1, CountBucket(auditorBucketsAfterOperatorCreate, bucketName), "auditor credential still sees tenant admin bucket");
            EnsureEqual(1, CountBucket(auditorBucketsAfterOperatorCreate, operatorBucket), "auditor credential sees operator-created tenant bucket");
            await EnsureS3FailureAsync(
                () => operatorClient.ListBucketsAsync(cancellationToken),
                "operator credential ListBuckets").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => operatorClient.GetObjectAsync(bucketName, adminKey, cancellationToken),
                "operator credential read of admin-owned object").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => securityClient.ListBucketsAsync(cancellationToken),
                "security admin credential ListBuckets").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => securityClient.PutBucketAsync(new PutBucketRequest { BucketName = "security-create-" + TestIds.Suffix().Substring(0, 8) }, cancellationToken),
                "security admin credential CreateBucket").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => securityClient.GetObjectAsync(bucketName, adminKey, cancellationToken),
                "security admin credential GetObject").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => memberClient.ListBucketsAsync(cancellationToken),
                "tenant member credential ListBuckets").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => memberClient.GetObjectAsync(bucketName, adminKey, cancellationToken),
                "tenant member credential GetObject").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => noRoleClient.ListBucketsAsync(cancellationToken),
                "no-role credential ListBuckets in built-in role matrix").ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => noRoleClient.GetObjectAsync(bucketName, adminKey, cancellationToken),
                "no-role credential GetObject in built-in role matrix").ConfigureAwait(false);
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

        internal static async Task RestBuiltInRolePermissionMatrixAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "rest-role-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal noRole = await CreateTenantPrincipalAsync(server, "rest-role-norole", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal member = await CreateTenantPrincipalAsync(server, "rest-role-member", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal securityAdmin = await CreateTenantPrincipalAsync(server, "rest-role-security", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal auditor = await CreateTenantPrincipalAsync(server, "rest-role-auditor", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal operatorPrincipal = await CreateTenantPrincipalAsync(server, "rest-role-operator", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_tenantmember", "User", member.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_securityadmin", "User", securityAdmin.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_auditor", "User", auditor.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_operator", "User", operatorPrincipal.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);

            string targetUserId = TestIds.User();
            string deleteDeniedUserId = TestIds.User();
            await CreateUserAsync(server, admin.TenantId, targetUserId, "target-" + targetUserId + "@example.com", cancellationToken).ConfigureAwait(false);
            await CreateUserAsync(server, admin.TenantId, deleteDeniedUserId, "delete-denied-" + deleteDeniedUserId + "@example.com", cancellationToken).ConfigureAwait(false);

            string bucketName = "restrole-" + TestIds.Suffix().Substring(0, 8);
            string objectKey = "role-matrix.txt";
            using (IAmazonS3 adminClient = server.CreateS3Client(admin.AccessKey, admin.SecretKey))
            {
                await PutBucketAsync(adminClient, bucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(adminClient, bucketName, objectKey, "role-matrix", cancellationToken).ConfigureAwait(false);
            }

            string bucketId = await ReadBucketIdByNameAsync(server, admin.TenantId, bucketName, cancellationToken).ConfigureAwait(false);
            string objectId = await ReadObjectIdByKeyAsync(server, admin.TenantId, bucketId, objectKey, cancellationToken).ConfigureAwait(false);

            string noRoleToken = await LoginAsync(server, admin.TenantId, noRole.Email, "password", cancellationToken).ConfigureAwait(false);
            string memberToken = await LoginAsync(server, admin.TenantId, member.Email, "password", cancellationToken).ConfigureAwait(false);
            string securityToken = await LoginAsync(server, admin.TenantId, securityAdmin.Email, "password", cancellationToken).ConfigureAwait(false);
            string auditorToken = await LoginAsync(server, admin.TenantId, auditor.Email, "password", cancellationToken).ConfigureAwait(false);
            string operatorToken = await LoginAsync(server, admin.TenantId, operatorPrincipal.Email, "password", cancellationToken).ConfigureAwait(false);

            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, noRoleToken, null, HttpStatusCode.Forbidden, "no-role own tenant read", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "users/" + targetUserId, noRoleToken, null, HttpStatusCode.Forbidden, "no-role user read", cancellationToken).ConfigureAwait(false);

            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, memberToken, null, HttpStatusCode.OK, "TenantMember own tenant read", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId + "/exists", memberToken, null, HttpStatusCode.OK, "TenantMember own tenant exists", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants", memberToken, null, HttpStatusCode.Forbidden, "TenantMember tenant enumeration denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "users/" + targetUserId, memberToken, null, HttpStatusCode.Forbidden, "TenantMember user read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "users", memberToken, NewUserJson(admin.TenantId, TestIds.User(), "blocked-member"), HttpStatusCode.Forbidden, "TenantMember user create denied", cancellationToken).ConfigureAwait(false);

            string auditorUserBody = await EnsureBearerStatusAsync(server, HttpMethod.Get, "users/" + targetUserId, auditorToken, null, HttpStatusCode.OK, "Auditor user read", cancellationToken).ConfigureAwait(false);
            EnsureContains(auditorUserBody, targetUserId, "Auditor user read body");
            string auditorUsersBody = await EnsureBearerStatusAsync(server, HttpMethod.Get, "users?tenantId=" + admin.TenantId, auditorToken, null, HttpStatusCode.OK, "Auditor user enumerate", cancellationToken).ConfigureAwait(false);
            EnsureContains(auditorUsersBody, targetUserId, "Auditor user enumerate body");
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "buckets/" + bucketId + "?tenantId=" + admin.TenantId, auditorToken, null, HttpStatusCode.OK, "Auditor bucket read", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "objects/" + objectId + "?tenantId=" + admin.TenantId + "&bucketId=" + bucketId, auditorToken, null, HttpStatusCode.OK, "Auditor object read", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "authorizationaudit?tenantId=" + admin.TenantId, auditorToken, null, HttpStatusCode.OK, "Auditor authorization audit enumerate", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "users", auditorToken, NewUserJson(admin.TenantId, TestIds.User(), "blocked-auditor"), HttpStatusCode.Forbidden, "Auditor user create denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Put, "users/" + targetUserId, auditorToken, NewUserJson(admin.TenantId, targetUserId, "blocked-auditor-update"), HttpStatusCode.Forbidden, "Auditor user update denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Delete, "users/" + deleteDeniedUserId + "?tenantId=" + admin.TenantId, auditorToken, null, HttpStatusCode.Forbidden, "Auditor user delete denied", cancellationToken).ConfigureAwait(false);

            string securityCreatedUserId = TestIds.User();
            string securityRoleId = TestIds.Role();
            string securityPermissionId = TestIds.Permission();
            string securityAssignmentId = TestIds.Assignment();
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "users/" + targetUserId, securityToken, null, HttpStatusCode.OK, "SecurityAdmin user read", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "users", securityToken, NewUserJson(admin.TenantId, securityCreatedUserId, "security-created"), HttpStatusCode.Created, "SecurityAdmin user create", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "roles?tenantId=" + admin.TenantId, securityToken, RoleJson(admin.TenantId, securityRoleId, "Security matrix role", true), HttpStatusCode.Created, "SecurityAdmin role create", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "permissions?tenantId=" + admin.TenantId, securityToken, PermissionJson(admin.TenantId, securityPermissionId, securityRoleId, "Tenant", "Read", true, true), HttpStatusCode.Created, "SecurityAdmin permission create", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "roleassignments?tenantId=" + admin.TenantId, securityToken, AssignmentJson(admin.TenantId, securityAssignmentId, securityRoleId, "User", noRole.UserId, "Tenant", admin.TenantId, true), HttpStatusCode.Created, "SecurityAdmin assignment create", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "authorizationaudit?tenantId=" + admin.TenantId, securityToken, null, HttpStatusCode.OK, "SecurityAdmin authorization audit enumerate", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, securityToken, null, HttpStatusCode.Forbidden, "SecurityAdmin tenant read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "buckets/" + bucketId + "?tenantId=" + admin.TenantId, securityToken, null, HttpStatusCode.Forbidden, "SecurityAdmin bucket read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "buckets?tenantId=" + admin.TenantId, securityToken, BucketJson(admin.TenantId, TestIds.Bucket(), securityAdmin.UserId, "blocked-security-" + TestIds.Suffix().Substring(0, 8)), HttpStatusCode.Forbidden, "SecurityAdmin bucket create denied", cancellationToken).ConfigureAwait(false);

            string operatorTagId = TestIds.BucketTag();
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "buckettags?tenantId=" + admin.TenantId + "&bucketId=" + bucketId, operatorToken, JsonSerializer.Serialize(new
            {
                Id = operatorTagId,
                TenantId = admin.TenantId,
                BucketId = bucketId,
                Key = "operator",
                Value = "write"
            }), HttpStatusCode.Created, "Operator bucket tag create", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "buckettags/" + operatorTagId + "?tenantId=" + admin.TenantId + "&bucketId=" + bucketId, operatorToken, null, HttpStatusCode.Forbidden, "Operator bucket tag read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "buckets/" + bucketId + "?tenantId=" + admin.TenantId, operatorToken, null, HttpStatusCode.Forbidden, "Operator bucket read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "users/" + targetUserId, operatorToken, null, HttpStatusCode.Forbidden, "Operator user read denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "roles?tenantId=" + admin.TenantId, operatorToken, RoleJson(admin.TenantId, TestIds.Role(), "Blocked operator role", true), HttpStatusCode.Forbidden, "Operator role create denied", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, operatorToken, null, HttpStatusCode.Forbidden, "Operator tenant read denied", cancellationToken).ConfigureAwait(false);
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

        internal static async Task RestEffectivePermissionsBuiltInRoleMatrixAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "eff-role-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal member = await CreateTenantPrincipalAsync(server, "eff-role-member", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal securityAdmin = await CreateTenantPrincipalAsync(server, "eff-role-security", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal auditor = await CreateTenantPrincipalAsync(server, "eff-role-auditor", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal operatorPrincipal = await CreateTenantPrincipalAsync(server, "eff-role-operator", false, cancellationToken, admin.TenantId).ConfigureAwait(false);
            TenantPrincipal noRole = await CreateTenantPrincipalAsync(server, "eff-role-norole", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_tenantmember", "User", member.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_securityadmin", "User", securityAdmin.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_auditor", "User", auditor.UserId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);
            await GrantBuiltInRoleAsync(server, admin.TenantId, "rol_builtin_operator", "Credential", operatorPrincipal.CredentialId, "Tenant", admin.TenantId, cancellationToken).ConfigureAwait(false);

            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", member.UserId, "Tenant", admin.TenantId, "Read", true, "TenantMember tenant read", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", member.UserId, "User", noRole.UserId, "Read", false, "TenantMember user read denied", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", member.UserId, "Tenant", admin.TenantId, "Create", false, "TenantMember tenant create denied", cancellationToken).ConfigureAwait(false);

            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", auditor.UserId, "User", noRole.UserId, "Read", true, "Auditor user read", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", auditor.UserId, "Bucket", null, "Enumerate", true, "Auditor bucket enumerate", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", auditor.UserId, "User", noRole.UserId, "Create", false, "Auditor user create denied", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", auditor.UserId, "Object", null, "Delete", false, "Auditor object delete denied", cancellationToken).ConfigureAwait(false);

            await EnsureEffectivePermissionAsync(server, admin.TenantId, "Credential", operatorPrincipal.CredentialId, "Bucket", null, "Create", true, "Operator bucket create", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "Credential", operatorPrincipal.CredentialId, "Object", null, "Create", true, "Operator object create", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "Credential", operatorPrincipal.CredentialId, "Storage", null, "Read", false, "Operator storage read denied", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "Credential", operatorPrincipal.CredentialId, "Object", null, "Read", false, "Operator object read denied", cancellationToken).ConfigureAwait(false);

            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", securityAdmin.UserId, "User", noRole.UserId, "Create", true, "SecurityAdmin user create", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", securityAdmin.UserId, "Role", "rol_builtin_tenantmember", "Read", true, "SecurityAdmin role read", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", securityAdmin.UserId, "AuthorizationAudit", null, "Enumerate", true, "SecurityAdmin audit enumerate", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", securityAdmin.UserId, "Bucket", null, "Create", false, "SecurityAdmin bucket create denied", cancellationToken).ConfigureAwait(false);
            await EnsureEffectivePermissionAsync(server, admin.TenantId, "User", securityAdmin.UserId, "Tenant", admin.TenantId, "Read", false, "SecurityAdmin tenant read denied", cancellationToken).ConfigureAwait(false);
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

        internal static async Task RestCrossTenantSecurityMutationSpoofingDeniedAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal tenantA = await CreateTenantPrincipalAsync(server, "rest-sec-a", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal tenantB = await CreateTenantPrincipalAsync(server, "rest-sec-b", true, cancellationToken).ConfigureAwait(false);
            string tokenA = await LoginAsync(server, tenantA.TenantId, tenantA.Email, "password", cancellationToken).ConfigureAwait(false);

            string spoofedRoleId = TestIds.Role();
            string spoofedPermissionId = TestIds.Permission();
            string spoofedAssignmentId = TestIds.Assignment();
            string spoofedCredentialId = TestIds.Credential();

            await EnsureBearerStatusAsync(server, HttpMethod.Post, "roles", tokenA, RoleJson(tenantB.TenantId, spoofedRoleId, "Spoofed role", true), HttpStatusCode.Forbidden, "tenant A token create tenant B role through body TenantId", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "permissions", tokenA, PermissionJson(tenantB.TenantId, spoofedPermissionId, "rol_builtin_tenantmember", "Tenant", "Read", true, true), HttpStatusCode.Forbidden, "tenant A token create tenant B permission through body TenantId", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "roleassignments", tokenA, AssignmentJson(tenantB.TenantId, spoofedAssignmentId, "rol_builtin_tenantmember", "User", tenantB.UserId, "Tenant", tenantB.TenantId, true), HttpStatusCode.Forbidden, "tenant A token create tenant B assignment through body TenantId", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Post, "credentials", tokenA, JsonSerializer.Serialize(new
            {
                Id = spoofedCredentialId,
                TenantId = tenantB.TenantId,
                UserId = tenantB.UserId,
                Description = "spoofed credential",
                AccessKey = "spoof-" + TestIds.Suffix(),
                SecretKey = "secret-" + TestIds.Suffix(),
                IsBase64 = false,
                Active = true
            }), HttpStatusCode.Forbidden, "tenant A token create tenant B credential through body TenantId", cancellationToken).ConfigureAwait(false);

            await EnsureBearerStatusAsync(server, HttpMethod.Get, "roles?tenantId=" + tenantB.TenantId, tokenA, null, HttpStatusCode.Forbidden, "tenant A token enumerate tenant B roles", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "permissions?tenantId=" + tenantB.TenantId, tokenA, null, HttpStatusCode.Forbidden, "tenant A token enumerate tenant B permissions", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "roleassignments?tenantId=" + tenantB.TenantId, tokenA, null, HttpStatusCode.Forbidden, "tenant A token enumerate tenant B assignments", cancellationToken).ConfigureAwait(false);
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "credentials/" + tenantB.CredentialId + "?tenantId=" + tenantB.TenantId, tokenA, null, HttpStatusCode.Forbidden, "tenant A token read tenant B credential", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage verifyRole = await server.RestGetAsync("roles/" + spoofedRoleId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyRole.StatusCode, "spoofed tenant B role must not exist");
            HttpResponseMessage verifyPermission = await server.RestGetAsync("permissions/" + spoofedPermissionId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyPermission.StatusCode, "spoofed tenant B permission must not exist");
            HttpResponseMessage verifyAssignment = await server.RestGetAsync("roleassignments/" + spoofedAssignmentId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyAssignment.StatusCode, "spoofed tenant B assignment must not exist");
            HttpResponseMessage verifyCredential = await server.RestGetAsync("credentials/" + spoofedCredentialId + "?tenantId=" + tenantB.TenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NotFound, verifyCredential.StatusCode, "spoofed tenant B credential must not exist");
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

        internal static async Task RbacInactiveRolePermissionAssignmentIgnoredAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "inactive-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal scoped = await CreateTenantPrincipalAsync(server, "inactive-user", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            string roleId = TestIds.Role();
            string permissionId = TestIds.Permission();
            string assignmentId = TestIds.Assignment();
            string token = await LoginAsync(server, admin.TenantId, scoped.Email, "password", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage roleCreate = await server.RestPostAsync("roles?tenantId=" + admin.TenantId, RoleJson(admin.TenantId, roleId, "Inactive matrix role", true), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, roleCreate.StatusCode, "create inactive matrix role");
            HttpResponseMessage permissionCreate = await server.RestPostAsync("permissions?tenantId=" + admin.TenantId, PermissionJson(admin.TenantId, permissionId, roleId, "Tenant", "Read", true, true), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, permissionCreate.StatusCode, "create inactive matrix permission");
            HttpResponseMessage assignmentCreate = await server.RestPostAsync("roleassignments?tenantId=" + admin.TenantId, AssignmentJson(admin.TenantId, assignmentId, roleId, "User", scoped.UserId, "Tenant", admin.TenantId, false), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, assignmentCreate.StatusCode, "create inactive matrix assignment");

            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, token, null, HttpStatusCode.Forbidden, "inactive assignment denied", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage assignmentUpdate = await server.RestPutAsync("roleassignments/" + assignmentId + "?tenantId=" + admin.TenantId, AssignmentJson(admin.TenantId, assignmentId, roleId, "User", scoped.UserId, "Tenant", admin.TenantId, true), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, assignmentUpdate.StatusCode, "activate assignment");
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, token, null, HttpStatusCode.OK, "active assignment permits", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage permissionDeactivate = await server.RestPutAsync("permissions/" + permissionId + "?tenantId=" + admin.TenantId, PermissionJson(admin.TenantId, permissionId, roleId, "Tenant", "Read", true, false), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, permissionDeactivate.StatusCode, "deactivate permission");
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, token, null, HttpStatusCode.Forbidden, "inactive permission denied", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage permissionReactivate = await server.RestPutAsync("permissions/" + permissionId + "?tenantId=" + admin.TenantId, PermissionJson(admin.TenantId, permissionId, roleId, "Tenant", "Read", true, true), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, permissionReactivate.StatusCode, "reactivate permission");
            HttpResponseMessage roleDeactivate = await server.RestPutAsync("roles/" + roleId + "?tenantId=" + admin.TenantId, RoleJson(admin.TenantId, roleId, "Inactive matrix role", false), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, roleDeactivate.StatusCode, "deactivate role");
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, token, null, HttpStatusCode.Forbidden, "inactive role denied", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage roleReactivate = await server.RestPutAsync("roles/" + roleId + "?tenantId=" + admin.TenantId, RoleJson(admin.TenantId, roleId, "Inactive matrix role", true), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, roleReactivate.StatusCode, "reactivate role");
            await EnsureBearerStatusAsync(server, HttpMethod.Get, "tenants/" + admin.TenantId, token, null, HttpStatusCode.OK, "reactivated role permission assignment permits", cancellationToken).ConfigureAwait(false);
        }

        internal static async Task RbacCredentialExplicitDenyOverridesPermitAsync(
            Less3TestServer server,
            CancellationToken cancellationToken)
        {
            TenantPrincipal admin = await CreateTenantPrincipalAsync(server, "deny-admin", true, cancellationToken).ConfigureAwait(false);
            TenantPrincipal scoped = await CreateTenantPrincipalAsync(server, "deny-credential", false, cancellationToken, admin.TenantId).ConfigureAwait(false);

            string bucketName = "creddeny-" + TestIds.Suffix().Substring(0, 8);
            using IAmazonS3 adminClient = server.CreateS3Client(admin.AccessKey, admin.SecretKey);
            using IAmazonS3 scopedClient = server.CreateS3Client(scoped.AccessKey, scoped.SecretKey);

            await PutBucketAsync(adminClient, bucketName, cancellationToken).ConfigureAwait(false);

            await GrantCustomRoleAsync(
                server,
                admin.TenantId,
                scoped.CredentialId,
                "Credential",
                "Storage",
                "Read",
                true,
                "Tenant",
                admin.TenantId,
                cancellationToken).ConfigureAwait(false);

            ListBucketsResponse permittedBuckets = await scopedClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, permittedBuckets.HttpStatusCode, "credential storage read permit ListBuckets");
            await PutTextObjectAsync(adminClient, bucketName, "permit-read.txt", "permit-read", cancellationToken).ConfigureAwait(false);
            EnsureEqual(
                "permit-read",
                await ReadObjectBodyAsync(scopedClient, bucketName, "permit-read.txt", cancellationToken).ConfigureAwait(false),
                "credential storage read permit reads object");

            await GrantCustomRoleAsync(
                server,
                admin.TenantId,
                scoped.CredentialId,
                "Credential",
                "Storage",
                "Read",
                false,
                "Tenant",
                admin.TenantId,
                cancellationToken).ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => scopedClient.GetObjectAsync(bucketName, "permit-read.txt", cancellationToken),
                "credential explicit deny overrides storage read object permit").ConfigureAwait(false);
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

        private static async Task<string> EnsureBearerStatusAsync(
            Less3TestServer server,
            HttpMethod method,
            string path,
            string token,
            string? body,
            HttpStatusCode expected,
            string operation,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await SendBearerRestAsync(server, method, path, token, body, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != expected)
            {
                throw new InvalidOperationException(
                    operation + " expected HTTP " + (int)expected + " but received HTTP " + (int)response.StatusCode + ". Body: " + responseBody);
            }

            return responseBody;
        }

        private static async Task EnsureEffectivePermissionAsync(
            Less3TestServer server,
            string tenantId,
            string principalType,
            string principalId,
            string resourceType,
            string? resourceId,
            string operation,
            bool expectedPermitted,
            string assertion,
            CancellationToken cancellationToken)
        {
            string path = "effectivepermissions?tenantId=" + tenantId
                + "&principalType=" + principalType
                + "&principalId=" + principalId
                + "&resourceType=" + resourceType
                + "&operation=" + operation;
            if (!String.IsNullOrEmpty(resourceId))
            {
                path += "&resourceId=" + resourceId;
            }

            HttpResponseMessage response = await server.AdminGetAsync(path, cancellationToken).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.StatusCode, assertion + " effective permission response");

            using JsonDocument document = JsonDocument.Parse(body);
            bool permitted = document.RootElement.GetProperty("Permitted").GetBoolean();
            EnsureEqual(expectedPermitted, permitted, assertion + " effective permission permitted flag");
        }

        private static string NewUserJson(string tenantId, string userId, string label)
        {
            return JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = tenantId,
                Name = label + " " + userId,
                Email = label + "-" + userId + "@example.com",
                PasswordHash = "password",
                IsAdmin = false,
                IsTenantAdmin = false,
                Active = true
            });
        }

        private static string RoleJson(string tenantId, string roleId, string name, bool active)
        {
            return JsonSerializer.Serialize(new
            {
                Id = roleId,
                TenantId = tenantId,
                Name = name,
                Description = name,
                InheritsToChildren = true,
                Active = active
            });
        }

        private static string PermissionJson(
            string tenantId,
            string permissionId,
            string roleId,
            string resourceType,
            string operation,
            bool permit,
            bool active)
        {
            return JsonSerializer.Serialize(new
            {
                Id = permissionId,
                TenantId = tenantId,
                RoleId = roleId,
                ResourceType = resourceType,
                Operation = operation,
                Permit = permit,
                Active = active
            });
        }

        private static string AssignmentJson(
            string tenantId,
            string assignmentId,
            string roleId,
            string principalType,
            string principalId,
            string resourceType,
            string resourceId,
            bool active)
        {
            return JsonSerializer.Serialize(new
            {
                Id = assignmentId,
                TenantId = tenantId,
                RoleId = roleId,
                PrincipalType = principalType,
                PrincipalId = principalId,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Active = active
            });
        }

        private static string BucketJson(string tenantId, string bucketId, string ownerId, string name)
        {
            return JsonSerializer.Serialize(new
            {
                Id = bucketId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = name,
                RegionString = "us-west-1",
                StorageType = "Disk",
                DiskDirectory = "./disk/" + name + "/Objects/",
                EnableVersioning = false,
                EnablePublicWrite = false,
                EnablePublicRead = false
            });
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

        private static int CountBuckets(ListBucketsResponse response)
        {
            if (response.Buckets == null) return 0;
            return response.Buckets.Count;
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
