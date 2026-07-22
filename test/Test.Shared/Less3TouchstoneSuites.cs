namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using Less3.Helpers;
    using Touchstone.Core;

    /// <summary>
    /// Touchstone descriptor catalog for Less3 backend tests.
    /// </summary>
    public static class Less3TouchstoneSuites
    {
        /// <summary>
        /// All Touchstone suites.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    LiveTemporaryInstanceSuite(),
                    IdentifierAndContractSuite(),
                    TenantSuite(),
                    DatabaseSchemaAndMigrationSuite(),
                    AuthenticationAndSessionSuite(),
                    RbacSuite(),
                    S3ServiceAndBucketSuite(),
                    S3ObjectSuite(),
                    S3MultipartSuite(),
                    S3AclAndTaggingSuite(),
                    S3VersioningSuite(),
                    S3ProtocolCompatibilitySuite(),
                    Less3RestApiSuite(),
                    AdminApiSuite(),
                    RequestHistoryAndReportingSuite(),
                    HealthAndMaintenanceSuite(),
                    ProviderMatrixSuite(),
                    SecurityAndAuditSuite(),
                    ConcurrencyAndReliabilitySuite(),
                    DockerAndBootstrapSuite()
                };
            }
        }

        private static TestSuiteDescriptor LiveTemporaryInstanceSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "LiveTemporaryInstance",
                displayName: "Live Temporary Less3 Instance",
                cases: new List<TestCaseDescriptor>
                {
                    Active(
                        "LiveTemporaryInstance",
                        "StartsRootHealthOpenApiAndAdminAuth",
                        "Live server starts and exposes root, health, OpenAPI, and admin auth behavior",
                        StartsRootHealthOpenApiAndAdminAuthAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "AdminBootstrapCredentialAndS3ListBuckets",
                        "Live server creates user and credential, then authenticates S3 ListBuckets",
                        AdminBootstrapCredentialAndS3ListBucketsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "ContainerBootstrapDefaultCredentialAndS3ListBuckets",
                        "Container bootstrap seeds default tenant, admin user, and default S3 credential",
                        ContainerBootstrapDefaultCredentialAndS3ListBucketsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "FirstBootSeedsDefaultTenantAndRbacRestSurface",
                        "Live server first boot seeds default tenant and RBAC records visible through REST",
                        FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "AuthSessionLoginValidateAndRevoke",
                        "Live server creates, validates, and revokes a tenant-bound auth session",
                        AuthSessionLoginValidateAndRevokeAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "RestBearerSessionEnforcesRbacPermitAndDeny",
                        "Live server enforces RBAC for bearer-authenticated REST sessions",
                        RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "InactiveTenantBlocksLoginAndS3CredentialAuth",
                        "Live server rejects session login and S3 credential auth for inactive tenants",
                        InactiveTenantBlocksLoginAndS3CredentialAuthAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "Less3RestTenantCrudEnumerateAndExists",
                        "Live server supports Less3 REST tenant create, read, enumerate, update, exists, and delete",
                        Less3RestTenantCrudEnumerateAndExistsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "Less3RestRbacCrudEnumerateAndExists",
                        "Live server supports Less3 REST RBAC create, read, enumerate, update, exists, and delete",
                        Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "Less3RestObjectCrudEnumerateAndExists",
                        "Live server supports Less3 REST object metadata create, read, enumerate, update, exists, and delete",
                        Less3RestObjectCrudEnumerateAndExistsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "S3TenantIsolationRejectsCrossTenantBucketAndObjectAccess",
                        "Live server rejects S3 bucket and object access across tenant credentials",
                        S3TenantIsolationRejectsCrossTenantBucketAndObjectAccessAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "S3UnauthorizedCredentialCannotCreateBucket",
                        "Live server rejects S3 bucket creation for authenticated credentials without RBAC permissions",
                        S3UnauthorizedCredentialCannotCreateBucketAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "S3CredentialLastUsedAndLastFailedTimestamps",
                        "Live server updates credential last-used and last-failed timestamps during S3 auth",
                        S3CredentialLastUsedAndLastFailedTimestampsAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "RequestHistoryCapturesS3TenantCredentialAndFilters",
                        "Live server records S3 request history with tenant and credential filters",
                        RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active(
                        "LiveTemporaryInstance",
                        "S3SameBucketNameDifferentTenants",
                        "Live server allows the same S3 bucket name in different tenants resolved by access key",
                        S3SameBucketNameDifferentTenantsAsync)
                });
        }

        private static TestSuiteDescriptor IdentifierAndContractSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "IdentifierAndContracts",
                displayName: "Identifier and Public Contract Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesTenantId_WithPrefixAndMaxLength",
                        "PrettyID tenant IDs use prefix and maximum length",
                        PrettyIdGeneratesTenantIdWithPrefixAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesUserId_WithPrefixAndMaxLength",
                        "PrettyID user IDs use prefix and maximum length",
                        PrettyIdGeneratesUserIdWithPrefixAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesCredentialId_WithPrefixAndMaxLength",
                        "PrettyID credential IDs use prefix and maximum length",
                        PrettyIdGeneratesCredentialIdWithPrefixAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesBucketId_WithPrefixAndMaxLength",
                        "PrettyID bucket IDs use prefix and maximum length",
                        PrettyIdGeneratesBucketIdWithPrefixAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesObjectId_WithPrefixAndMaxLength",
                        "PrettyID object IDs use prefix and maximum length",
                        PrettyIdGeneratesObjectIdWithPrefixAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesUploadAndPartIds_WithPrefixesAndMaxLength",
                        "PrettyID multipart upload and part IDs use prefixes and maximum length",
                        PrettyIdGeneratesUploadAndPartIdsWithPrefixesAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratesRolePermissionAssignmentSessionAuditIds_WithPrefixesAndMaxLength",
                        "PrettyID RBAC/session/audit IDs use prefixes and maximum length",
                        PrettyIdGeneratesRbacSessionAuditIdsWithPrefixesAndMaxLengthAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_IsKSortableAcrossSequentialGeneration",
                        "PrettyID K-sortable IDs sort by generation time",
                        PrettyIdIsKSortableAcrossSequentialGenerationAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PrettyId_GeneratedIdsAreUniqueAcrossLargeSample",
                        "PrettyID values are unique across a large generated sample",
                        PrettyIdGeneratedIdsAreUniqueAcrossLargeSampleAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PublicContracts_ExposeStringId_ForTenantOwnedModels",
                        "Tenant-owned public contracts expose string Id properties",
                        PublicContractsExposeStringIdForTenantOwnedModelsAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PublicContracts_ExposeTenantIdOnTenantOwnedModels",
                        "Tenant-owned public contracts expose string TenantId properties",
                        PublicContractsExposeTenantIdOnTenantOwnedModelsAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PublicContracts_DoNotSerializeDatabaseIntegerIds",
                        "Tenant-owned public contracts do not expose integer Id properties",
                        PublicContractsDoNotSerializeDatabaseIntegerIdsAsync),
                    Active(
                        "IdentifierAndContracts",
                        "PublicContracts_DoNotSerializeLegacyGuidProperties",
                        "Public contracts do not expose legacy GUID-named properties",
                        PublicContractsDoNotSerializeLegacyGuidPropertiesAsync),
                    Active(
                        "IdentifierAndContracts",
                        "DashboardTypes_UseIdAndTenantId",
                        "Dashboard source uses Id and TenantId contract names without GUID names",
                        DashboardTypesUseIdAndTenantIdAsync),
                    Active(
                        "IdentifierAndContracts",
                        "OpenApiSchemas_UseIdAndTenantId",
                        "OpenAPI document uses Id and TenantId without GUID names",
                        OpenApiSchemasUseIdAndTenantIdAsync),
                    Active(
                        "IdentifierAndContracts",
                        "RequestHistory_UsesRequestIdAndTenantId",
                        "Request history exposes string request Id and tenant Id",
                        RequestHistoryUsesRequestIdAndTenantIdAsync),
                    Active(
                        "IdentifierAndContracts",
                        "BlobFilenames_DoNotUseGuidShapedNames",
                        "Server source does not generate GUID-shaped blob filenames",
                        BlobFilenamesDoNotUseGuidShapedNamesAsync),
                    Active(
                        "IdentifierAndContracts",
                        "NoGuidGeneration_RemainsAbsentInServerCode",
                        "Server code does not generate GUID identifiers",
                        NoGuidGenerationRemainsAbsentInServerCodeAsync),
                    Active(
                        "IdentifierAndContracts",
                        "NoGuidNamedRoutes_RemainInV3Api",
                        "V3 API source does not expose GUID-named routes or parameters",
                        NoGuidNamedRoutesRemainInV3ApiAsync),
                    Active(
                        "IdentifierAndContracts",
                        "NoGuidNamedDatabaseMethods_RemainInV3Interfaces",
                        "Database interfaces do not expose GUID-named methods",
                        NoGuidNamedDatabaseMethodsRemainInV3InterfacesAsync)
                });
        }

        private static TestSuiteDescriptor TenantSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Tenants",
                displayName: "Tenant Lifecycle and Isolation Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("Tenants", "Tenant_Create_DefaultsActive", "Tenant REST create defaults and active flag round-trip", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Planned("Tenants", "Tenant_Create_DuplicateIdReturnsConflict", "Duplicate tenant id conflict behavior needs exact active assertion."),
                    Planned("Tenants", "Tenant_Create_DuplicateNameAllowedAcrossDifferentTenantScopesWhereApplicable", "Tenant name scoping behavior needs exact active assertion."),
                    Active("Tenants", "Tenant_Read_ById", "Tenant REST read by id works", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Planned("Tenants", "Tenant_Read_NotFound", "Tenant not-found behavior needs exact active assertion."),
                    Active("Tenants", "Tenant_Enumerate_PaginatesAndSorts", "Tenant REST enumerate supports limit/offset/sort inputs", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Active("Tenants", "Tenant_Update_NameStatusAndMetadata", "Tenant REST update round-trips status", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Planned("Tenants", "Tenant_Update_NotFound", "Tenant update-not-found behavior needs exact active assertion."),
                    Active("Tenants", "Tenant_Delete_EmptyTenant", "Tenant REST delete removes an empty tenant", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Planned("Tenants", "Tenant_Delete_WithOwnedResourcesRequiresExplicitDestroy", "Tenant delete-with-owned-resources guard needs product-level behavior."),
                    Active("Tenants", "Tenant_Exists_ReturnsTrueForExistingTenant", "Tenant REST exists returns true", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Active("Tenants", "Tenant_Exists_ReturnsFalseForMissingTenant", "Tenant REST exists returns false after delete", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Active("Tenants", "Tenant_InactiveBlocksUserLogin", "Inactive tenants reject user login", InactiveTenantBlocksLoginAndS3CredentialAuthAsync),
                    Active("Tenants", "Tenant_InactiveBlocksCredentialAuth", "Inactive tenants reject S3 credential auth", InactiveTenantBlocksLoginAndS3CredentialAuthAsync),
                    Active("Tenants", "TenantIsolation_UserCannotReadOtherTenantTenantRecord", "Tenant session RBAC blocks unauthorized tenant reads", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Active("Tenants", "TenantIsolation_UserCannotEnumerateOtherTenantResources", "Tenant credentials cannot enumerate another tenant's buckets", S3ListBucketsReturnsOnlyCredentialTenantBucketsAsync),
                    Planned("Tenants", "TenantIsolation_SystemAdminCanEnumerateAcrossTenantsWhenRequested", "System-admin cross-tenant enumeration needs exact active assertion."),
                    Planned("Tenants", "TenantIsolation_TenantAdminCannotEscalateToSystemAdmin", "Tenant-admin escalation guard needs exact active assertion."),
                    Active("Tenants", "Tenant_DefaultSeedExistsOnFirstBoot", "First boot seeds default tenant", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Planned("Tenants", "Tenant_DefaultSeedIsIdempotentAcrossRestarts", "Seed idempotency across restart needs persistent fixture coverage.")
                });
        }

        private static TestSuiteDescriptor DatabaseSchemaAndMigrationSuite()
        {
            return PlannedSuite(
                "DatabaseSchemaAndMigrations",
                "Database Schema and Migration Coverage",
                "Provider schema conversion and idempotent migrations pending full implementation.",
                "Schema_TenantsTableExists_AllProviders",
                "Schema_RolesPermissionsAssignmentsSessionsAuditTablesExist_AllProviders",
                "Schema_CredentialTableRenamedCredentials_AllProviders",
                "Schema_NoSingularCredentialTable_AllProviders",
                "Schema_TenantIdColumnsExistOnBucketsObjectsTagsAclsUsersCredentialsUploadsRequestHistory",
                "Schema_ApplicationIdentityColumnsAreStringIds",
                "Schema_DatabasePrimaryKeyColumnRemainsId",
                "Schema_BucketNameUniquePerTenant",
                "Schema_AccessKeyGloballyUnique",
                "Schema_UserEmailUniquePerTenant",
                "Schema_CompoundIndexesExistForBucketLookup",
                "Schema_CompoundIndexesExistForObjectLookupAndVersionLookup",
                "Schema_CompoundIndexesExistForTagAndAclLookup",
                "Schema_CompoundIndexesExistForRequestHistoryFilters",
                "Schema_CompoundIndexesExistForRbacLookups",
                "Migration_V2ToV3_Sqlite_AppliesToRepresentativeV2Database",
                "Migration_V2ToV3_MySql_AppliesToRepresentativeV2Database",
                "Migration_V2ToV3_PostgreSql_AppliesToRepresentativeV2Database",
                "Migration_V2ToV3_SqlServer_AppliesToRepresentativeV2Database",
                "Migration_IsIdempotentWhenRunTwice_AllProviders",
                "Migration_PreservesExistingBucketsObjectsUsersCredentials_AllProviders",
                "Migration_RewritesLegacyIdValuesToPrettyIds_AllProviders",
                "FirstBoot_EmptyDatabaseInitializes_AllProviders",
                "FirstBoot_SeedsDefaultTenantAdminCredentialRolesPermissions_AllProviders",
                "FirstBoot_DoesNotRequireV2Artifacts_AllProviders",
                "Schema_ProviderColumnNamesRemainConsistentAcrossAllProviders",
                "Schema_CreatedUpdatedTimestampsUseUtc_AllProviders",
                "Schema_SecretMaterialIsNotStoredInRequestHistory_AllProviders");
        }

        private static TestSuiteDescriptor AuthenticationAndSessionSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "AuthenticationAndSessions",
                displayName: "Authentication and Session Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("AuthenticationAndSessions", "S3Auth_LoadsCredentialByGloballyUniqueAccessKey", "S3 auth loads credentials by globally unique access key", ContainerBootstrapDefaultCredentialAndS3ListBucketsAsync),
                    Active("AuthenticationAndSessions", "S3Auth_DerivesTenantFromCredential", "S3 auth derives tenant from credential access key", S3SameBucketNameDifferentTenantsAsync),
                    Planned("AuthenticationAndSessions", "S3Auth_RejectsUnknownAccessKey", "Unknown access key rejection needs exact signature-path active assertion."),
                    Active("AuthenticationAndSessions", "S3Auth_RejectsInactiveCredential", "S3 auth rejects inactive credentials", S3CredentialLastUsedAndLastFailedTimestampsAsync),
                    Planned("AuthenticationAndSessions", "S3Auth_RejectsInactiveUser", "Inactive user rejection needs exact active assertion."),
                    Active("AuthenticationAndSessions", "S3Auth_RejectsInactiveTenant", "S3 auth rejects inactive tenants", InactiveTenantBlocksLoginAndS3CredentialAuthAsync),
                    Planned("AuthenticationAndSessions", "S3Auth_RejectsCredentialUserTenantMismatch", "Credential/user tenant mismatch rejection needs exact active assertion."),
                    Active("AuthenticationAndSessions", "S3Auth_UpdatesLastUsedUtcOnSuccess", "S3 auth updates last-used timestamp", S3CredentialLastUsedAndLastFailedTimestampsAsync),
                    Active("AuthenticationAndSessions", "S3Auth_UpdatesLastFailedUtcOnFailure", "S3 auth updates last-failed timestamp", S3CredentialLastUsedAndLastFailedTimestampsAsync),
                    Active("AuthenticationAndSessions", "DashboardLogin_ValidAdminCreatesSession", "Dashboard session login creates a session", AuthSessionLoginValidateAndRevokeAsync),
                    Active("AuthenticationAndSessions", "DashboardLogin_InvalidPasswordFails", "Dashboard session login rejects an invalid password", AuthSessionLoginValidateAndRevokeAsync),
                    Planned("AuthenticationAndSessions", "DashboardLogin_InactiveUserFails", "Inactive user login rejection needs exact active assertion."),
                    Active("AuthenticationAndSessions", "DashboardLogin_InactiveTenantFails", "Dashboard session login rejects inactive tenants", InactiveTenantBlocksLoginAndS3CredentialAuthAsync),
                    Active("AuthenticationAndSessions", "Session_ValidateActiveToken", "Session validate accepts active tokens", AuthSessionLoginValidateAndRevokeAsync),
                    Planned("AuthenticationAndSessions", "Session_RejectExpiredToken", "Expired session behavior needs controllable expiration fixture."),
                    Active("AuthenticationAndSessions", "Session_RejectRevokedToken", "Session validate rejects revoked tokens", AuthSessionLoginValidateAndRevokeAsync),
                    Active("AuthenticationAndSessions", "Session_RevokeSingleSession", "Session revoke invalidates a single token", AuthSessionLoginValidateAndRevokeAsync),
                    Planned("AuthenticationAndSessions", "Session_RevokeAllForUser", "Revoke-all sessions endpoint needs product behavior."),
                    Active("AuthenticationAndSessions", "Session_TokenHashNeverReturnsRawToken", "Session APIs do not expose token hashes", AuthSessionLoginValidateAndRevokeAsync),
                    Planned("AuthenticationAndSessions", "Session_TenantBoundCannotCrossTenant", "Cross-tenant session reads need exact active assertion."),
                    Planned("AuthenticationAndSessions", "DirectCredentialAuth_AllowedOnlyForConfiguredAdminFlows", "Direct credential auth policy needs product behavior."),
                    Planned("AuthenticationAndSessions", "AuthContext_ContainsTenantUserCredentialSessionPrincipalScopesAndAdminFlags", "Full auth context shape needs exact active assertion across session and credential flows."),
                    Planned("AuthenticationAndSessions", "AuthContext_UnauthenticatedRequestsRemainExplicit", "Unauthenticated context assertion needs exact active coverage.")
                });
        }

        private static TestSuiteDescriptor RbacSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Rbac",
                displayName: "RBAC Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("Rbac", "Rbac_SeedsTenantAdminRole", "RBAC seeds TenantAdmin role", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Active("Rbac", "Rbac_SeedsSecurityAdminRole", "RBAC seeds SecurityAdmin role surface", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Active("Rbac", "Rbac_SeedsAuditorRole", "RBAC seeds Auditor role surface", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Active("Rbac", "Rbac_SeedsOperatorRole", "RBAC seeds Operator role surface", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Active("Rbac", "Rbac_SeedsTenantMemberRole", "RBAC seeds TenantMember role surface", FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync),
                    Planned("Rbac", "Rbac_BuiltInRolesAreImmutable", "Built-in role immutability needs exact mutation assertion."),
                    Active("Rbac", "Rbac_CreateCustomRole", "RBAC REST creates custom roles", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Rbac", "Rbac_UpdateCustomRole", "RBAC REST updates custom roles", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Rbac", "Rbac_DeleteUnusedCustomRole", "RBAC REST deletes unused custom roles", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Planned("Rbac", "Rbac_PreventDeleteAssignedRole", "Assigned-role delete guard needs exact active assertion."),
                    Active("Rbac", "Rbac_CreatePermission", "RBAC REST creates permissions", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Planned("Rbac", "Rbac_UpdatePermission", "Permission update needs exact active assertion."),
                    Active("Rbac", "Rbac_DeleteUnusedPermission", "RBAC REST deletes unused permissions", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Rbac", "Rbac_AssignRoleToUser", "RBAC role assignment to user works", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Active("Rbac", "Rbac_AssignRoleToCredential", "RBAC role assignment to credential works", S3SameBucketNameDifferentTenantsAsync),
                    Planned("Rbac", "Rbac_AssignPermissionToRole", "Direct permission-to-role update needs exact active assertion."),
                    Active("Rbac", "Rbac_AssignmentCanBeScopedToTenant", "RBAC assignments can be scoped to tenants", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Planned("Rbac", "Rbac_AssignmentCanBeScopedToBucket", "Bucket-scoped RBAC assignment needs exact active assertion."),
                    Planned("Rbac", "Rbac_AssignmentCanBeScopedToObjectPrefix", "Object-prefix scoped RBAC assignment needs exact active assertion."),
                    Active("Rbac", "Rbac_ExplicitDenyOverridesPermit", "RBAC explicit deny overrides permit", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Planned("Rbac", "Rbac_SystemAdminBypassWorksOnlyForSystemAdmin", "System-admin bypass semantics need exact active assertion."),
                    Planned("Rbac", "Rbac_TenantAdminLimitedToTenant", "Tenant-admin boundary needs exact active assertion."),
                    Planned("Rbac", "Rbac_AuditorReadOnly", "Auditor read-only role behavior needs exact active assertion."),
                    Planned("Rbac", "Rbac_OperatorCanOperateButCannotManageSecurity", "Operator role boundary needs exact active assertion."),
                    Active("Rbac", "Rbac_CustomRolePermissionSetControlsAccess", "Custom role permissions control REST access", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Active("Rbac", "Rbac_AuthorizationFailureAudited", "RBAC authorization failures are audited", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Planned("Rbac", "Rbac_SensitiveAdminOperationAudited", "Sensitive admin operation audit coverage needs admin RBAC behavior."),
                    Planned("Rbac", "Rbac_NoAuditSecretLeakage", "Authorization audit secret-leakage checks need exact active assertion.")
                });
        }

        private static TestSuiteDescriptor S3ServiceAndBucketSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "S3ServiceAndBuckets",
                displayName: "S3 Service and Bucket Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("S3ServiceAndBuckets", "S3_ListBuckets_ReturnsOnlyCredentialTenantBuckets", "S3 ListBuckets returns only buckets in the authenticated credential tenant", S3ListBucketsReturnsOnlyCredentialTenantBucketsAsync),
                    Active("S3ServiceAndBuckets", "S3_ListBuckets_EmptyTenantReturnsEmptyList", "S3 ListBuckets returns an empty set for a tenant without buckets", S3ListBucketsEmptyTenantReturnsEmptyListAsync),
                    Active("S3ServiceAndBuckets", "S3_CreateBucket_SucceedsForAuthorizedTenant", "S3 CreateBucket succeeds for an authorized tenant credential", S3CreateBucketSucceedsForAuthorizedTenantAsync),
                    Planned("S3ServiceAndBuckets", "S3_CreateBucket_DuplicateNameSameTenantFails", "Current S3 create-bucket behavior is idempotent for the same owner and must be reconciled with the v3 contract."),
                    Active("S3ServiceAndBuckets", "S3_CreateBucket_SameNameDifferentTenantSucceeds", "S3 CreateBucket allows the same bucket name in different tenants", S3SameBucketNameDifferentTenantsAsync),
                    Active("S3ServiceAndBuckets", "S3_CreateBucket_InvalidNameFails", "S3 CreateBucket rejects invalid bucket names", S3CreateBucketInvalidNameFailsAsync),
                    Active("S3ServiceAndBuckets", "S3_CreateBucket_ReservedRouteNameFails", "S3 CreateBucket rejects reserved route names", BucketReservedRouteNamesRejectedAcrossApisAsync),
                    Active("S3ServiceAndBuckets", "S3_CreateBucket_UnauthorizedRoleFails", "S3 CreateBucket rejects credentials without RBAC permission", S3UnauthorizedCredentialCannotCreateBucketAsync),
                    Active("S3ServiceAndBuckets", "S3_HeadBucket_ExistingSameTenantSucceeds", "S3 HeadBucket succeeds for an existing same-tenant bucket", S3HeadBucketExistingSameTenantSucceedsAsync),
                    Active("S3ServiceAndBuckets", "S3_HeadBucket_OtherTenantBucketReturnsNotFoundOrAccessDenied", "S3 HeadBucket fails for another tenant's bucket", S3HeadBucketOtherTenantBucketFailsAsync),
                    Active("S3ServiceAndBuckets", "S3_DeleteBucket_EmptyBucketSucceeds", "S3 DeleteBucket succeeds for an empty same-tenant bucket", S3DeleteBucketEmptyBucketSucceedsAsync),
                    Active("S3ServiceAndBuckets", "S3_DeleteBucket_NonEmptyBucketFailsWithoutDestroy", "S3 DeleteBucket rejects a non-empty bucket", S3DeleteBucketNonEmptyBucketFailsAsync),
                    Active("S3ServiceAndBuckets", "S3_DeleteBucket_OtherTenantBucketFails", "S3 DeleteBucket fails for another tenant's bucket", S3DeleteBucketOtherTenantBucketFailsAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_EmptyBucket", "S3 ListObjects returns an empty set for a new bucket", S3ListObjectsEmptyBucketAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_WithPrefix", "S3 ListObjects honors prefix filtering", S3ListObjectsWithPrefixAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_WithDelimiter", "S3 ListObjects returns common prefixes for delimiters", S3ListObjectsWithDelimiterAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_WithContinuationToken", "S3 ListObjects supports continuation tokens", S3ListObjectsWithContinuationAndMaxKeysAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_WithMaxKeys", "S3 ListObjects honors MaxKeys", S3ListObjectsWithContinuationAndMaxKeysAsync),
                    Active("S3ServiceAndBuckets", "S3_ListObjects_TruncatedResponseHasNextToken", "S3 ListObjects returns a next token when truncated", S3ListObjectsWithContinuationAndMaxKeysAsync),
                    Active("S3ServiceAndBuckets", "S3_BucketLocation_ReturnsConfiguredRegion", "S3 GetBucketLocation returns the configured region", S3BucketLocationReturnsConfiguredRegionAsync),
                    Active("S3ServiceAndBuckets", "S3_BucketVersioning_ReadDefault", "S3 GetBucketVersioning reads the default suspended state", S3BucketVersioningReadDefaultAsync),
                    Active("S3ServiceAndBuckets", "S3_BucketVersioning_EnableDisableRoundTrip", "S3 bucket versioning enable and suspend round-trips", S3BucketVersioningEnableDisableRoundTripAsync),
                    Active("S3ServiceAndBuckets", "S3_BucketMultipartUploads_ListEmpty", "S3 ListMultipartUploads returns empty for a new bucket", S3BucketMultipartUploadsListEmptyAsync),
                    Active("S3ServiceAndBuckets", "S3_BucketMultipartUploads_ListActiveUploadsTenantScoped", "S3 ListMultipartUploads returns active uploads and enforces tenant scope", S3BucketMultipartUploadsListActiveUploadsTenantScopedAsync)
                });
        }

        private static TestSuiteDescriptor S3ObjectSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "S3Objects",
                displayName: "S3 Object Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("S3Objects", "S3_PutObject_Text", "S3 PutObject writes text content", S3PutObjectTextAsync),
                    Active("S3Objects", "S3_PutObject_Binary", "S3 PutObject writes binary content", S3PutObjectBinaryAsync),
                    Active("S3Objects", "S3_PutObject_EmptyBody", "S3 PutObject writes empty content", S3PutObjectEmptyBodyAsync),
                    Active("S3Objects", "S3_PutObject_LargeObject", "S3 PutObject writes a large object", S3PutObjectLargeObjectAsync),
                    Active("S3Objects", "S3_PutObject_OverwritesUnversionedObject", "S3 PutObject overwrites an unversioned object", S3PutObjectOverwritesUnversionedObjectAsync),
                    Active("S3Objects", "S3_PutObject_CreatesNewVersionWhenVersioningEnabled", "S3 PutObject creates new versions when versioning is enabled", S3PutObjectCreatesNewVersionWhenVersioningEnabledAsync),
                    Active("S3Objects", "S3_PutObject_WithContentType", "S3 PutObject preserves content type", S3PutObjectWithContentTypeAsync),
                    Active("S3Objects", "S3_PutObject_WithMetadata", "S3 PutObject preserves user metadata", S3PutObjectWithMetadataAsync),
                    Active("S3Objects", "S3_PutObject_OtherTenantBucketFails", "S3 PutObject fails for another tenant's bucket", S3TenantIsolationRejectsCrossTenantBucketAndObjectAccessAsync),
                    Active("S3Objects", "S3_HeadObject_Existing", "S3 HeadObject succeeds for an existing object", S3HeadObjectExistingAsync),
                    Active("S3Objects", "S3_HeadObject_Missing", "S3 HeadObject fails for a missing object", S3HeadObjectMissingAsync),
                    Active("S3Objects", "S3_GetObject_Text", "S3 GetObject reads text content", S3GetObjectTextAsync),
                    Active("S3Objects", "S3_GetObject_Binary", "S3 GetObject reads binary content", S3GetObjectBinaryAsync),
                    Active("S3Objects", "S3_GetObject_RangeStartEnd", "S3 GetObject supports explicit byte ranges", S3GetObjectRangeStartEndAsync),
                    Planned("S3Objects", "S3_GetObject_RangeSuffix", "Suffix range semantics need explicit raw HTTP coverage."),
                    Active("S3Objects", "S3_GetObject_InvalidRangeReturns416", "S3 GetObject rejects invalid byte ranges", S3GetObjectInvalidRangeReturns416Async),
                    Planned("S3Objects", "S3_GetObject_IfMatch", "Conditional GET semantics pending active coverage."),
                    Planned("S3Objects", "S3_GetObject_IfNoneMatch", "Conditional GET semantics pending active coverage."),
                    Planned("S3Objects", "S3_CopyObject_SameBucket", "CopyObject implementation pending active coverage."),
                    Planned("S3Objects", "S3_CopyObject_CrossBucketSameTenant", "CopyObject implementation pending active coverage."),
                    Planned("S3Objects", "S3_CopyObject_CrossTenantFails", "CopyObject implementation pending active coverage."),
                    Active("S3Objects", "S3_DeleteObject_Existing", "S3 DeleteObject removes an existing object", S3DeleteObjectExistingAsync),
                    Planned("S3Objects", "S3_DeleteObject_MissingIsIdempotent", "Current DeleteObject validates object existence and must be reconciled with the v3 contract."),
                    Active("S3Objects", "S3_DeleteObjects_Multiple", "S3 DeleteObjects removes multiple objects", S3DeleteObjectsMultipleAsync),
                    Active("S3Objects", "S3_DeleteObjects_MixedExistingAndMissing", "S3 DeleteObjects reports mixed existing and missing keys", S3DeleteObjectsMixedExistingAndMissingAsync),
                    Planned("S3Objects", "S3_ObjectKeys_WithSpacesUnicodeAndReservedCharacters", "Object-key edge coverage needs raw URL encoding assertions."),
                    Active("S3Objects", "S3_ObjectKeys_WithNestedPrefixes", "S3 object keys with nested prefixes are listable", S3ObjectKeysWithNestedPrefixesAsync),
                    Active("S3Objects", "S3_ObjectContent_EtagStableForStoredContent", "S3 object ETags remain stable for stored content", S3ObjectContentEtagStableForStoredContentAsync),
                    Planned("S3Objects", "S3_ObjectStorage_BlobFileCreatedUnderTenantScopedPath", "Storage path assertions need provider-neutral blob location helpers.")
                });
        }

        private static TestSuiteDescriptor S3MultipartSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "S3Multipart",
                displayName: "S3 Multipart Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("S3Multipart", "S3_CreateMultipartUpload_ReturnsPrettyUploadId", "S3 multipart create returns a PrettyId upload id", S3CreateMultipartUploadReturnsPrettyUploadIdAsync),
                    Active("S3Multipart", "S3_UploadPart_SinglePart", "S3 multipart upload accepts a single part", S3UploadPartSinglePartAsync),
                    Active("S3Multipart", "S3_UploadPart_MultipleParts", "S3 multipart upload accepts multiple parts", S3UploadPartMultiplePartsAsync),
                    Active("S3Multipart", "S3_UploadPart_OverwritePartNumber", "S3 multipart upload overwrites a repeated part number", S3UploadPartOverwritePartNumberAsync),
                    Active("S3Multipart", "S3_ListParts_ReturnsUploadedParts", "S3 multipart list parts returns uploaded parts", S3ListPartsReturnsUploadedPartsAsync),
                    Planned("S3Multipart", "S3_ListParts_Paginates", "ListParts pagination needs explicit marker support coverage."),
                    Active("S3Multipart", "S3_CompleteMultipartUpload_AssemblesObject", "S3 multipart complete assembles the stored object", S3CompleteMultipartUploadAssemblesObjectAsync),
                    Active("S3Multipart", "S3_CompleteMultipartUpload_MissingPartFails", "S3 multipart complete rejects a missing part", S3CompleteMultipartUploadMissingPartFailsAsync),
                    Active("S3Multipart", "S3_CompleteMultipartUpload_InvalidEtagFails", "S3 multipart complete rejects an invalid ETag", S3CompleteMultipartUploadInvalidEtagFailsAsync),
                    Active("S3Multipart", "S3_AbortMultipartUpload_RemovesUploadAndParts", "S3 multipart abort removes uploaded parts", S3AbortMultipartUploadRemovesUploadAndPartsAsync),
                    Active("S3Multipart", "S3_AbortMultipartUpload_MissingUploadFails", "S3 multipart abort rejects a missing upload id", S3AbortMultipartUploadMissingUploadFailsAsync),
                    Active("S3Multipart", "S3_Multipart_OtherTenantUploadIdFails", "S3 multipart operations fail across tenants", S3MultipartOtherTenantUploadIdFailsAsync),
                    Active("S3Multipart", "S3_Multipart_TempFilesCleanedAfterComplete", "S3 multipart complete cleans temporary files", S3MultipartTempFilesCleanedAfterCompleteAsync),
                    Active("S3Multipart", "S3_Multipart_TempFilesCleanedAfterAbort", "S3 multipart abort cleans temporary files", S3MultipartTempFilesCleanedAfterAbortAsync),
                    Planned("S3Multipart", "S3_Multipart_ExpiredUploadCleanup", "Expired multipart cleanup needs clock or retention control hooks.")
                });
        }

        private static TestSuiteDescriptor S3AclAndTaggingSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "S3AclAndTagging",
                displayName: "S3 ACL and Tagging Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("S3AclAndTagging", "S3_BucketAcl_ReadDefaultOwner", "S3 bucket ACL reads default owner grants", S3BucketAclReadDefaultOwnerAsync),
                    Active("S3AclAndTagging", "S3_BucketAcl_WriteCannedPrivate", "S3 bucket ACL writes canned private ACLs", S3BucketAclWriteCannedPrivateAsync),
                    Active("S3AclAndTagging", "S3_BucketAcl_WriteCannedPublicRead", "S3 bucket ACL writes canned public-read ACLs", S3BucketAclWriteCannedPublicReadAsync),
                    Planned("S3AclAndTagging", "S3_BucketAcl_WriteGrantByCanonicalUser", "Canonical-user grant coverage needs tenant user grant fixtures."),
                    Planned("S3AclAndTagging", "S3_BucketAcl_DeleteOrOverwriteExistingGrants", "ACL overwrite semantics need explicit grant diff assertions."),
                    Planned("S3AclAndTagging", "S3_BucketAcl_OtherTenantUserGrantFails", "Cross-tenant canonical-user grant rejection needs product-level validation."),
                    Active("S3AclAndTagging", "S3_ObjectAcl_ReadDefaultOwner", "S3 object ACL reads default owner grants", S3ObjectAclReadDefaultOwnerAsync),
                    Active("S3AclAndTagging", "S3_ObjectAcl_WriteCannedPrivate", "S3 object ACL writes canned private ACLs", S3ObjectAclWriteCannedPrivateAsync),
                    Planned("S3AclAndTagging", "S3_ObjectAcl_WriteGrantByCanonicalUser", "Canonical-user object grant coverage needs tenant user grant fixtures."),
                    Planned("S3AclAndTagging", "S3_ObjectAcl_OtherTenantUserGrantFails", "Cross-tenant object grant rejection needs product-level validation."),
                    Planned("S3AclAndTagging", "S3_BucketTags_ReadMissingReturnsEmpty", "Current bucket tag read returns NoSuchTagSet and must be reconciled with the v3 contract."),
                    Active("S3AclAndTagging", "S3_BucketTags_PutGetDeleteRoundTrip", "S3 bucket tags put/get/delete round-trips", S3BucketTagsPutGetDeleteRoundTripAsync),
                    Planned("S3AclAndTagging", "S3_BucketTags_MaxTagCountEnforced", "Bucket tag validation limits need product-level enforcement."),
                    Planned("S3AclAndTagging", "S3_BucketTags_InvalidKeyFails", "Bucket tag key validation needs product-level enforcement."),
                    Active("S3AclAndTagging", "S3_ObjectTags_ReadMissingReturnsEmpty", "S3 object tags read empty when no tags exist", S3ObjectTagsReadMissingReturnsEmptyAsync),
                    Active("S3AclAndTagging", "S3_ObjectTags_PutGetDeleteRoundTrip", "S3 object tags put/get/delete round-trips", S3ObjectTagsPutGetDeleteRoundTripAsync),
                    Active("S3AclAndTagging", "S3_ObjectTags_VersionSpecificRoundTrip", "S3 object tags round-trip for a specific version", S3ObjectTagsVersionSpecificRoundTripAsync),
                    Active("S3AclAndTagging", "S3_Tags_OtherTenantResourceFails", "S3 tag operations fail across tenants", S3TagsOtherTenantResourceFailsAsync),
                    Planned("S3AclAndTagging", "S3_AclAndTags_RequestHistoryCapturesOperation", "Request-history operation names for ACL/tagging need exact active assertions.")
                });
        }

        private static TestSuiteDescriptor S3VersioningSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "S3Versioning",
                displayName: "S3 Versioning Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("S3Versioning", "S3_Versioning_DefaultSuspended", "S3 versioning defaults to suspended", S3BucketVersioningReadDefaultAsync),
                    Active("S3Versioning", "S3_Versioning_Enable", "S3 versioning can be enabled", S3VersioningEnableAsync),
                    Active("S3Versioning", "S3_Versioning_Suspend", "S3 versioning can be suspended", S3BucketVersioningEnableDisableRoundTripAsync),
                    Active("S3Versioning", "S3_PutObject_VersionIdsIncrease", "S3 object version ids increase across writes", S3PutObjectCreatesNewVersionWhenVersioningEnabledAsync),
                    Active("S3Versioning", "S3_GetObject_ByVersionId", "S3 GetObject can read a specific version id", S3GetObjectByVersionIdAsync),
                    Active("S3Versioning", "S3_GetObject_MissingVersionFails", "S3 GetObject rejects missing version ids", S3GetObjectMissingVersionFailsAsync),
                    Active("S3Versioning", "S3_DeleteObject_CreatesDeleteMarkerWhenVersioningEnabled", "S3 DeleteObject creates delete markers when versioning is enabled", S3DeleteObjectCreatesDeleteMarkerWhenVersioningEnabledAsync),
                    Active("S3Versioning", "S3_DeleteObjectVersion_RemovesSpecificVersion", "S3 DeleteObject with version id removes a specific version", S3DeleteObjectVersionRemovesSpecificVersionAsync),
                    Active("S3Versioning", "S3_ListObjectVersions_ReturnsVersionsAndDeleteMarkers", "S3 ListObjectVersions returns versions and delete markers", S3ListObjectVersionsReturnsVersionsAndDeleteMarkersAsync),
                    Planned("S3Versioning", "S3_ListObjectVersions_Paginates", "ListObjectVersions pagination needs explicit marker support coverage."),
                    Planned("S3Versioning", "S3_ListObjectVersions_PrefixAndDelimiter", "Current ListObjectVersions delimiter handling does not expose common prefixes."),
                    Planned("S3Versioning", "S3_RestoreVersion_ByCopyToCurrent", "Version restore-by-copy needs CopyObject coverage."),
                    Active("S3Versioning", "S3_Versioning_OtherTenantVersionAccessFails", "S3 version-specific reads fail across tenants", S3VersioningOtherTenantVersionAccessFailsAsync)
                });
        }

        private static TestSuiteDescriptor S3ProtocolCompatibilitySuite()
        {
            return PlannedSuite(
                "S3ProtocolCompatibility",
                "S3 Protocol Compatibility Coverage",
                "AWS SDK protocol parity coverage pending full implementation.",
                "S3_UnsignedRequestRejectedWhenSignaturesEnabled",
                "S3_SignatureV2AcceptedWhenEnabled",
                "S3_SignatureV4AcceptedWhenEnabled",
                "S3_InvalidSignatureRejected",
                "S3_WrongSecretRejected",
                "S3_ClockSkewRejectedWhenConfigured",
                "S3_PathStyleAddressingWorks",
                "S3_VirtualHostedStyleAddressingWorks",
                "S3_CorsPreflightAllowsS3Headers",
                "S3_ErrorShape_NoSuchBucket",
                "S3_ErrorShape_NoSuchKey",
                "S3_ErrorShape_AccessDenied",
                "S3_ErrorShape_InvalidRequest",
                "S3_ResponseHeaders_EtagContentLengthContentType",
                "S3_ResponseHeaders_RequestIdPresent",
                "S3_ChunkedUploadCompatibility",
                "S3_AwsSdkNativeClientCompatibility",
                "S3_AwsCliCompatibilitySmoke");
        }

        private static TestSuiteDescriptor Less3RestApiSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Less3RestApi",
                displayName: "Less3 REST API Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("Less3RestApi", "Rest_Tenants_CreateReadEnumerateUpdateDeleteExists", "Less3 REST tenants CRUD/enumerate/exists", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Buckets_CreateReadEnumerateUpdateDeleteExists", "Less3 REST bucket CRUD/enumerate/exists", Less3RestBucketCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Buckets_ReservedRouteNameFails", "Less3 REST bucket create rejects reserved route names", BucketReservedRouteNamesRejectedAcrossApisAsync),
                    Active("Less3RestApi", "Rest_Objects_CreateReadEnumerateUpdateDeleteExists", "Less3 REST objects CRUD/enumerate/exists", Less3RestObjectCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_BucketTags_CreateReadEnumerateUpdateDeleteExists", "Less3 REST bucket tag CRUD/enumerate/exists", Less3RestTagAndAclCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_ObjectTags_CreateReadEnumerateUpdateDeleteExists", "Less3 REST object tag CRUD/enumerate/exists", Less3RestTagAndAclCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_BucketAcls_CreateReadEnumerateUpdateDeleteExists", "Less3 REST bucket ACL CRUD/enumerate/exists", Less3RestTagAndAclCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_ObjectAcls_CreateReadEnumerateUpdateDeleteExists", "Less3 REST object ACL CRUD/enumerate/exists", Less3RestTagAndAclCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Users_CreateReadEnumerateUpdateDeleteExists", "Less3 REST user CRUD/enumerate/exists", Less3RestUserAndCredentialCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Credentials_CreateReadEnumerateUpdateDeleteExists", "Less3 REST credential CRUD/enumerate/exists", Less3RestUserAndCredentialCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Roles_CreateReadEnumerateUpdateDeleteExists", "Less3 REST roles CRUD/enumerate/exists", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_Permissions_CreateReadEnumerateUpdateDeleteExists", "Less3 REST permissions CRUD/enumerate/exists", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_RoleAssignments_CreateReadEnumerateUpdateDeleteExists", "Less3 REST role assignments CRUD/enumerate/exists", Less3RestRbacCrudEnumerateAndExistsAsync),
                    Active("Less3RestApi", "Rest_AuthSessions_ReadEnumerateRevokeExists", "Less3 REST auth session read/enumerate/revoke/exists", AuthSessionRestReadEnumerateRevokeExistsAsync),
                    Active("Less3RestApi", "Rest_AuthorizationAudit_ReadEnumerateDeleteExists", "Less3 REST authorization audit read/enumerate/delete/exists", AuthorizationAuditRestReadEnumerateDeleteExistsAsync),
                    Active("Less3RestApi", "Rest_RequestHistory_ReadEnumerateDeleteExists", "Less3 REST request history read/enumerate/delete/exists", RequestHistoryRestReadEnumerateDeleteExistsAsync),
                    Planned("Less3RestApi", "Rest_LogicalOperation_MultiDeleteUsesPost", "REST logical multi-delete operation needs exact active assertion."),
                    Active("Less3RestApi", "Rest_Enumeration_LimitOffsetSort", "Less3 REST enumeration accepts limit/offset/sort", Less3RestTenantCrudEnumerateAndExistsAsync),
                    Planned("Less3RestApi", "Rest_Enumeration_ContinuationToken", "REST continuation-token enumeration needs product behavior."),
                    Active("Less3RestApi", "Rest_Enumeration_FilterEcho", "Less3 REST request-history enumeration applies filters", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("Less3RestApi", "Rest_Enumeration_TenantScopeEnforced", "Less3 REST tenant scoping is enforced by session RBAC", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Planned("Less3RestApi", "Rest_CancellationToken_PropagatesToDatabase", "Cancellation token propagation needs instrumented database coverage."),
                    Planned("Less3RestApi", "Rest_InvalidJsonReturns400", "Invalid JSON response shape needs exact active assertion."),
                    Planned("Less3RestApi", "Rest_InvalidIdReturns404", "Invalid/missing id response shape needs exact active assertion."),
                    Planned("Less3RestApi", "Rest_UnauthorizedReturns401", "REST unauthenticated response shape needs exact active assertion."),
                    Active("Less3RestApi", "Rest_ForbiddenReturns403", "REST session RBAC returns forbidden for denied requests", RestBearerSessionEnforcesRbacPermitAndDenyAsync)
                });
        }

        private static TestSuiteDescriptor AdminApiSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "AdminApi",
                displayName: "Admin API Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("AdminApi", "Admin_Health_RequiresAdminKey", "Admin health requires the admin API key", StartsRootHealthOpenApiAndAdminAuthAsync),
                    Active("AdminApi", "Admin_Health_ReturnsVersionUptimeDatabaseStorageDiskTempRetentionCleanup", "Admin health returns live status fields", StartsRootHealthOpenApiAndAdminAuthAsync),
                    Planned("AdminApi", "Admin_Stats_ReturnsBucketObjectStorageTotals", "Admin stats endpoint needs exact active assertion."),
                    Planned("AdminApi", "Admin_Stats_TenantScopedForTenantAdmin", "Admin stats tenant scoping needs tenant admin auth behavior."),
                    Planned("AdminApi", "Admin_Users_CreateReadListUpdateDelete", "Admin users CRUD needs exact active assertion."),
                    Planned("AdminApi", "Admin_Users_TenantScoped", "Admin users tenant scoping needs exact active assertion."),
                    Planned("AdminApi", "Admin_Users_DuplicateEmailSameTenantFails", "Duplicate same-tenant user email needs exact active assertion."),
                    Planned("AdminApi", "Admin_Users_DuplicateEmailDifferentTenantSucceeds", "Duplicate cross-tenant user email needs exact active assertion."),
                    Planned("AdminApi", "Admin_Credentials_CreateReadListUpdateDelete", "Admin credential CRUD needs exact active assertion."),
                    Planned("AdminApi", "Admin_Credentials_AccessKeyGloballyUnique", "Credential access-key uniqueness needs exact active assertion."),
                    Planned("AdminApi", "Admin_Credentials_SecretHiddenExceptCreate", "Credential secret one-time return behavior needs product coverage."),
                    Planned("AdminApi", "Admin_Credentials_Rotate", "Credential rotation needs product behavior."),
                    Planned("AdminApi", "Admin_Credentials_Disable", "Credential disable needs exact active assertion."),
                    Active("AdminApi", "Admin_Credentials_LastUsedLastFailed", "Credential last-used/last-failed fields update through S3 auth", S3CredentialLastUsedAndLastFailedTimestampsAsync),
                    Planned("AdminApi", "Admin_Buckets_CreateReadListDelete", "Admin bucket CRUD needs exact active assertion."),
                    Active("AdminApi", "Admin_Buckets_ReservedRouteNameFails", "Admin bucket create rejects reserved route names", BucketReservedRouteNamesRejectedAcrossApisAsync),
                    Planned("AdminApi", "Admin_Buckets_DuplicateNameSameTenantFails", "Same-tenant duplicate bucket behavior currently differs from the v3 contract."),
                    Active("AdminApi", "Admin_Buckets_DuplicateNameDifferentTenantSucceeds", "Same bucket name can exist in different tenants", S3SameBucketNameDifferentTenantsAsync),
                    Active("AdminApi", "Admin_RequestHistory_ServerSideEnumeration", "Admin request history enumeration is available through REST", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("AdminApi", "Admin_OpenApi_CombinedDocumentAvailable", "Combined OpenAPI document is available", StartsRootHealthOpenApiAndAdminAuthAsync),
                    Active("AdminApi", "Admin_InvalidApiKeyReturns401", "Admin endpoints reject missing or invalid API keys", StartsRootHealthOpenApiAndAdminAuthAsync),
                    Active("AdminApi", "Admin_MissingApiKeyReturns401", "Admin endpoints reject missing API keys", StartsRootHealthOpenApiAndAdminAuthAsync)
                });
        }

        private static TestSuiteDescriptor RequestHistoryAndReportingSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "RequestHistoryAndReporting",
                displayName: "Request History and Reporting Coverage",
                cases: new List<TestCaseDescriptor>
                {
                    Active("RequestHistoryAndReporting", "RequestHistory_CapturesTenantIdUserIdAccessKeyRequestTypeMethodStatusDuration", "Request history captures tenant/access-key/request fields", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_RedactsSecretHeadersAndBodies", "Request history redacts secret request data", AuthSessionLoginValidateAndRevokeAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_CapturesFailure", "Request history captures failed authentication attempts", AuthSessionLoginValidateAndRevokeAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_CapturesAuthorizationFailure", "Authorization audit captures RBAC denials", RestBearerSessionEnforcesRbacPermitAndDenyAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_LimitOffset", "Request history enumerate accepts limit/offset", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_StartEndUtc", "Request history enumerate filters by start/end UTC", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_Method", "Request history enumerate filters by method", AuthSessionLoginValidateAndRevokeAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_Status", "Request history enumerate filters by status", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_Success", "Request history captures success state", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_SourceIp", "Request history captures and filters by source IP", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_RequestType", "Request history filters by request type", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_UserId", "Request history filters by user id", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_AccessKey", "Request history filters by access key", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_Enumerate_TenantScope", "Request history enumeration is tenant scoped", RequestHistoryCapturesS3TenantCredentialAndFiltersAsync),
                    Active("RequestHistoryAndReporting", "RequestHistory_DeleteSingle", "Request history delete removes a single entry", RequestHistoryRestReadEnumerateDeleteExistsAsync),
                    Planned("RequestHistoryAndReporting", "RequestHistory_PurgeOlderThanRetention", "Request history purge/retention APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_RequestsPerMinute", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_FailureRate", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_P50P95Latency", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_TopBucketsByBytes", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_TopBucketsByRequestCount", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_TopFailedRequestTypes", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_TopAccessKeys", "Reporting APIs are not implemented."),
                    Planned("RequestHistoryAndReporting", "Reporting_TenantScopeEnforced", "Reporting APIs are not implemented.")
                });
        }

        private static TestSuiteDescriptor HealthAndMaintenanceSuite()
        {
            return PlannedSuite(
                "HealthAndMaintenance",
                "Health and Maintenance Coverage",
                "Maintenance endpoints pending implementation.",
                "Health_Returns200WhenDatabaseAndStorageHealthy",
                "Health_Returns503WhenDatabaseUnreachable",
                "Health_Returns503WhenStorageNotWritable",
                "Health_ReportsFreeDisk",
                "Health_ReportsTempUploadCount",
                "Health_ReportsRequestHistoryRetention",
                "Health_ReportsLastCleanupRun",
                "Maintenance_UpdateRequestHistoryRetention",
                "Maintenance_PurgeRequestHistory",
                "Maintenance_CleanupTempUploads",
                "Maintenance_VerifyObjectRowsVsBlobFiles",
                "Maintenance_ExportConfigSummaryRedactsSecrets",
                "Maintenance_ShowMigrationStatus",
                "Maintenance_UpdateRuntimeSettingThatDoesNotRequireRestart",
                "Maintenance_UpdateSettingThatRequiresRestartIsMarked",
                "Maintenance_RejectUnauthorizedTenantAdmin");
        }

        private static TestSuiteDescriptor ProviderMatrixSuite()
        {
            return PlannedSuite(
                "ProviderMatrix",
                "Database Provider Matrix Coverage",
                "Cross-provider automated environments pending implementation.",
                "Provider_Sqlite_FirstBoot",
                "Provider_MySql_FirstBoot",
                "Provider_PostgreSql_FirstBoot",
                "Provider_SqlServer_FirstBoot",
                "Provider_Sqlite_TenantCrud",
                "Provider_MySql_TenantCrud",
                "Provider_PostgreSql_TenantCrud",
                "Provider_SqlServer_TenantCrud",
                "Provider_Sqlite_UserCredentialCrud",
                "Provider_MySql_UserCredentialCrud",
                "Provider_PostgreSql_UserCredentialCrud",
                "Provider_SqlServer_UserCredentialCrud",
                "Provider_Sqlite_BucketObjectCrud",
                "Provider_MySql_BucketObjectCrud",
                "Provider_PostgreSql_BucketObjectCrud",
                "Provider_SqlServer_BucketObjectCrud",
                "Provider_All_TenantScopedEnumeration",
                "Provider_All_AuthorizationSensitiveReads",
                "Provider_All_ConcurrentWrites",
                "Provider_All_MigrationStatus",
                "Provider_All_RequestHistoryFilters");
        }

        private static TestSuiteDescriptor SecurityAndAuditSuite()
        {
            return PlannedSuite(
                "SecurityAndAudit",
                "Security and Audit Coverage",
                "Full security audit implementation pending.",
                "Security_AdminApiKeyNeverLogged",
                "Security_AccessKeyLoggedOnlyWhereAllowed",
                "Security_SecretKeyNeverReturnedFromMetadata",
                "Security_SecretKeyShownOnceOnCreateOnly",
                "Security_CredentialDisableBlocksS3Immediately",
                "Security_CredentialRotateInvalidatesOldSecret",
                "Security_SessionTokenStoredHashed",
                "Security_CorsPreflightDoesNotBypassAuth",
                "Security_OpenApiDoesNotExposeSecrets",
                "Security_PathTraversalObjectKeyCannotEscapeStorageRoot",
                "Security_PathTraversalMultipartTempCannotEscapeTempRoot",
                "Security_InvalidTenantIdCannotInjectSql",
                "Security_InvalidSortFieldCannotInjectSql",
                "Security_AuditRecordsSensitiveAdminMutations",
                "Security_AuditRecordsDeniedRequests",
                "Security_AuditTenantScopeEnforced");
        }

        private static TestSuiteDescriptor ConcurrencyAndReliabilitySuite()
        {
            return PlannedSuite(
                "ConcurrencyAndReliability",
                "Concurrency and Reliability Coverage",
                "Load and concurrency coverage pending implementation.",
                "Concurrency_ParallelPutObjectSameBucketDifferentKeys",
                "Concurrency_ParallelPutObjectSameKeyUnversionedLastWriterConsistent",
                "Concurrency_ParallelPutObjectSameKeyVersionedCreatesDistinctVersions",
                "Concurrency_ParallelCreateBucketSameNameSameTenantOneSucceeds",
                "Concurrency_ParallelCreateBucketSameNameDifferentTenantsAllSucceed",
                "Concurrency_ParallelMultipartUploads",
                "Concurrency_ParallelCredentialAuthUpdatesLastUsedSafely",
                "Concurrency_RequestHistoryWritesDoNotBlockS3Responses",
                "Reliability_ServerRestartsPreserveData",
                "Reliability_ServerRestartsPreserveSessionsWhereConfigured",
                "Reliability_CleanupCanRunDuringRequests",
                "Reliability_DatabaseTransientFailureReturnsExpectedError",
                "Reliability_LargeObjectDoesNotBufferEntireResponseInMemory",
                "Reliability_CancellationStopsLongEnumeration");
        }

        private static TestSuiteDescriptor DockerAndBootstrapSuite()
        {
            return PlannedSuite(
                "DockerAndBootstrap",
                "Docker and Bootstrap Coverage",
                "Docker release smoke automation pending implementation.",
                "Docker_BuildServerImage",
                "Docker_BuildDashboardImage",
                "Docker_ComposeStartsServerAndDashboard",
                "Docker_ComposeHealthCheckPasses",
                "Docker_DefaultTenantUserCredentialSeeded",
                "Docker_DefaultS3CredentialCanListBuckets",
                "Docker_OpenApiAvailable",
                "Docker_DashboardLoginWorks",
                "Docker_PersistentVolumesRetainDataAfterRestart",
                "Docker_RuntimeDirectoriesCreatedWhenMissing",
                "Docker_SystemJsonGeneratedInContainerWhenMissing",
                "Docker_LogsDoNotContainSecrets",
                "Bootstrap_DefaultConfigUsesTenantDefault",
                "Bootstrap_EmptyDatabaseInitializesWithoutV2Artifacts");
        }

        private static TestSuiteDescriptor PlannedSuite(string suiteId, string displayName, string reason, params string[] caseIds)
        {
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>();

            foreach (string caseId in caseIds)
            {
                cases.Add(Planned(suiteId, caseId, reason));
            }

            return new TestSuiteDescriptor(suiteId: suiteId, displayName: displayName, cases: cases);
        }

        private static TestCaseDescriptor Planned(string suiteId, string caseId, string reason)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: caseId,
                skip: true,
                skipReason: reason,
                executeAsync: _ => Task.CompletedTask);
        }

        private static TestCaseDescriptor Active(
            string suiteId,
            string caseId,
            string displayName,
            Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: executeAsync);
        }

        private static async Task StartsRootHealthOpenApiAndAdminAuthAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            HttpResponseMessage root = await server.HttpClient.GetAsync(server.BaseUrl + "/", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, root.StatusCode, "root endpoint");

            string rootBody = await root.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(rootBody, "Less3", "root body");

            HttpResponseMessage missingKey = await server.HttpClient.GetAsync(server.BaseUrl + "/admin/health", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Unauthorized, missingKey.StatusCode, "admin health without API key");

            using HttpRequestMessage invalidKeyRequest = new HttpRequestMessage(HttpMethod.Get, server.BaseUrl + "/admin/health");
            invalidKeyRequest.Headers.Add("x-api-key", "wrong-admin-key");
            HttpResponseMessage invalidKey = await server.HttpClient.SendAsync(invalidKeyRequest, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Unauthorized, invalidKey.StatusCode, "admin health with invalid API key");

            HttpResponseMessage health = await server.AdminGetAsync("health", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, health.StatusCode, "admin health");
            string healthBody = await health.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(healthBody, "\"ServerVersion\"", "health body");
            EnsureContains(healthBody, "\"UptimeSeconds\"", "health body");
            EnsureContains(healthBody, "\"DatabaseType\"", "health body");
            EnsureContains(healthBody, "\"DatabaseReachable\"", "health body");
            EnsureContains(healthBody, "\"StoragePath\"", "health body");
            EnsureContains(healthBody, "\"StoragePathWritable\"", "health body");
            EnsureContains(healthBody, "\"FreeDiskBytes\"", "health body");
            EnsureContains(healthBody, "\"TempPath\"", "health body");
            EnsureContains(healthBody, "\"TempUploadCount\"", "health body");
            EnsureContains(healthBody, "\"RequestHistoryRetentionDays\"", "health body");
            EnsureContains(healthBody, "\"LastCleanupRunUtc\"", "health body");
            EnsureContains(healthBody, "\"GeneratedUtc\"", "health body");

            HttpResponseMessage openApi = await server.HttpClient.GetAsync(server.BaseUrl + "/openapi.json", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, openApi.StatusCode, "openapi");
            string openApiBody = await openApi.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(openApiBody, "\"openapi\"", "openapi body");
            EnsureContains(openApiBody, "/admin/health", "openapi body");
            EnsureContains(openApiBody, "/api/v1/{type}", "openapi body");
            EnsureContains(openApiBody, "/api/v1/objects", "openapi body");
            EnsureContains(openApiBody, "/api/v1/roleassignments", "openapi body");
            EnsureContains(openApiBody, "/api/v1/authsessions/login", "openapi body");
        }

        private static async Task AdminBootstrapCredentialAndS3ListBucketsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string userId = "usr_live_smoke";
            string credentialId = "crd_live_smoke";

            string userJson = JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = "default",
                Name = "Live Smoke User",
                Email = "live-smoke@example.com",
                PasswordHash = "password",
                Active = true
            });

            HttpResponseMessage userResponse = await server.AdminPostAsync("users", userJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userResponse.StatusCode, "create user");

            string credentialJson = JsonSerializer.Serialize(new
            {
                Id = credentialId,
                TenantId = "default",
                UserId = userId,
                Description = "Live smoke credential",
                AccessKey = server.AccessKey,
                SecretKey = server.SecretKey,
                IsBase64 = false,
                Active = true
            });

            HttpResponseMessage credentialResponse = await server.AdminPostAsync("credentials", credentialJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialResponse.StatusCode, "create credential");

            HttpResponseMessage assignmentResponse = await server.RestPostAsync("roleassignments?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = TestIds.Assignment(),
                TenantId = "default",
                RoleId = "rol_builtin_tenantadmin",
                PrincipalType = "User",
                PrincipalId = userId,
                ResourceType = "Tenant",
                ResourceId = "default",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, assignmentResponse.StatusCode, "assign live smoke tenant admin role");

            ListBucketsResponse buckets = await server.S3Client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, buckets.HttpStatusCode, "S3 ListBuckets");
        }

        private static async Task ContainerBootstrapDefaultCredentialAndS3ListBucketsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer(
                simulateContainerEnvironment: true,
                omitSystemJson: true);
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            HttpResponseMessage usersResponse = await server.AdminGetAsync("users", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, usersResponse.StatusCode, "bootstrap users");
            string usersBody = await usersResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(usersBody, "\"Id\": \"usr_default_admin\"", "bootstrap users");
            EnsureContains(usersBody, "\"TenantId\": \"default\"", "bootstrap users");
            EnsureContains(usersBody, "\"Email\": \"admin@less3\"", "bootstrap users");

            HttpResponseMessage credentialsResponse = await server.AdminGetAsync("credentials", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, credentialsResponse.StatusCode, "bootstrap credentials");
            string credentialsBody = await credentialsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(credentialsBody, "\"Id\": \"crd_default\"", "bootstrap credentials");
            EnsureContains(credentialsBody, "\"TenantId\": \"default\"", "bootstrap credentials");
            EnsureContains(credentialsBody, "\"UserId\": \"usr_default_admin\"", "bootstrap credentials");
            EnsureContains(credentialsBody, "\"AccessKey\": \"default\"", "bootstrap credentials");

            using Amazon.S3.IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            ListBucketsResponse buckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, buckets.HttpStatusCode, "bootstrap S3 ListBuckets");
        }

        private static Task PrettyIdGeneratesTenantIdWithPrefixAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateTenantId(), "ten_", "tenant ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesUserIdWithPrefixAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateUserId(), "usr_", "user ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesCredentialIdWithPrefixAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateCredentialId(), "crd_", "credential ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesBucketIdWithPrefixAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateBucketId(), "bkt_", "bucket ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesObjectIdWithPrefixAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateObjectId(), "obj_", "object ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesUploadAndPartIdsWithPrefixesAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateUploadId(), "upl_", "upload ID");
            EnsureId(IdGenerator.GenerateUploadPartId(), "prt_", "upload part ID");
            return Task.CompletedTask;
        }

        private static Task PrettyIdGeneratesRbacSessionAuditIdsWithPrefixesAndMaxLengthAsync(CancellationToken cancellationToken)
        {
            EnsureId(IdGenerator.GenerateBucketTagId(), "btg_", "bucket tag ID");
            EnsureId(IdGenerator.GenerateObjectTagId(), "otg_", "object tag ID");
            EnsureId(IdGenerator.GenerateBucketAclId(), "bac_", "bucket ACL ID");
            EnsureId(IdGenerator.GenerateObjectAclId(), "oac_", "object ACL ID");
            EnsureId(IdGenerator.GenerateRoleId(), "rol_", "role ID");
            EnsureId(IdGenerator.GeneratePermissionId(), "per_", "permission ID");
            EnsureId(IdGenerator.GenerateAssignmentId(), "asn_", "assignment ID");
            EnsureId(IdGenerator.GenerateSessionId(), "ses_", "session ID");
            EnsureId(IdGenerator.GenerateAuthorizationAuditId(), "aud_", "authorization audit ID");
            EnsureId(IdGenerator.GenerateRequestHistoryId(), "req_", "request history ID");
            return Task.CompletedTask;
        }

        private static async Task PrettyIdIsKSortableAcrossSequentialGenerationAsync(CancellationToken cancellationToken)
        {
            string first = IdGenerator.GenerateRequestHistoryId();
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            string second = IdGenerator.GenerateRequestHistoryId();

            if (String.CompareOrdinal(first, second) >= 0)
            {
                throw new InvalidOperationException("K-sortable IDs did not sort in generation order.");
            }
        }

        private static Task PrettyIdGeneratedIdsAreUniqueAcrossLargeSampleAsync(CancellationToken cancellationToken)
        {
            HashSet<string> ids = new HashSet<string>();

            for (int i = 0; i < 512; i++)
            {
                string id = IdGenerator.GenerateObjectId();
                if (!ids.Add(id))
                {
                    throw new InvalidOperationException("Duplicate PrettyID generated: " + id);
                }
            }

            return Task.CompletedTask;
        }

        private static Task PublicContractsExposeStringIdForTenantOwnedModelsAsync(CancellationToken cancellationToken)
        {
            foreach (Type type in TenantOwnedContractTypes())
            {
                PropertyInfo property = GetPublicProperty(type, "Id");
                if (property.PropertyType != typeof(string))
                {
                    throw new InvalidOperationException(type.FullName + ".Id was not a string.");
                }
            }

            return Task.CompletedTask;
        }

        private static Task PublicContractsExposeTenantIdOnTenantOwnedModelsAsync(CancellationToken cancellationToken)
        {
            foreach (Type type in TenantOwnedContractTypes())
            {
                PropertyInfo property = GetPublicProperty(type, "TenantId");
                if (property.PropertyType != typeof(string))
                {
                    throw new InvalidOperationException(type.FullName + ".TenantId was not a string.");
                }
            }

            return Task.CompletedTask;
        }

        private static Task PublicContractsDoNotSerializeDatabaseIntegerIdsAsync(CancellationToken cancellationToken)
        {
            foreach (Type type in TenantOwnedContractTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.Name.EndsWith("Id", StringComparison.Ordinal)
                        && IsIntegerType(property.PropertyType))
                    {
                        throw new InvalidOperationException(type.FullName + "." + property.Name + " exposes an integer identifier.");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static Task PublicContractsDoNotSerializeLegacyGuidPropertiesAsync(CancellationToken cancellationToken)
        {
            foreach (Type type in PublicContractTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.PropertyType == typeof(Guid)
                        || property.Name.Contains("Guid", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Contains("GUID", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(type.FullName + "." + property.Name + " exposes a legacy GUID contract.");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static Task DashboardTypesUseIdAndTenantIdAsync(CancellationToken cancellationToken)
        {
            string root = FindRepositoryRoot();
            AssertNoRegexInFiles(
                Path.Combine(root, "dashboard", "src"),
                new Regex(@"\b(guid|Guid|GUID)\b", RegexOptions.Compiled),
                "dashboard GUID contract scan");

            AssertAtLeastOneRegexInFiles(
                Path.Combine(root, "dashboard", "src"),
                new Regex(@"\btenantId\b|\bTenantId\b", RegexOptions.Compiled),
                "dashboard TenantId contract scan");

            return Task.CompletedTask;
        }

        private static async Task OpenApiSchemasUseIdAndTenantIdAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            HttpResponseMessage openApi = await server.HttpClient.GetAsync(server.BaseUrl + "/openapi.json", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, openApi.StatusCode, "openapi identifier contract");
            string body = await openApi.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(body, "TenantId", "openapi identifier contract");
            EnsureNotContains(body, "Guid", "openapi identifier contract");
            EnsureNotContains(body, "GUID", "openapi identifier contract");
            EnsureNotContains(body, "guid", "openapi identifier contract");
        }

        private static Task RequestHistoryUsesRequestIdAndTenantIdAsync(CancellationToken cancellationToken)
        {
            PropertyInfo id = GetPublicProperty(typeof(Less3.Classes.RequestHistory), "Id");
            PropertyInfo tenantId = GetPublicProperty(typeof(Less3.Classes.RequestHistory), "TenantId");

            if (id.PropertyType != typeof(string))
            {
                throw new InvalidOperationException("RequestHistory.Id was not a string.");
            }

            if (tenantId.PropertyType != typeof(string))
            {
                throw new InvalidOperationException("RequestHistory.TenantId was not a string.");
            }

            return Task.CompletedTask;
        }

        private static Task BlobFilenamesDoNotUseGuidShapedNamesAsync(CancellationToken cancellationToken)
        {
            string root = FindRepositoryRoot();
            AssertNoRegexInFiles(
                Path.Combine(root, "src", "Less3"),
                new Regex(@"Guid\.NewGuid|new\s+Guid|System\.Guid", RegexOptions.Compiled),
                "blob filename GUID generation scan");

            return Task.CompletedTask;
        }

        private static Task NoGuidGenerationRemainsAbsentInServerCodeAsync(CancellationToken cancellationToken)
        {
            string root = FindRepositoryRoot();
            AssertNoRegexInFiles(
                Path.Combine(root, "src", "Less3"),
                new Regex(@"Guid\.NewGuid|new\s+Guid|System\.Guid", RegexOptions.Compiled),
                "server GUID generation scan");

            return Task.CompletedTask;
        }

        private static Task NoGuidNamedRoutesRemainInV3ApiAsync(CancellationToken cancellationToken)
        {
            string root = FindRepositoryRoot();
            AssertNoRegexInFiles(
                Path.Combine(root, "src", "Less3", "Api"),
                new Regex(@"\b(guid|Guid|GUID)\b", RegexOptions.Compiled),
                "API GUID route scan");

            return Task.CompletedTask;
        }

        private static Task NoGuidNamedDatabaseMethodsRemainInV3InterfacesAsync(CancellationToken cancellationToken)
        {
            string root = FindRepositoryRoot();
            AssertNoRegexInFiles(
                Path.Combine(root, "src", "Less3", "Database", "Interfaces"),
                new Regex(@"\b(guid|Guid|GUID)\b", RegexOptions.Compiled),
                "database interface GUID scan");

            return Task.CompletedTask;
        }

        private static async Task FirstBootSeedsDefaultTenantAndRbacRestSurfaceAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            HttpResponseMessage tenantResponse = await server.RestGetAsync("tenants/default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, tenantResponse.StatusCode, "REST read default tenant");
            string tenantBody = await tenantResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(tenantBody, "\"Id\": \"default\"", "REST default tenant");
            EnsureContains(tenantBody, "\"Name\": \"Default\"", "REST default tenant");

            HttpResponseMessage roleResponse = await server.RestGetAsync("roles/rol_builtin_tenantadmin?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, roleResponse.StatusCode, "REST read built-in role");
            string roleBody = await roleResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(roleBody, "\"Name\": \"TenantAdmin\"", "REST built-in role");

            foreach (KeyValuePair<string, string> role in new Dictionary<string, string>
            {
                { "rol_builtin_securityadmin", "SecurityAdmin" },
                { "rol_builtin_auditor", "Auditor" },
                { "rol_builtin_operator", "Operator" },
                { "rol_builtin_tenantmember", "TenantMember" }
            })
            {
                HttpResponseMessage seededRoleResponse = await server.RestGetAsync("roles/" + role.Key + "?tenantId=default", cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, seededRoleResponse.StatusCode, "REST read built-in role " + role.Value);
                string seededRoleBody = await seededRoleResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                EnsureContains(seededRoleBody, "\"Name\": \"" + role.Value + "\"", "REST built-in role " + role.Value);
            }

            HttpResponseMessage permissionResponse = await server.RestGetAsync("permissions/per_builtin_tenantadmin_all?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, permissionResponse.StatusCode, "REST read built-in permission");
            string permissionBody = await permissionResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(permissionBody, "\"RoleId\": \"rol_builtin_tenantadmin\"", "REST built-in permission");

            foreach (KeyValuePair<string, string> permission in new Dictionary<string, string>
            {
                { "per_builtin_security_admin", "rol_builtin_securityadmin" },
                { "per_builtin_auditor_read", "rol_builtin_auditor" },
                { "per_builtin_operator_rw", "rol_builtin_operator" },
                { "per_builtin_tenantmember_read", "rol_builtin_tenantmember" }
            })
            {
                HttpResponseMessage seededPermissionResponse = await server.RestGetAsync("permissions/" + permission.Key + "?tenantId=default", cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, seededPermissionResponse.StatusCode, "REST read built-in permission " + permission.Key);
                string seededPermissionBody = await seededPermissionResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                EnsureContains(seededPermissionBody, "\"RoleId\": \"" + permission.Value + "\"", "REST built-in permission " + permission.Key);
            }

            HttpResponseMessage assignmentResponse = await server.RestGetAsync("roleassignments/asn_default_tenantadmin?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, assignmentResponse.StatusCode, "REST read default assignment");
            string assignmentBody = await assignmentResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(assignmentBody, "\"PrincipalId\": \"usr_default_admin\"", "REST default assignment");

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            ListBucketsResponse buckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, buckets.HttpStatusCode, "default first-boot S3 ListBuckets");
        }

        private static async Task AuthSessionLoginValidateAndRevokeAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string loginJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Email = "admin@less3",
                Password = "password",
                ExpirationMinutes = 30
            });

            HttpResponseMessage loginResponse = await server.RestPostUnauthenticatedAsync("authsessions/login", loginJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, loginResponse.StatusCode, "REST session login");
            string loginBody = await loginResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(loginBody, "\"Token\"", "REST session login");
            EnsureContains(loginBody, "\"TenantId\": \"default\"", "REST session login");
            EnsureContains(loginBody, "\"PrincipalId\": \"usr_default_admin\"", "REST session login");
            EnsureNotContains(loginBody, "TokenHash", "REST session login");

            string token = ExtractString(loginBody, "Token", "REST session login token");
            if (String.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("REST session login token was empty.");
            }

            string tokenJson = JsonSerializer.Serialize(new
            {
                Token = token
            });

            HttpResponseMessage validateResponse = await server.RestPostUnauthenticatedAsync("authsessions/validate", tokenJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, validateResponse.StatusCode, "REST session validate");
            string validateBody = await validateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(validateBody, "\"Valid\": true", "REST session validate");
            EnsureContains(validateBody, "\"TenantId\": \"default\"", "REST session validate");
            EnsureNotContains(validateBody, "TokenHash", "REST session validate");

            HttpResponseMessage revokeResponse = await server.RestPostUnauthenticatedAsync("authsessions/revoke", tokenJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, revokeResponse.StatusCode, "REST session revoke");
            EnsureContains(await revokeResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Valid\": false", "REST session revoke");

            HttpResponseMessage revokedValidateResponse = await server.RestPostUnauthenticatedAsync("authsessions/validate", tokenJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Unauthorized, revokedValidateResponse.StatusCode, "REST revoked session validate");
            EnsureContains(await revokedValidateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Valid\": false", "REST revoked session validate");

            string badLoginJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Email = "admin@less3",
                Password = "wrong-password"
            });

            HttpResponseMessage badLoginResponse = await server.RestPostUnauthenticatedAsync("authsessions/login", badLoginJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Unauthorized, badLoginResponse.StatusCode, "REST invalid session login");

            HttpResponseMessage historyResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 100,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "method", "POST" }
                }
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, historyResponse.StatusCode, "REST request history secret redaction");
            string historyBody = await historyResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(historyBody, "[redacted]", "REST request history secret redaction");
            EnsureNotContains(historyBody, token, "REST request history session token redaction");
            EnsureNotContains(historyBody, "\"Password\": \"password\"", "REST request history password redaction");
            EnsureNotContains(historyBody, "\"Password\": \"wrong-password\"", "REST request history invalid password redaction");
        }

        private static async Task AuthSessionRestReadEnumerateRevokeExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string loginJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Email = "admin@less3",
                Password = "password",
                ExpirationMinutes = 30
            });

            HttpResponseMessage loginResponse = await server.RestPostUnauthenticatedAsync("authsessions/login", loginJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, loginResponse.StatusCode, "REST auth session create");
            string loginBody = await loginResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string sessionId = ExtractNestedString(loginBody, "Session", "Id", "REST auth session login");

            HttpResponseMessage readResponse = await server.RestGetAsync("authsessions/" + sessionId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST auth session read");
            string readBody = await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(readBody, sessionId, "REST auth session read");
            EnsureContains(readBody, "\"PrincipalId\": \"usr_default_admin\"", "REST auth session read");
            EnsureNotContains(readBody, "TokenHash", "REST auth session read");

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("authsessions/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 10,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "principalId", "usr_default_admin" }
                }
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST auth session enumerate");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), sessionId, "REST auth session enumerate");

            HttpResponseMessage existsResponse = await server.RestPostAsync("authsessions/exists?id=" + sessionId + "&tenantId=default", "{}", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST auth session exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST auth session exists");

            HttpResponseMessage revokeResponse = await server.RestDeleteAsync("authsessions/" + sessionId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, revokeResponse.StatusCode, "REST auth session revoke by id");

            HttpResponseMessage revokedReadResponse = await server.RestGetAsync("authsessions/" + sessionId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, revokedReadResponse.StatusCode, "REST auth session read revoked");
            string revokedReadBody = await revokedReadResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(revokedReadBody, "\"Active\": false", "REST auth session revoked");
            EnsureContains(revokedReadBody, "\"RevokedUtc\":", "REST auth session revoked");
        }

        private static async Task AuthorizationAuditRestReadEnumerateDeleteExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string auditId = TestIds.AuthorizationAudit();
            HttpResponseMessage createResponse = await server.RestPostAsync("authorizationaudit?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = auditId,
                TenantId = "default",
                UserId = "usr_default_admin",
                ResourceType = "Tenant",
                ResourceId = "default",
                Operation = "Read",
                Permitted = true,
                Reason = "Live REST surface coverage"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, createResponse.StatusCode, "REST authorization audit create");

            HttpResponseMessage readResponse = await server.RestGetAsync("authorizationaudit/" + auditId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST authorization audit read");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), auditId, "REST authorization audit read");

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("authorizationaudit/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 10,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "userId", "usr_default_admin" },
                    { "resourceType", "Tenant" },
                    { "operation", "Read" }
                }
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST authorization audit enumerate");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), auditId, "REST authorization audit enumerate");

            HttpResponseMessage existsResponse = await server.RestPostAsync("authorizationaudit/exists?id=" + auditId + "&tenantId=default", "{}", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST authorization audit exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST authorization audit exists");

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync("authorizationaudit/" + auditId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST authorization audit delete");

            HttpResponseMessage missingExistsResponse = await server.RestPostAsync("authorizationaudit/exists?id=" + auditId + "&tenantId=default", "{}", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingExistsResponse.StatusCode, "REST authorization audit missing exists");
            EnsureContains(await missingExistsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST authorization audit missing exists");
        }

        private static async Task RequestHistoryRestReadEnumerateDeleteExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            ListBucketsResponse buckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, buckets.HttpStatusCode, "REST request history seed ListBuckets");

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 10,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "requestType", "ListBuckets" },
                    { "accessKey", "default" }
                }
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST request history enumerate for delete");
            string enumerateBody = await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string requestHistoryId = ExtractFirstEnumerationItemString(enumerateBody, "Id", "REST request history enumerate for delete");

            HttpResponseMessage readResponse = await server.RestGetAsync("requesthistory/" + requestHistoryId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST request history read");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), requestHistoryId, "REST request history read");

            HttpResponseMessage existsResponse = await server.RestPostAsync("requesthistory/exists?id=" + requestHistoryId + "&tenantId=default", "{}", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST request history exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST request history exists");

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync("requesthistory/" + requestHistoryId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST request history delete");

            HttpResponseMessage missingExistsResponse = await server.RestPostAsync("requesthistory/exists?id=" + requestHistoryId + "&tenantId=default", "{}", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingExistsResponse.StatusCode, "REST request history missing exists");
            EnsureContains(await missingExistsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST request history missing exists");
        }

        private static async Task RestBearerSessionEnforcesRbacPermitAndDenyAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string adminToken = await LoginAndExtractTokenAsync(server, "default", "admin@less3", "password", cancellationToken).ConfigureAwait(false);
            HttpResponseMessage adminTenantRead = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/default", adminToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, adminTenantRead.StatusCode, "admin bearer tenant read");
            EnsureContains(await adminTenantRead.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Id\": \"default\"", "admin bearer tenant read");

            string userId = TestIds.User();
            string userEmail = userId + "@example.com";

            HttpResponseMessage userResponse = await server.RestPostAsync("users?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = "default",
                Name = "Unprivileged REST user",
                Email = userEmail,
                PasswordHash = "password",
                IsAdmin = false,
                IsTenantAdmin = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userResponse.StatusCode, "create unprivileged REST user");

            string userToken = await LoginAndExtractTokenAsync(server, "default", userEmail, "password", cancellationToken).ConfigureAwait(false);

            HttpResponseMessage deniedRead = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/default", userToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, deniedRead.StatusCode, "unassigned bearer tenant read");

            string assignmentId = TestIds.Assignment();
            HttpResponseMessage assignmentResponse = await server.RestPostAsync("roleassignments?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = assignmentId,
                TenantId = "default",
                RoleId = "rol_builtin_tenantmember",
                PrincipalType = "User",
                PrincipalId = userId,
                ResourceType = "Tenant",
                ResourceId = "default",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, assignmentResponse.StatusCode, "assign TenantMember to REST user");

            HttpResponseMessage allowedRead = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/default", userToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, allowedRead.StatusCode, "assigned bearer tenant read");

            string denyRoleId = TestIds.Role();
            string denyPermissionId = TestIds.Permission();
            string denyAssignmentId = TestIds.Assignment();

            HttpResponseMessage denyRoleResponse = await server.RestPostAsync("roles?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = denyRoleId,
                TenantId = "default",
                Name = "Deny tenant read",
                Description = "Created by live RBAC denial test",
                InheritsToChildren = true,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, denyRoleResponse.StatusCode, "create deny role");

            HttpResponseMessage denyPermissionResponse = await server.RestPostAsync("permissions?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = denyPermissionId,
                TenantId = "default",
                RoleId = denyRoleId,
                ResourceType = "Tenant",
                Operation = "Read",
                Permit = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, denyPermissionResponse.StatusCode, "create deny permission");

            HttpResponseMessage denyAssignmentResponse = await server.RestPostAsync("roleassignments?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = denyAssignmentId,
                TenantId = "default",
                RoleId = denyRoleId,
                PrincipalType = "User",
                PrincipalId = userId,
                ResourceType = "Tenant",
                ResourceId = "default",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, denyAssignmentResponse.StatusCode, "assign deny role");

            HttpResponseMessage deniedAfterExplicitDeny = await SendBearerRestAsync(server, HttpMethod.Get, "tenants/default", userToken, null, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, deniedAfterExplicitDeny.StatusCode, "explicit deny overrides tenant read permit");

            string blockedCreateJson = JsonSerializer.Serialize(new
            {
                Id = TestIds.Tenant(),
                Name = "Blocked bearer tenant",
                Active = true
            });

            HttpResponseMessage blockedCreate = await SendBearerRestAsync(server, HttpMethod.Post, "tenants", userToken, blockedCreateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Forbidden, blockedCreate.StatusCode, "assigned bearer tenant create remains denied");

            HttpResponseMessage auditResponse = await server.RestPostAsync("authorizationaudit/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 1000,
                Offset = 0
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, auditResponse.StatusCode, "authorization audit enumerate");
            string auditBody = await auditResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(auditBody, userId, "authorization audit user id");
            EnsureContains(auditBody, "\"Permitted\": false", "authorization audit deny");
            EnsureContains(auditBody, "\"Permitted\": true", "authorization audit permit");
        }

        private static async Task InactiveTenantBlocksLoginAndS3CredentialAuthAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "inactive-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            string updateJson = JsonSerializer.Serialize(new
            {
                Id = tenantId,
                Name = "Inactive tenant",
                Active = false
            });

            HttpResponseMessage updateResponse = await server.RestPutAsync("tenants/" + tenantId, updateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateResponse.StatusCode, "deactivate tenant");

            string loginJson = JsonSerializer.Serialize(new
            {
                TenantId = tenantId,
                Email = userId + "@example.com",
                Password = "password"
            });

            HttpResponseMessage loginResponse = await server.RestPostUnauthenticatedAsync("authsessions/login", loginJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Unauthorized, loginResponse.StatusCode, "inactive tenant session login");

            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);
            await EnsureS3FailureAsync(
                () => tenantClient.ListBucketsAsync(cancellationToken),
                "inactive tenant S3 ListBuckets").ConfigureAwait(false);
        }

        private static async Task Less3RestTenantCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string createJson = JsonSerializer.Serialize(new
            {
                Id = tenantId,
                Name = "REST tenant",
                Active = true
            });

            HttpResponseMessage createResponse = await server.RestPostAsync("tenants", createJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, createResponse.StatusCode, "REST create tenant");

            HttpResponseMessage readResponse = await server.RestGetAsync("tenants/" + tenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST read tenant");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), tenantId, "REST read tenant");

            HttpResponseMessage existsResponse = await server.RestGetAsync("tenants/" + tenantId + "/exists", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST tenant exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST tenant exists");

            string enumerateJson = JsonSerializer.Serialize(new
            {
                Limit = 100,
                Offset = 0,
                SortField = "id"
            });

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("tenants/enumerate", enumerateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST enumerate tenants");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), tenantId, "REST enumerate tenants");

            string updateJson = JsonSerializer.Serialize(new
            {
                Id = tenantId,
                Name = "REST tenant updated",
                Active = false
            });

            HttpResponseMessage updateResponse = await server.RestPutAsync("tenants/" + tenantId, updateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateResponse.StatusCode, "REST update tenant");
            EnsureContains(await updateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Active\": false", "REST update tenant");

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync("tenants/" + tenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST delete tenant");

            HttpResponseMessage missingResponse = await server.RestGetAsync("tenants/" + tenantId + "/exists", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingResponse.StatusCode, "REST tenant missing exists");
            EnsureContains(await missingResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST tenant missing exists");
        }

        private static async Task Less3RestBucketCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string bucketId = TestIds.Bucket();
            string bucketName = "rest-buckets-" + TestIds.Suffix().Substring(0, 8);

            string createJson = JsonSerializer.Serialize(new
            {
                Id = bucketId,
                TenantId = "default",
                OwnerId = "usr_default_admin",
                Name = bucketName,
                RegionString = "us-west-1",
                EnableVersioning = false,
                EnablePublicWrite = false,
                EnablePublicRead = false
            });

            HttpResponseMessage createResponse = await server.RestPostAsync("buckets?tenantId=default", createJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, createResponse.StatusCode, "REST create bucket");

            HttpResponseMessage readResponse = await server.RestGetAsync("buckets/" + bucketId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST read bucket");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), bucketName, "REST read bucket");

            HttpResponseMessage existsResponse = await server.RestGetAsync("buckets/" + bucketId + "/exists?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST bucket exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST bucket exists");

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("buckets/enumerate?tenantId=default", JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 100,
                Offset = 0,
                SortField = "id"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST enumerate buckets");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), bucketId, "REST enumerate buckets");

            string updateJson = JsonSerializer.Serialize(new
            {
                Id = bucketId,
                TenantId = "default",
                OwnerId = "usr_default_admin",
                Name = bucketName,
                RegionString = "us-west-2",
                EnableVersioning = true,
                EnablePublicWrite = false,
                EnablePublicRead = false
            });

            HttpResponseMessage updateResponse = await server.RestPutAsync("buckets/" + bucketId + "?tenantId=default", updateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateResponse.StatusCode, "REST update bucket");
            EnsureContains(await updateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"EnableVersioning\": true", "REST update bucket");

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync("buckets/" + bucketId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST delete bucket");

            HttpResponseMessage missingResponse = await server.RestGetAsync("buckets/" + bucketId + "/exists?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingResponse.StatusCode, "REST missing bucket exists");
            EnsureContains(await missingResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST missing bucket exists");
        }

        private static async Task Less3RestUserAndCredentialCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "rest-cred-" + TestIds.Suffix();

            HttpResponseMessage userCreateResponse = await server.RestPostAsync("users?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = "default",
                Name = "REST user",
                Email = userId + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userCreateResponse.StatusCode, "REST create user");

            await AssertRestCrudRoundTripAsync(
                server,
                "users",
                userId,
                "tenantId=default",
                "user",
                JsonSerializer.Serialize(new
                {
                    Id = userId,
                    TenantId = "default",
                    Name = "REST user updated",
                    Email = userId + "@example.com",
                    PasswordHash = "password",
                    Active = true
                }),
                "REST user updated",
                cancellationToken).ConfigureAwait(false);

            HttpResponseMessage replacementUserResponse = await server.RestPostAsync("users?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = "default",
                Name = "REST user",
                Email = userId + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, replacementUserResponse.StatusCode, "REST recreate user for credential");

            HttpResponseMessage credentialCreateResponse = await server.RestPostAsync("credentials?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = credentialId,
                TenantId = "default",
                UserId = userId,
                Description = "REST credential",
                AccessKey = accessKey,
                SecretKey = "secret-" + TestIds.Suffix(),
                IsBase64 = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialCreateResponse.StatusCode, "REST create credential");

            await AssertRestCrudRoundTripAsync(
                server,
                "credentials",
                credentialId,
                "tenantId=default",
                "credential",
                JsonSerializer.Serialize(new
                {
                    Id = credentialId,
                    TenantId = "default",
                    UserId = userId,
                    Description = "REST credential updated",
                    AccessKey = accessKey,
                    SecretKey = "secret-updated-" + TestIds.Suffix(),
                    IsBase64 = false,
                    Active = false
                }),
                "REST credential updated",
                cancellationToken).ConfigureAwait(false);

            HttpResponseMessage userDeleteResponse = await server.RestDeleteAsync("users/" + userId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, userDeleteResponse.StatusCode, "REST delete user after credential");
        }

        private static async Task Less3RestRbacCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string roleId = TestIds.Role();
            string permissionId = TestIds.Permission();
            string assignmentId = TestIds.Assignment();

            string roleJson = JsonSerializer.Serialize(new
            {
                Id = roleId,
                TenantId = "default",
                Name = "REST custom role",
                Description = "Created by live REST test",
                InheritsToChildren = true,
                Active = true
            });

            HttpResponseMessage roleCreateResponse = await server.RestPostAsync("roles?tenantId=default", roleJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, roleCreateResponse.StatusCode, "REST create role");

            string permissionJson = JsonSerializer.Serialize(new
            {
                Id = permissionId,
                TenantId = "default",
                RoleId = roleId,
                ResourceType = "Bucket",
                Operation = "Read",
                Permit = true,
                Active = true
            });

            HttpResponseMessage permissionCreateResponse = await server.RestPostAsync("permissions?tenantId=default", permissionJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, permissionCreateResponse.StatusCode, "REST create permission");

            string assignmentJson = JsonSerializer.Serialize(new
            {
                Id = assignmentId,
                TenantId = "default",
                RoleId = roleId,
                PrincipalType = "User",
                PrincipalId = "usr_default_admin",
                ResourceType = "Tenant",
                ResourceId = "default",
                Active = true
            });

            HttpResponseMessage assignmentCreateResponse = await server.RestPostAsync("roleassignments?tenantId=default", assignmentJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, assignmentCreateResponse.StatusCode, "REST create role assignment");

            HttpResponseMessage readResponse = await server.RestGetAsync("roles/" + roleId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST read role");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), roleId, "REST read role");

            HttpResponseMessage existsResponse = await server.RestGetAsync("permissions/" + permissionId + "/exists?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST permission exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST permission exists");

            string enumerateJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 100,
                Offset = 0,
                SortField = "name"
            });

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("roles/enumerate", enumerateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST enumerate roles");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), roleId, "REST enumerate roles");

            string roleUpdateJson = JsonSerializer.Serialize(new
            {
                Id = roleId,
                TenantId = "default",
                Name = "REST custom role updated",
                Description = "Updated by live REST test",
                InheritsToChildren = true,
                Active = true
            });

            HttpResponseMessage roleUpdateResponse = await server.RestPutAsync("roles/" + roleId + "?tenantId=default", roleUpdateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, roleUpdateResponse.StatusCode, "REST update role");
            EnsureContains(await roleUpdateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "REST custom role updated", "REST update role");

            HttpResponseMessage assignmentDeleteResponse = await server.RestDeleteAsync("roleassignments/" + assignmentId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, assignmentDeleteResponse.StatusCode, "REST delete role assignment");

            HttpResponseMessage permissionDeleteResponse = await server.RestDeleteAsync("permissions/" + permissionId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, permissionDeleteResponse.StatusCode, "REST delete permission");

            HttpResponseMessage roleDeleteResponse = await server.RestDeleteAsync("roles/" + roleId + "?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, roleDeleteResponse.StatusCode, "REST delete role");
        }

        private static async Task Less3RestObjectCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string bucketId = TestIds.Bucket();
            string objectId = TestIds.Object();
            string bucketName = "rest-objects-" + TestIds.Suffix().Substring(0, 8);

            string bucketJson = JsonSerializer.Serialize(new
            {
                Id = bucketId,
                TenantId = "default",
                OwnerId = "usr_default_admin",
                Name = bucketName,
                RegionString = "us-west-1"
            });

            HttpResponseMessage bucketResponse = await server.RestPostAsync("buckets?tenantId=default", bucketJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, bucketResponse.StatusCode, "REST create object test bucket");

            string createJson = JsonSerializer.Serialize(new
            {
                Id = objectId,
                TenantId = "default",
                BucketId = bucketId,
                OwnerId = "usr_default_admin",
                AuthorId = "usr_default_admin",
                Key = "notes/rest-object.txt",
                ContentType = "text/plain",
                ContentLength = 12,
                Version = 1,
                Etag = "",
                BlobFilename = "",
                IsFolder = false,
                DeleteMarker = false,
                Md5 = "",
                Metadata = "{\"source\":\"rest\"}"
            });

            HttpResponseMessage createResponse = await server.RestPostAsync("objects?tenantId=default&bucketId=" + bucketId, createJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, createResponse.StatusCode, "REST create object");
            EnsureContains(await createResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"TenantId\": \"default\"", "REST create object");

            HttpResponseMessage readResponse = await server.RestGetAsync("objects/" + objectId + "?tenantId=default&bucketId=" + bucketId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST read object");
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Key\": \"notes/rest-object.txt\"", "REST read object");

            HttpResponseMessage existsResponse = await server.RestGetAsync("objects/" + objectId + "/exists?tenantId=default&bucketId=" + bucketId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST object exists");
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST object exists");

            string enumerateJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 20,
                Offset = 0,
                SortField = "id",
                Filters = new Dictionary<string, string>
                {
                    { "prefix", "notes/" }
                }
            });

            HttpResponseMessage enumerateResponse = await server.RestPostAsync("objects/enumerate?tenantId=default&bucketId=" + bucketId, enumerateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST enumerate objects");
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), objectId, "REST enumerate objects");

            string updateJson = JsonSerializer.Serialize(new
            {
                Id = objectId,
                TenantId = "default",
                BucketId = bucketId,
                OwnerId = "usr_default_admin",
                AuthorId = "usr_default_admin",
                Key = "notes/rest-object-updated.txt",
                ContentType = "text/plain",
                ContentLength = 24,
                Version = 1,
                Etag = "",
                BlobFilename = "",
                IsFolder = false,
                DeleteMarker = false,
                Md5 = "",
                Metadata = "{\"source\":\"rest\",\"updated\":true}"
            });

            HttpResponseMessage updateResponse = await server.RestPutAsync("objects/" + objectId + "?tenantId=default&bucketId=" + bucketId, updateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateResponse.StatusCode, "REST update object");
            EnsureContains(await updateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "rest-object-updated.txt", "REST update object");

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync("objects/" + objectId + "?tenantId=default&bucketId=" + bucketId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST delete object");

            HttpResponseMessage missingResponse = await server.RestGetAsync("objects/" + objectId + "/exists?tenantId=default&bucketId=" + bucketId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingResponse.StatusCode, "REST missing object exists");
            EnsureContains(await missingResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST missing object exists");
        }

        private static async Task Less3RestTagAndAclCrudEnumerateAndExistsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string bucketId = TestIds.Bucket();
            string objectId = TestIds.Object();
            string bucketName = "rest-tags-acls-" + TestIds.Suffix().Substring(0, 8);

            HttpResponseMessage bucketResponse = await server.RestPostAsync("buckets?tenantId=default", JsonSerializer.Serialize(new
            {
                Id = bucketId,
                TenantId = "default",
                OwnerId = "usr_default_admin",
                Name = bucketName,
                RegionString = "us-west-1"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, bucketResponse.StatusCode, "REST create tag/ACL test bucket");

            HttpResponseMessage objectResponse = await server.RestPostAsync("objects?tenantId=default&bucketId=" + bucketId, JsonSerializer.Serialize(new
            {
                Id = objectId,
                TenantId = "default",
                BucketId = bucketId,
                OwnerId = "usr_default_admin",
                AuthorId = "usr_default_admin",
                Key = "rest-tag-acl-object.txt",
                ContentType = "text/plain",
                ContentLength = 3,
                Version = 1,
                Etag = "",
                BlobFilename = "",
                IsFolder = false,
                DeleteMarker = false,
                Md5 = "",
                Metadata = "{}"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, objectResponse.StatusCode, "REST create tag/ACL test object");

            string bucketTagId = TestIds.BucketTag();
            HttpResponseMessage bucketTagCreate = await server.RestPostAsync("buckettags?tenantId=default&bucketId=" + bucketId, JsonSerializer.Serialize(new
            {
                Id = bucketTagId,
                TenantId = "default",
                BucketId = bucketId,
                Key = "env",
                Value = "test"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, bucketTagCreate.StatusCode, "REST create bucket tag");

            await AssertRestCrudRoundTripAsync(
                server,
                "buckettags",
                bucketTagId,
                "tenantId=default&bucketId=" + bucketId,
                "bucket tag",
                JsonSerializer.Serialize(new
                {
                    Id = bucketTagId,
                    TenantId = "default",
                    BucketId = bucketId,
                    Key = "env",
                    Value = "updated"
                }),
                "updated",
                cancellationToken).ConfigureAwait(false);

            string objectTagId = TestIds.ObjectTag();
            HttpResponseMessage objectTagCreate = await server.RestPostAsync("objecttags?tenantId=default&bucketId=" + bucketId + "&objectId=" + objectId, JsonSerializer.Serialize(new
            {
                Id = objectTagId,
                TenantId = "default",
                BucketId = bucketId,
                ObjectId = objectId,
                Key = "state",
                Value = "new"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, objectTagCreate.StatusCode, "REST create object tag");

            await AssertRestCrudRoundTripAsync(
                server,
                "objecttags",
                objectTagId,
                "tenantId=default&bucketId=" + bucketId + "&objectId=" + objectId,
                "object tag",
                JsonSerializer.Serialize(new
                {
                    Id = objectTagId,
                    TenantId = "default",
                    BucketId = bucketId,
                    ObjectId = objectId,
                    Key = "state",
                    Value = "updated"
                }),
                "updated",
                cancellationToken).ConfigureAwait(false);

            string bucketAclId = IdGenerator.GenerateBucketAclId();
            HttpResponseMessage bucketAclCreate = await server.RestPostAsync("bucketacls?tenantId=default&bucketId=" + bucketId, JsonSerializer.Serialize(new
            {
                Id = bucketAclId,
                TenantId = "default",
                BucketId = bucketId,
                UserId = "usr_default_admin",
                IssuedByUserId = "usr_default_admin",
                PermitRead = true,
                PermitWrite = false,
                PermitReadAcp = true,
                PermitWriteAcp = false,
                FullControl = false
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, bucketAclCreate.StatusCode, "REST create bucket ACL");

            await AssertRestCrudRoundTripAsync(
                server,
                "bucketacls",
                bucketAclId,
                "tenantId=default&bucketId=" + bucketId,
                "bucket ACL",
                JsonSerializer.Serialize(new
                {
                    Id = bucketAclId,
                    TenantId = "default",
                    BucketId = bucketId,
                    UserId = "usr_default_admin",
                    IssuedByUserId = "usr_default_admin",
                    PermitRead = true,
                    PermitWrite = true,
                    PermitReadAcp = true,
                    PermitWriteAcp = false,
                    FullControl = false
                }),
                "\"PermitWrite\": true",
                cancellationToken).ConfigureAwait(false);

            string objectAclId = IdGenerator.GenerateObjectAclId();
            HttpResponseMessage objectAclCreate = await server.RestPostAsync("objectacls?tenantId=default&bucketId=" + bucketId + "&objectId=" + objectId, JsonSerializer.Serialize(new
            {
                Id = objectAclId,
                TenantId = "default",
                BucketId = bucketId,
                ObjectId = objectId,
                UserId = "usr_default_admin",
                IssuedByUserId = "usr_default_admin",
                PermitRead = true,
                PermitWrite = false,
                PermitReadAcp = true,
                PermitWriteAcp = false,
                FullControl = false
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, objectAclCreate.StatusCode, "REST create object ACL");

            await AssertRestCrudRoundTripAsync(
                server,
                "objectacls",
                objectAclId,
                "tenantId=default&bucketId=" + bucketId + "&objectId=" + objectId,
                "object ACL",
                JsonSerializer.Serialize(new
                {
                    Id = objectAclId,
                    TenantId = "default",
                    BucketId = bucketId,
                    ObjectId = objectId,
                    UserId = "usr_default_admin",
                    IssuedByUserId = "usr_default_admin",
                    PermitRead = true,
                    PermitWrite = true,
                    PermitReadAcp = true,
                    PermitWriteAcp = false,
                    FullControl = false
                }),
                "\"PermitWrite\": true",
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3TenantIsolationRejectsCrossTenantBucketAndObjectAccessAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "isolation-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string defaultBucketName = "default-private-" + TestIds.Suffix().Substring(0, 8);
            string secondBucketName = "second-private-" + TestIds.Suffix().Substring(0, 8);
            string key = "tenant-only.txt";

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 secondTenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse defaultBucket = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = defaultBucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultBucket.HttpStatusCode, "create default isolation bucket");

            PutObjectResponse defaultObject = await defaultClient.PutObjectAsync(new PutObjectRequest
            {
                BucketName = defaultBucketName,
                Key = key,
                ContentBody = "default tenant only"
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultObject.HttpStatusCode, "create default isolation object");

            PutBucketResponse secondBucket = await secondTenantClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = secondBucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, secondBucket.HttpStatusCode, "create second tenant isolation bucket");

            await EnsureS3FailureAsync(
                () => secondTenantClient.GetObjectMetadataAsync(defaultBucketName, key, cancellationToken),
                "second tenant metadata read of default bucket").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => secondTenantClient.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = defaultBucketName,
                    Key = "blocked-write.txt",
                    ContentBody = "blocked"
                }, cancellationToken),
                "second tenant write to default bucket").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => defaultClient.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = secondBucketName
                }, cancellationToken),
                "default tenant list of second tenant bucket").ConfigureAwait(false);
        }

        private static async Task S3UnauthorizedCredentialCannotCreateBucketAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "norole-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "blocked-" + TestIds.Suffix().Substring(0, 8);

            HttpResponseMessage tenantResponse = await server.RestPostAsync("tenants", JsonSerializer.Serialize(new
            {
                Id = tenantId,
                Name = "No role tenant",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, tenantResponse.StatusCode, "create no-role tenant");

            HttpResponseMessage userResponse = await server.RestPostAsync("users?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = tenantId,
                Name = "No role user",
                Email = userId + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userResponse.StatusCode, "create no-role user");

            HttpResponseMessage credentialResponse = await server.RestPostAsync("credentials?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = credentialId,
                TenantId = tenantId,
                UserId = userId,
                Description = "No role credential",
                AccessKey = accessKey,
                SecretKey = secretKey,
                IsBase64 = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialResponse.StatusCode, "create no-role credential");

            using IAmazonS3 noRoleClient = server.CreateS3Client(accessKey, secretKey);
            await EnsureS3FailureAsync(
                () => noRoleClient.ListBucketsAsync(cancellationToken),
                "no-role credential list buckets").ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => noRoleClient.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName
                }, cancellationToken),
                "no-role credential create bucket").ConfigureAwait(false);
        }

        private static async Task S3CredentialLastUsedAndLastFailedTimestampsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            ListBucketsResponse defaultBuckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultBuckets.HttpStatusCode, "default credential timestamp ListBuckets");

            HttpResponseMessage defaultCredentialResponse = await server.RestGetAsync("credentials/crd_default?tenantId=default", cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultCredentialResponse.StatusCode, "read default credential timestamp");
            string defaultCredentialBody = await defaultCredentialResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(defaultCredentialBody, "\"LastUsedUtc\":", "default credential last used");
            EnsureNotContains(defaultCredentialBody, "\"LastUsedUtc\": null", "default credential last used");

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "inactive-cred-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage updateCredentialResponse = await server.RestPutAsync("credentials/" + credentialId + "?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = credentialId,
                TenantId = tenantId,
                UserId = userId,
                Description = "Inactive timestamp credential",
                AccessKey = accessKey,
                SecretKey = secretKey,
                IsBase64 = false,
                Active = false
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateCredentialResponse.StatusCode, "deactivate timestamp credential");

            using IAmazonS3 inactiveClient = server.CreateS3Client(accessKey, secretKey);
            await EnsureS3FailureAsync(
                () => inactiveClient.ListBucketsAsync(cancellationToken),
                "inactive credential timestamp ListBuckets").ConfigureAwait(false);

            HttpResponseMessage inactiveCredentialResponse = await server.RestGetAsync("credentials/" + credentialId + "?tenantId=" + tenantId, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, inactiveCredentialResponse.StatusCode, "read inactive credential timestamp");
            string inactiveCredentialBody = await inactiveCredentialResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(inactiveCredentialBody, "\"LastFailedUtc\":", "inactive credential last failed");
            EnsureNotContains(inactiveCredentialBody, "\"LastFailedUtc\": null", "inactive credential last failed");
        }

        private static async Task RequestHistoryCapturesS3TenantCredentialAndFiltersAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            ListBucketsResponse firstBuckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, firstBuckets.HttpStatusCode, "request history first S3 ListBuckets");
            ListBucketsResponse secondBuckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, secondBuckets.HttpStatusCode, "request history second S3 ListBuckets");

            string secondTenantId = TestIds.Tenant();
            string secondUserId = TestIds.User();
            string secondCredentialId = TestIds.Credential();
            string secondAccessKey = "hist-" + TestIds.Suffix();
            string secondSecretKey = "secret-" + TestIds.Suffix();
            await CreateTenantUserAndCredentialAsync(server, secondTenantId, secondUserId, secondCredentialId, secondAccessKey, secondSecretKey, cancellationToken).ConfigureAwait(false);
            using IAmazonS3 secondTenantClient = server.CreateS3Client(secondAccessKey, secondSecretKey);
            ListBucketsResponse otherTenantBuckets = await secondTenantClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, otherTenantBuckets.HttpStatusCode, "request history other tenant S3 ListBuckets");

            string queryJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 1,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "requestType", "ListBuckets" },
                    { "accessKey", "default" }
                }
            });

            HttpResponseMessage historyResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", queryJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, historyResponse.StatusCode, "REST enumerate request history");
            string historyBody = await historyResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(historyBody, "\"Limit\": 1", "REST request history pagination");
            EnsureContains(historyBody, "\"Offset\": 0", "REST request history pagination");
            EnsureGreaterOrEqual(2, ExtractEnumerationTotal(historyBody, "REST request history pagination"), "REST request history pagination total");
            EnsureEqual(1, ExtractEnumerationItemCount(historyBody, "REST request history pagination"), "REST request history pagination item count");

            string sourceIp = ExtractFirstEnumerationItemString(historyBody, "SourceIp", "REST request history source IP");
            EnsureTrue(!String.IsNullOrWhiteSpace(sourceIp), "REST request history source IP");

            EnsureContains(historyBody, "\"TenantId\": \"default\"", "REST request history");
            EnsureContains(historyBody, "\"HttpMethod\": \"GET\"", "REST request history");
            EnsureContains(historyBody, "\"StatusCode\": 200", "REST request history");
            EnsureContains(historyBody, "\"DurationMs\":", "REST request history");
            EnsureContains(historyBody, "\"AccessKey\": \"default\"", "REST request history");
            EnsureContains(historyBody, "\"UserId\": \"usr_default_admin\"", "REST request history");
            EnsureContains(historyBody, "\"RequestType\": \"ListBuckets\"", "REST request history");
            EnsureContains(historyBody, "\"Success\": true", "REST request history");

            string fullFilterJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 10,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "method", "GET" },
                    { "status", "200" },
                    { "success", "true" },
                    { "sourceIp", sourceIp },
                    { "requestType", "ListBuckets" },
                    { "userId", "usr_default_admin" },
                    { "accessKey", "default" }
                }
            });

            HttpResponseMessage filteredHistoryResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", fullFilterJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, filteredHistoryResponse.StatusCode, "REST filtered request history");
            string filteredHistoryBody = await filteredHistoryResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureGreaterOrEqual(1, ExtractEnumerationTotal(filteredHistoryBody, "REST filtered request history"), "REST filtered request history total");
            EnsureContains(filteredHistoryBody, "\"SourceIp\": \"" + sourceIp + "\"", "REST filtered request history source IP");
            EnsureContains(filteredHistoryBody, "\"UserId\": \"usr_default_admin\"", "REST filtered request history user id");

            string windowFilterJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 10,
                Offset = 0,
                StartUtc = DateTime.UtcNow.AddMinutes(-5),
                EndUtc = DateTime.UtcNow.AddMinutes(5),
                Filters = new Dictionary<string, string>
                {
                    { "requestType", "ListBuckets" },
                    { "accessKey", "default" }
                }
            });

            HttpResponseMessage windowHistoryResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", windowFilterJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, windowHistoryResponse.StatusCode, "REST request history start/end filter");
            string windowHistoryBody = await windowHistoryResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureGreaterOrEqual(1, ExtractEnumerationTotal(windowHistoryBody, "REST request history start/end filter"), "REST request history start/end total");

            string tenantScopeJson = JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 20,
                Offset = 0,
                Filters = new Dictionary<string, string>
                {
                    { "requestType", "ListBuckets" }
                }
            });

            HttpResponseMessage tenantScopeResponse = await server.RestPostAsync("requesthistory/enumerate?tenantId=default", tenantScopeJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, tenantScopeResponse.StatusCode, "REST request history tenant scope");
            string tenantScopeBody = await tenantScopeResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            EnsureContains(tenantScopeBody, "\"TenantId\": \"default\"", "REST request history tenant scope");
            EnsureNotContains(tenantScopeBody, secondTenantId, "REST request history tenant scope");
            EnsureNotContains(tenantScopeBody, secondAccessKey, "REST request history tenant scope");
        }

        private static async Task S3SameBucketNameDifferentTenantsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "tenant2-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "shared-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 secondTenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse defaultCreate = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultCreate.HttpStatusCode, "create default tenant bucket");

            PutBucketResponse secondCreate = await secondTenantClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, secondCreate.HttpStatusCode, "create second tenant bucket");

            ListBucketsResponse defaultBuckets = await defaultClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, defaultBuckets.HttpStatusCode, "list default tenant buckets");
            EnsureEqual(1, CountBucket(defaultBuckets, bucketName), "default tenant bucket count");

            ListBucketsResponse secondBuckets = await secondTenantClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, secondBuckets.HttpStatusCode, "list second tenant buckets");
            EnsureEqual(1, CountBucket(secondBuckets, bucketName), "second tenant bucket count");
        }

        private static async Task S3ListBucketsReturnsOnlyCredentialTenantBucketsAsync(CancellationToken cancellationToken)
        {
            await S3SameBucketNameDifferentTenantsAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListBucketsEmptyTenantReturnsEmptyListAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "empty-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);
            ListBucketsResponse response = await tenantClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "empty-tenant ListBuckets");
            EnsureEqual(0, CountBuckets(response), "empty-tenant bucket count");
        }

        private static async Task S3CreateBucketSucceedsForAuthorizedTenantAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-create", async (server, client, bucketName) =>
            {
                ListBucketsResponse response = await client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "authorized create bucket list");
                EnsureEqual(1, CountBucket(response, bucketName), "authorized created bucket count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3CreateBucketInvalidNameFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 client = server.CreateS3Client("default", "default");
            try
            {
                await client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = "Invalid_Bucket_Name"
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex) when ((int)ex.StatusCode >= 400)
            {
                return;
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("invalid bucket name unexpectedly succeeded.");
        }

        private static async Task BucketReservedRouteNamesRejectedAcrossApisAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 client = server.CreateS3Client("default", "default");
            foreach (string name in new string[] { "api", "admin", "openapi.json", "favicon.ico", "robots.txt" })
            {
                await EnsureS3FailureAsync(
                    () => client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = name
                    }, cancellationToken),
                    "S3 reserved bucket name " + name).ConfigureAwait(false);

                string restJson = JsonSerializer.Serialize(new
                {
                    Id = TestIds.Bucket(),
                    TenantId = "default",
                    OwnerId = "usr_default_admin",
                    Name = name
                });

                HttpResponseMessage restResponse = await server.RestPostAsync("buckets?tenantId=default", restJson, cancellationToken).ConfigureAwait(false);
                EnsureTrue((int)restResponse.StatusCode >= 400, "REST reserved bucket name " + name);

                string adminJson = JsonSerializer.Serialize(new
                {
                    Id = TestIds.Bucket(),
                    TenantId = "default",
                    OwnerId = "usr_default_admin",
                    Name = name
                });

                HttpResponseMessage adminResponse = await server.AdminPostAsync("buckets", adminJson, cancellationToken).ConfigureAwait(false);
                EnsureTrue((int)adminResponse.StatusCode >= 400, "admin reserved bucket name " + name);
            }
        }

        private static async Task S3HeadBucketExistingSameTenantSucceedsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-head", async (server, client, bucketName) =>
            {
                bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName).ConfigureAwait(false);
                EnsureTrue(exists, "same-tenant HeadBucket");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3HeadBucketOtherTenantBucketFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "head-other-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "head-private-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create HeadBucket isolation bucket");

            bool visible = await AmazonS3Util.DoesS3BucketExistV2Async(tenantClient, bucketName).ConfigureAwait(false);
            EnsureFalse(visible, "other-tenant HeadBucket visibility");
        }

        private static async Task S3DeleteBucketEmptyBucketSucceedsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-empty", async (server, client, bucketName) =>
            {
                DeleteBucketResponse delete = await client.DeleteBucketAsync(new DeleteBucketRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);

                EnsureTrue(
                    delete.HttpStatusCode == HttpStatusCode.OK || delete.HttpStatusCode == HttpStatusCode.NoContent,
                    "delete empty bucket status");

                bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName).ConfigureAwait(false);
                EnsureFalse(exists, "deleted bucket existence");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteBucketNonEmptyBucketFailsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-nonempty", async (server, client, bucketName) =>
            {
                PutObjectResponse put = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "not-empty.txt",
                    ContentBody = "not empty"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put object before non-empty delete");

                await EnsureS3FailureAsync(
                    () => client.DeleteBucketAsync(new DeleteBucketRequest
                    {
                        BucketName = bucketName
                    }, cancellationToken),
                    "delete non-empty bucket").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteBucketOtherTenantBucketFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "del-other-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "delete-private-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create delete isolation bucket");

            await EnsureS3FailureAsync(
                () => tenantClient.DeleteBucketAsync(new DeleteBucketRequest
                {
                    BucketName = bucketName
                }, cancellationToken),
                "other-tenant DeleteBucket").ConfigureAwait(false);
        }

        private static async Task S3ListObjectsEmptyBucketAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-empty", async (server, client, bucketName) =>
            {
                ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list empty bucket");
                EnsureEqual(0, CountObjects(response), "empty list object count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListObjectsWithPrefixAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-prefix", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "alpha/one.txt", "one", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "beta/two.txt", "two", cancellationToken).ConfigureAwait(false);

                ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "alpha/"
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list with prefix");
                EnsureTrue(ContainsObject(response, "alpha/one.txt"), "prefix includes expected object");
                EnsureFalse(ContainsObject(response, "beta/two.txt"), "prefix excludes other object");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListObjectsWithDelimiterAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-delimiter", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "docs/root.txt", "root", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "docs/sub/child.txt", "child", cancellationToken).ConfigureAwait(false);

                ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "docs/",
                    Delimiter = "/"
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list with delimiter");
                EnsureTrue(ContainsObject(response, "docs/root.txt"), "delimiter includes root object");
                EnsureTrue(ContainsPrefix(response, "docs/sub/"), "delimiter includes common prefix");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListObjectsWithContinuationAndMaxKeysAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-page", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "page/001.txt", "1", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "page/002.txt", "2", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "page/003.txt", "3", cancellationToken).ConfigureAwait(false);

                ListObjectsV2Response first = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "page/",
                    MaxKeys = 2
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, first.HttpStatusCode, "first paged list");
                EnsureEqual(2, first.S3Objects.Count, "first paged list count");
                EnsureTrue(first.IsTruncated == true, "first paged list truncation");
                EnsureTrue(!String.IsNullOrEmpty(first.NextContinuationToken), "first paged list next token");

                ListObjectsV2Response second = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "page/",
                    ContinuationToken = first.NextContinuationToken
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, second.HttpStatusCode, "second paged list");
                EnsureTrue(second.S3Objects.Count >= 1, "second paged list count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketLocationReturnsConfiguredRegionAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-location", async (server, client, bucketName) =>
            {
                GetBucketLocationResponse response = await client.GetBucketLocationAsync(new GetBucketLocationRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "bucket location");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketVersioningReadDefaultAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-version-default", async (server, client, bucketName) =>
            {
                GetBucketVersioningResponse response = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "bucket versioning default");
                EnsureTrue(response.VersioningConfig == null || response.VersioningConfig.Status != VersionStatus.Enabled, "bucket versioning default not enabled");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketVersioningEnableDisableRoundTripAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-version-toggle", async (server, client, bucketName) =>
            {
                PutBucketVersioningResponse enable = await client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, enable.HttpStatusCode, "enable bucket versioning");

                GetBucketVersioningResponse enabled = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, enabled.HttpStatusCode, "read enabled bucket versioning");
                EnsureTrue(enabled.VersioningConfig != null && enabled.VersioningConfig.Status == VersionStatus.Enabled, "bucket versioning enabled state");

                PutBucketVersioningResponse suspend = await client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Suspended
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, suspend.HttpStatusCode, "suspend bucket versioning");

                GetBucketVersioningResponse suspended = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, suspended.HttpStatusCode, "read suspended bucket versioning");
                EnsureTrue(suspended.VersioningConfig == null || suspended.VersioningConfig.Status != VersionStatus.Enabled, "bucket versioning suspended state");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketMultipartUploadsListEmptyAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-uploads-empty", async (server, client, bucketName) =>
            {
                ListMultipartUploadsResponse response = await client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list empty multipart uploads");
                EnsureEqual(0, CountMultipartUploads(response), "empty multipart upload count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketMultipartUploadsListActiveUploadsTenantScopedAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "uploads-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "uploads-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create multipart isolation bucket");

            InitiateMultipartUploadResponse initiate = await defaultClient.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = "active-upload.txt",
                ContentType = "text/plain"
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, initiate.HttpStatusCode, "initiate active multipart upload");

            ListMultipartUploadsResponse list = await defaultClient.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, list.HttpStatusCode, "list active multipart uploads");
            EnsureTrue(ContainsMultipartUpload(list, initiate.UploadId), "active multipart upload list contains upload");

            await EnsureS3FailureAsync(
                () => tenantClient.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
                {
                    BucketName = bucketName
                }, cancellationToken),
                "other-tenant ListMultipartUploads").ConfigureAwait(false);
        }

        private static async Task S3PutObjectTextAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-put-text", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "hello.txt", "Hello, Less3!", cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectBinaryAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-put-binary", async (server, client, bucketName) =>
            {
                byte[] bytes = BuildBytes(256);
                using MemoryStream stream = new MemoryStream(bytes);
                PutObjectResponse response = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "binary.dat",
                    InputStream = stream,
                    ContentType = "application/octet-stream"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put binary object");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectEmptyBodyAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-put-empty", async (server, client, bucketName) =>
            {
                using MemoryStream stream = new MemoryStream(Array.Empty<byte>());
                PutObjectResponse response = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "empty.dat",
                    InputStream = stream,
                    ContentType = "application/octet-stream"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put empty object");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectLargeObjectAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-put-large", async (server, client, bucketName) =>
            {
                byte[] bytes = BuildBytes(512 * 1024);
                using MemoryStream stream = new MemoryStream(bytes);
                PutObjectResponse response = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "large.bin",
                    InputStream = stream,
                    ContentType = "application/octet-stream"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put large object");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectOverwritesUnversionedObjectAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-overwrite", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "overwrite.txt", "first", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "overwrite.txt", "second", cancellationToken).ConfigureAwait(false);

                using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "overwrite.txt"
                }, cancellationToken).ConfigureAwait(false);

                string body = await ReadResponseStringAsync(response).ConfigureAwait(false);
                EnsureStringEqual("second", body, "unversioned overwrite body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectCreatesNewVersionWhenVersioningEnabledAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-version-put", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);

                PutObjectResponse first = await PutTextObjectAsync(client, bucketName, "versioned.txt", "one", cancellationToken).ConfigureAwait(false);
                PutObjectResponse second = await PutTextObjectAsync(client, bucketName, "versioned.txt", "two", cancellationToken).ConfigureAwait(false);

                EnsureTrue(!String.IsNullOrEmpty(first.VersionId), "first version id");
                EnsureTrue(!String.IsNullOrEmpty(second.VersionId), "second version id");
                EnsureNotEqual(first.VersionId, second.VersionId, "version ids");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectWithContentTypeAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-content-type", async (server, client, bucketName) =>
            {
                PutObjectResponse put = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "data.json",
                    ContentBody = "{\"key\":\"value\"}",
                    ContentType = "application/json"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put content type object");

                GetObjectMetadataResponse head = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "data.json"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, head.HttpStatusCode, "head content type object");
                EnsureStringEqual("application/json", head.Headers.ContentType, "content type");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3PutObjectWithMetadataAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-metadata", async (server, client, bucketName) =>
            {
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "metadata.txt",
                    ContentBody = "metadata-body",
                    ContentType = "text/plain"
                };
                request.Metadata.Add("color", "blue");
                request.Metadata.Add("shape", "circle");

                PutObjectResponse put = await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put metadata object");

                GetObjectMetadataResponse head = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "metadata.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, head.HttpStatusCode, "head metadata object");
                EnsureTrue(MetadataContainsKey(head, "color"), "metadata includes color");
                EnsureTrue(MetadataContainsKey(head, "shape"), "metadata includes shape");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3HeadObjectExistingAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-head-object", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "head.txt", "head", cancellationToken).ConfigureAwait(false);
                GetObjectMetadataResponse response = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "head.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "head existing object");
                EnsureEqual(4, (int)response.Headers.ContentLength, "head content length");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3HeadObjectMissingAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-head-missing", async (server, client, bucketName) =>
            {
                await EnsureS3FailureAsync(
                    () => client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = bucketName,
                        Key = "missing.txt"
                    }, cancellationToken),
                    "head missing object").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectTextAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-get-text", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "hello.txt", "Hello, Less3!", cancellationToken).ConfigureAwait(false);
                using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "hello.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get text object");
                EnsureStringEqual("Hello, Less3!", await ReadResponseStringAsync(response).ConfigureAwait(false), "get text object body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectBinaryAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-get-binary", async (server, client, bucketName) =>
            {
                byte[] bytes = BuildBytes(256);
                using MemoryStream stream = new MemoryStream(bytes);
                PutObjectResponse put = await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "binary.dat",
                    InputStream = stream,
                    ContentType = "application/octet-stream"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put binary before get");

                using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "binary.dat"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get binary object");

                byte[] body = await ReadResponseBytesAsync(response).ConfigureAwait(false);
                EnsureEqual(bytes.Length, body.Length, "binary body length");
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] != body[i])
                    {
                        throw new InvalidOperationException("binary body byte mismatch at " + i);
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectRangeStartEndAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-range", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "alphabet.txt", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", cancellationToken).ConfigureAwait(false);
                using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "alphabet.txt",
                    ByteRange = new ByteRange(0, 4)
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.PartialContent, response.HttpStatusCode, "range get object");
                EnsureStringEqual("ABCDE", await ReadResponseStringAsync(response).ConfigureAwait(false), "range body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectInvalidRangeReturns416Async(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-range-invalid", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "short.txt", "short", cancellationToken).ConfigureAwait(false);
                try
                {
                    using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "short.txt",
                        ByteRange = new ByteRange(100, 200)
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    return;
                }

                throw new InvalidOperationException("invalid range unexpectedly succeeded.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteObjectExistingAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-object", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "delete-me.txt", "delete", cancellationToken).ConfigureAwait(false);
                DeleteObjectResponse delete = await client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "delete-me.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(delete.HttpStatusCode == HttpStatusCode.OK || delete.HttpStatusCode == HttpStatusCode.NoContent, "delete object status");

                await EnsureS3FailureAsync(
                    () => client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = bucketName,
                        Key = "delete-me.txt"
                    }, cancellationToken),
                    "head deleted object").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteObjectsMultipleAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-objects", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "multi-1.txt", "1", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "multi-2.txt", "2", cancellationToken).ConfigureAwait(false);

                DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = new List<KeyVersion>
                    {
                        new KeyVersion { Key = "multi-1.txt" },
                        new KeyVersion { Key = "multi-2.txt" }
                    }
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "delete multiple objects");
                EnsureEqual(2, response.DeletedObjects.Count, "deleted object count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteObjectsMixedExistingAndMissingAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-mixed", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "exists.txt", "exists", cancellationToken).ConfigureAwait(false);

                try
                {
                    await client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = new List<KeyVersion>
                        {
                            new KeyVersion { Key = "exists.txt" },
                            new KeyVersion { Key = "missing.txt" }
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (DeleteObjectsException ex)
                {
                    EnsureEqual(1, ex.Response.DeletedObjects.Count, "mixed deleted object count");
                    EnsureTrue(ex.Response.DeleteErrors.Count >= 1, "mixed delete error count");
                    return;
                }

                throw new InvalidOperationException("mixed DeleteObjects unexpectedly succeeded without errors.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectKeysWithNestedPrefixesAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-nested", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "a/b/c/object.txt", "nested", cancellationToken).ConfigureAwait(false);

                ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "a/b/"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list nested object");
                EnsureTrue(ContainsObject(response, "a/b/c/object.txt"), "nested object key");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectContentEtagStableForStoredContentAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-etag", async (server, client, bucketName) =>
            {
                PutObjectResponse put = await PutTextObjectAsync(client, bucketName, "etag.txt", "etag-body", cancellationToken).ConfigureAwait(false);
                GetObjectMetadataResponse first = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "etag.txt"
                }, cancellationToken).ConfigureAwait(false);
                GetObjectMetadataResponse second = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "etag.txt"
                }, cancellationToken).ConfigureAwait(false);

                EnsureStatus(HttpStatusCode.OK, first.HttpStatusCode, "first etag head");
                EnsureStatus(HttpStatusCode.OK, second.HttpStatusCode, "second etag head");
                EnsureTrue(!String.IsNullOrEmpty(put.ETag), "put etag");
                EnsureStringEqual(first.ETag, second.ETag, "stable etag");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3CreateMultipartUploadReturnsPrettyUploadIdAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-create", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse response = await InitiateMultipartUploadAsync(client, bucketName, "multipart.txt", cancellationToken).ConfigureAwait(false);
                EnsureId(response.UploadId, "upl_", "multipart upload id");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3UploadPartSinglePartAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-single", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "single.txt", cancellationToken).ConfigureAwait(false);
                UploadPartResponse part = await UploadTextPartAsync(client, bucketName, "single.txt", initiate.UploadId, 1, "single-part", cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, part.HttpStatusCode, "upload single multipart part");
                EnsureTrue(!String.IsNullOrEmpty(part.ETag), "single multipart part etag");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3UploadPartMultiplePartsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-multiple", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "multiple.txt", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "multiple.txt", initiate.UploadId, 1, "part-one-", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "multiple.txt", initiate.UploadId, 2, "part-two", cancellationToken).ConfigureAwait(false);

                ListPartsResponse parts = await client.ListPartsAsync(new ListPartsRequest
                {
                    BucketName = bucketName,
                    Key = "multiple.txt",
                    UploadId = initiate.UploadId
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, parts.HttpStatusCode, "list multiple multipart parts");
                EnsureEqual(2, CountParts(parts), "multiple multipart part count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3UploadPartOverwritePartNumberAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-overwrite", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "overwrite-part.txt", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "overwrite-part.txt", initiate.UploadId, 1, "stale", cancellationToken).ConfigureAwait(false);
                UploadPartResponse fresh = await UploadTextPartAsync(client, bucketName, "overwrite-part.txt", initiate.UploadId, 1, "fresh", cancellationToken).ConfigureAwait(false);

                ListPartsResponse parts = await client.ListPartsAsync(new ListPartsRequest
                {
                    BucketName = bucketName,
                    Key = "overwrite-part.txt",
                    UploadId = initiate.UploadId
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, parts.HttpStatusCode, "list overwritten multipart part");
                EnsureEqual(1, CountParts(parts), "overwritten multipart part count");
                EnsureTrue(ContainsPartEtag(parts, 1, fresh.ETag), "overwritten multipart part etag");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListPartsReturnsUploadedPartsAsync(CancellationToken cancellationToken)
        {
            await S3UploadPartMultiplePartsAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3CompleteMultipartUploadAssemblesObjectAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-complete", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "complete.txt", cancellationToken).ConfigureAwait(false);
                UploadPartResponse first = await UploadTextPartAsync(client, bucketName, "complete.txt", initiate.UploadId, 1, "part-one-", cancellationToken).ConfigureAwait(false);
                UploadPartResponse second = await UploadTextPartAsync(client, bucketName, "complete.txt", initiate.UploadId, 2, "part-two", cancellationToken).ConfigureAwait(false);

                CompleteMultipartUploadResponse complete = await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = "complete.txt",
                    UploadId = initiate.UploadId,
                    PartETags = new List<PartETag>
                    {
                        new PartETag(1, first.ETag),
                        new PartETag(2, second.ETag)
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, complete.HttpStatusCode, "complete multipart upload");

                using GetObjectResponse read = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "complete.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStringEqual("part-one-part-two", await ReadResponseStringAsync(read).ConfigureAwait(false), "completed multipart body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3CompleteMultipartUploadMissingPartFailsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-missing", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "missing-part.txt", cancellationToken).ConfigureAwait(false);
                await EnsureS3FailureAsync(
                    () => client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = "missing-part.txt",
                        UploadId = initiate.UploadId,
                        PartETags = new List<PartETag> { new PartETag(1, "\"missing\"") }
                    }, cancellationToken),
                    "complete multipart with missing part").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3CompleteMultipartUploadInvalidEtagFailsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-bad-etag", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "bad-etag.txt", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "bad-etag.txt", initiate.UploadId, 1, "part", cancellationToken).ConfigureAwait(false);
                await EnsureS3FailureAsync(
                    () => client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = "bad-etag.txt",
                        UploadId = initiate.UploadId,
                        PartETags = new List<PartETag> { new PartETag(1, "\"definitely-wrong\"") }
                    }, cancellationToken),
                    "complete multipart with invalid etag").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3AbortMultipartUploadRemovesUploadAndPartsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-abort", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "abort.txt", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "abort.txt", initiate.UploadId, 1, "abort", cancellationToken).ConfigureAwait(false);

                AbortMultipartUploadResponse abort = await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = "abort.txt",
                    UploadId = initiate.UploadId
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(abort.HttpStatusCode == HttpStatusCode.OK || abort.HttpStatusCode == HttpStatusCode.NoContent, "abort multipart upload");

                await EnsureS3FailureAsync(
                    () => client.ListPartsAsync(new ListPartsRequest
                    {
                        BucketName = bucketName,
                        Key = "abort.txt",
                        UploadId = initiate.UploadId
                    }, cancellationToken),
                    "list parts after abort").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3AbortMultipartUploadMissingUploadFailsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-abort-missing", async (server, client, bucketName) =>
            {
                await EnsureS3FailureAsync(
                    () => client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = "missing.txt",
                        UploadId = IdGenerator.GenerateUploadId()
                    }, cancellationToken),
                    "abort missing multipart upload").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3MultipartOtherTenantUploadIdFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "mpu-other-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "mpu-other-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create other-tenant multipart bucket");

            InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(defaultClient, bucketName, "other.txt", cancellationToken).ConfigureAwait(false);
            await EnsureS3FailureAsync(
                () => tenantClient.ListPartsAsync(new ListPartsRequest
                {
                    BucketName = bucketName,
                    Key = "other.txt",
                    UploadId = initiate.UploadId
                }, cancellationToken),
                "other-tenant list multipart parts").ConfigureAwait(false);
        }

        private static async Task S3MultipartTempFilesCleanedAfterCompleteAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-temp-complete", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "temp-complete.txt", cancellationToken).ConfigureAwait(false);
                UploadPartResponse part = await UploadTextPartAsync(client, bucketName, "temp-complete.txt", initiate.UploadId, 1, "temp-complete", cancellationToken).ConfigureAwait(false);
                await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = "temp-complete.txt",
                    UploadId = initiate.UploadId,
                    PartETags = new List<PartETag> { new PartETag(1, part.ETag) }
                }, cancellationToken).ConfigureAwait(false);
                EnsureEqual(0, CountTempFiles(server), "temp file count after multipart complete");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3MultipartTempFilesCleanedAfterAbortAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-mpu-temp-abort", async (server, client, bucketName) =>
            {
                InitiateMultipartUploadResponse initiate = await InitiateMultipartUploadAsync(client, bucketName, "temp-abort.txt", cancellationToken).ConfigureAwait(false);
                await UploadTextPartAsync(client, bucketName, "temp-abort.txt", initiate.UploadId, 1, "temp-abort", cancellationToken).ConfigureAwait(false);
                await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = "temp-abort.txt",
                    UploadId = initiate.UploadId
                }, cancellationToken).ConfigureAwait(false);
                EnsureEqual(0, CountTempFiles(server), "temp file count after multipart abort");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketAclReadDefaultOwnerAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-bucket-acl-read", async (server, client, bucketName) =>
            {
                GetACLResponse response = await client.GetACLAsync(new GetACLRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get bucket acl");
                EnsureTrue(
                    response.AccessControlList != null
                    && response.AccessControlList.Owner != null
                    && !String.IsNullOrEmpty(response.AccessControlList.Owner.Id),
                    "bucket ACL owner");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketAclWriteCannedPrivateAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-bucket-acl-private", async (server, client, bucketName) =>
            {
                PutACLResponse response = await client.PutACLAsync(new PutACLRequest
                {
                    BucketName = bucketName,
                    CannedACL = S3CannedACL.Private
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put bucket private acl");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketAclWriteCannedPublicReadAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-bucket-acl-public", async (server, client, bucketName) =>
            {
                PutACLResponse response = await client.PutACLAsync(new PutACLRequest
                {
                    BucketName = bucketName,
                    CannedACL = S3CannedACL.PublicRead
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put bucket public-read acl");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectAclReadDefaultOwnerAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-object-acl-read", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "acl.txt", "acl", cancellationToken).ConfigureAwait(false);
                GetACLResponse response = await client.GetACLAsync(new GetACLRequest
                {
                    BucketName = bucketName,
                    Key = "acl.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get object acl");
                EnsureTrue(
                    response.AccessControlList != null
                    && response.AccessControlList.Owner != null
                    && !String.IsNullOrEmpty(response.AccessControlList.Owner.Id),
                    "object ACL owner");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectAclWriteCannedPrivateAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-object-acl-private", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "acl.txt", "acl", cancellationToken).ConfigureAwait(false);
                PutACLResponse response = await client.PutACLAsync(new PutACLRequest
                {
                    BucketName = bucketName,
                    Key = "acl.txt",
                    CannedACL = S3CannedACL.Private
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put object private acl");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3BucketTagsPutGetDeleteRoundTripAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-bucket-tags", async (server, client, bucketName) =>
            {
                PutBucketTaggingResponse put = await client.PutBucketTaggingAsync(new PutBucketTaggingRequest
                {
                    BucketName = bucketName,
                    TagSet = new List<Tag>
                    {
                        new Tag { Key = "Environment", Value = "Test" },
                        new Tag { Key = "Component", Value = "Less3" }
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put bucket tags");

                GetBucketTaggingResponse get = await client.GetBucketTaggingAsync(new GetBucketTaggingRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, get.HttpStatusCode, "get bucket tags");
                EnsureEqual(2, CountTags(get.TagSet), "bucket tag count");

                DeleteBucketTaggingResponse delete = await client.DeleteBucketTaggingAsync(new DeleteBucketTaggingRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(delete.HttpStatusCode == HttpStatusCode.OK || delete.HttpStatusCode == HttpStatusCode.NoContent, "delete bucket tags");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectTagsReadMissingReturnsEmptyAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-object-tags-empty", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "tags.txt", "tags", cancellationToken).ConfigureAwait(false);
                GetObjectTaggingResponse get = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "tags.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, get.HttpStatusCode, "get missing object tags");
                EnsureEqual(0, CountTags(get.Tagging), "missing object tag count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectTagsPutGetDeleteRoundTripAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-object-tags", async (server, client, bucketName) =>
            {
                await PutTextObjectAsync(client, bucketName, "tags.txt", "tags", cancellationToken).ConfigureAwait(false);
                PutObjectTaggingResponse put = await client.PutObjectTaggingAsync(new PutObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "tags.txt",
                    Tagging = new Amazon.S3.Model.Tagging
                    {
                        TagSet = new List<Tag>
                        {
                            new Tag { Key = "Type", Value = "Metadata" },
                            new Tag { Key = "Owner", Value = "Less3" }
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put object tags");

                GetObjectTaggingResponse get = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "tags.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, get.HttpStatusCode, "get object tags");
                EnsureEqual(2, CountTags(get.Tagging), "object tag count");

                DeleteObjectTaggingResponse delete = await client.DeleteObjectTaggingAsync(new DeleteObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "tags.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(delete.HttpStatusCode == HttpStatusCode.OK || delete.HttpStatusCode == HttpStatusCode.NoContent, "delete object tags");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ObjectTagsVersionSpecificRoundTripAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-object-tags-version", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                PutObjectResponse first = await PutTextObjectAsync(client, bucketName, "version-tags.txt", "one", cancellationToken).ConfigureAwait(false);
                PutObjectResponse second = await PutTextObjectAsync(client, bucketName, "version-tags.txt", "two", cancellationToken).ConfigureAwait(false);

                PutObjectTaggingResponse put = await client.PutObjectTaggingAsync(new PutObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "version-tags.txt",
                    VersionId = first.VersionId,
                    Tagging = new Amazon.S3.Model.Tagging
                    {
                        TagSet = new List<Tag> { new Tag { Key = "Version", Value = "One" } }
                    }
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, put.HttpStatusCode, "put version-specific object tags");

                GetObjectTaggingResponse firstTags = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "version-tags.txt",
                    VersionId = first.VersionId
                }, cancellationToken).ConfigureAwait(false);
                EnsureEqual(1, CountTags(firstTags.Tagging), "first version object tag count");

                GetObjectTaggingResponse secondTags = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "version-tags.txt",
                    VersionId = second.VersionId
                }, cancellationToken).ConfigureAwait(false);
                EnsureEqual(0, CountTags(secondTags.Tagging), "second version object tag count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3TagsOtherTenantResourceFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "tag-other-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "tag-other-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create tag isolation bucket");
            await PutTextObjectAsync(defaultClient, bucketName, "tags.txt", "tags", cancellationToken).ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => tenantClient.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = "tags.txt"
                }, cancellationToken),
                "other-tenant object tag read").ConfigureAwait(false);
        }

        private static async Task S3VersioningEnableAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-version-enable", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                GetBucketVersioningResponse response = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                {
                    BucketName = bucketName
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(response.VersioningConfig != null && response.VersioningConfig.Status == VersionStatus.Enabled, "versioning enabled");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectByVersionIdAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-get-version", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                PutObjectResponse first = await PutTextObjectAsync(client, bucketName, "versioned.txt", "one", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "versioned.txt", "two", cancellationToken).ConfigureAwait(false);

                using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "versioned.txt",
                    VersionId = first.VersionId
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "get object by version id");
                EnsureStringEqual("one", await ReadResponseStringAsync(response).ConfigureAwait(false), "versioned object body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3GetObjectMissingVersionFailsAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-get-missing-version", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "versioned.txt", "one", cancellationToken).ConfigureAwait(false);
                await EnsureS3FailureAsync(
                    () => client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "versioned.txt",
                        VersionId = "9999"
                    }, cancellationToken),
                    "get missing object version").ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteObjectCreatesDeleteMarkerWhenVersioningEnabledAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-marker", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "delete-marker.txt", "body", cancellationToken).ConfigureAwait(false);
                await client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "delete-marker.txt"
                }, cancellationToken).ConfigureAwait(false);

                ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest
                {
                    BucketName = bucketName,
                    Prefix = "delete-marker.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureTrue(CountDeleteMarkers(versions) > 0, "delete marker count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3DeleteObjectVersionRemovesSpecificVersionAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-delete-version", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                PutObjectResponse first = await PutTextObjectAsync(client, bucketName, "delete-version.txt", "one", cancellationToken).ConfigureAwait(false);
                PutObjectResponse second = await PutTextObjectAsync(client, bucketName, "delete-version.txt", "two", cancellationToken).ConfigureAwait(false);

                await client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "delete-version.txt",
                    VersionId = first.VersionId
                }, cancellationToken).ConfigureAwait(false);

                await EnsureS3FailureAsync(
                    () => client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "delete-version.txt",
                        VersionId = first.VersionId
                    }, cancellationToken),
                    "get deleted object version").ConfigureAwait(false);

                using GetObjectResponse current = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "delete-version.txt",
                    VersionId = second.VersionId
                }, cancellationToken).ConfigureAwait(false);
                EnsureStringEqual("two", await ReadResponseStringAsync(current).ConfigureAwait(false), "remaining version body");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListObjectVersionsReturnsVersionsAndDeleteMarkersAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-versions", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "versions.txt", "one", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "versions.txt", "two", cancellationToken).ConfigureAwait(false);
                await client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = "versions.txt"
                }, cancellationToken).ConfigureAwait(false);

                ListVersionsResponse response = await client.ListVersionsAsync(new ListVersionsRequest
                {
                    BucketName = bucketName,
                    Prefix = "versions.txt"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list object versions");
                EnsureTrue(CountVersions(response) >= 2, "version count");
                EnsureTrue(CountDeleteMarkers(response) >= 1, "delete marker count");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3ListObjectVersionsPrefixAndDelimiterAsync(CancellationToken cancellationToken)
        {
            await WithDefaultBucketAsync("s3-list-version-prefix", async (server, client, bucketName) =>
            {
                await EnableVersioningAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "docs/root.txt", "root", cancellationToken).ConfigureAwait(false);
                await PutTextObjectAsync(client, bucketName, "docs/sub/child.txt", "child", cancellationToken).ConfigureAwait(false);

                ListVersionsResponse response = await client.ListVersionsAsync(new ListVersionsRequest
                {
                    BucketName = bucketName,
                    Prefix = "docs/",
                    Delimiter = "/"
                }, cancellationToken).ConfigureAwait(false);
                EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "list versions with prefix delimiter");
                EnsureTrue(ContainsVersion(response, "docs/root.txt"), "version prefix includes root object");
                EnsureTrue(ContainsVersionPrefix(response, "docs/sub/"), "version prefix includes common prefix");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task S3VersioningOtherTenantVersionAccessFailsAsync(CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            string tenantId = TestIds.Tenant();
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = "ver-other-" + TestIds.Suffix();
            string secretKey = "secret-" + TestIds.Suffix();
            string bucketName = "ver-other-" + TestIds.Suffix().Substring(0, 8);

            await CreateTenantUserAndCredentialAsync(server, tenantId, userId, credentialId, accessKey, secretKey, cancellationToken).ConfigureAwait(false);

            using IAmazonS3 defaultClient = server.CreateS3Client("default", "default");
            using IAmazonS3 tenantClient = server.CreateS3Client(accessKey, secretKey);

            PutBucketResponse create = await defaultClient.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create version isolation bucket");
            await EnableVersioningAsync(defaultClient, bucketName, cancellationToken).ConfigureAwait(false);
            PutObjectResponse put = await PutTextObjectAsync(defaultClient, bucketName, "versioned.txt", "versioned", cancellationToken).ConfigureAwait(false);

            await EnsureS3FailureAsync(
                () => tenantClient.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "versioned.txt",
                    VersionId = put.VersionId
                }, cancellationToken),
                "other-tenant versioned object read").ConfigureAwait(false);
        }

        private static async Task CreateTenantUserAndCredentialAsync(
            Less3TestServer server,
            string tenantId,
            string userId,
            string credentialId,
            string accessKey,
            string secretKey,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage tenantResponse = await server.RestPostAsync("tenants", JsonSerializer.Serialize(new
            {
                Id = tenantId,
                Name = "Second tenant",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, tenantResponse.StatusCode, "create second tenant");

            HttpResponseMessage userResponse = await server.RestPostAsync("users?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = userId,
                TenantId = tenantId,
                Name = "Second tenant user",
                Email = userId + "@example.com",
                PasswordHash = "password",
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userResponse.StatusCode, "create second tenant user");

            HttpResponseMessage credentialResponse = await server.RestPostAsync("credentials?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = credentialId,
                TenantId = tenantId,
                UserId = userId,
                Description = "Second tenant credential",
                AccessKey = accessKey,
                SecretKey = secretKey,
                IsBase64 = false,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialResponse.StatusCode, "create second tenant credential");

            HttpResponseMessage userAssignmentResponse = await server.RestPostAsync("roleassignments?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = TestIds.Assignment(),
                TenantId = tenantId,
                RoleId = "rol_builtin_tenantadmin",
                PrincipalType = "User",
                PrincipalId = userId,
                ResourceType = "Tenant",
                ResourceId = tenantId,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, userAssignmentResponse.StatusCode, "assign tenant admin role to second tenant user");

            HttpResponseMessage credentialAssignmentResponse = await server.RestPostAsync("roleassignments?tenantId=" + tenantId, JsonSerializer.Serialize(new
            {
                Id = TestIds.Assignment(),
                TenantId = tenantId,
                RoleId = "rol_builtin_tenantadmin",
                PrincipalType = "Credential",
                PrincipalId = credentialId,
                ResourceType = "Tenant",
                ResourceId = tenantId,
                Active = true
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, credentialAssignmentResponse.StatusCode, "assign tenant admin role to second tenant credential");
        }

        private static IReadOnlyList<Type> TenantOwnedContractTypes()
        {
            return new List<Type>
            {
                typeof(Less3.Classes.Bucket),
                typeof(Less3.Classes.BucketAcl),
                typeof(Less3.Classes.BucketTag),
                typeof(Less3.Classes.Credential),
                typeof(Less3.Classes.Obj),
                typeof(Less3.Classes.ObjectAcl),
                typeof(Less3.Classes.ObjectTag),
                typeof(Less3.Classes.Permission),
                typeof(Less3.Classes.RequestHistory),
                typeof(Less3.Classes.Role),
                typeof(Less3.Classes.RoleAssignment),
                typeof(Less3.Classes.Upload),
                typeof(Less3.Classes.UploadPart),
                typeof(Less3.Classes.User)
            };
        }

        private static IReadOnlyList<Type> PublicContractTypes()
        {
            List<Type> types = new List<Type>
            {
                typeof(Less3.Classes.AuthSession),
                typeof(Less3.Classes.AuthorizationAudit),
                typeof(Less3.Classes.Bucket),
                typeof(Less3.Classes.BucketAcl),
                typeof(Less3.Classes.BucketTag),
                typeof(Less3.Classes.Credential),
                typeof(Less3.Classes.Obj),
                typeof(Less3.Classes.ObjectAcl),
                typeof(Less3.Classes.ObjectTag),
                typeof(Less3.Classes.Permission),
                typeof(Less3.Classes.RequestHistory),
                typeof(Less3.Classes.Role),
                typeof(Less3.Classes.RoleAssignment),
                typeof(Less3.Classes.Tenant),
                typeof(Less3.Classes.Upload),
                typeof(Less3.Classes.UploadPart),
                typeof(Less3.Classes.User)
            };

            return types;
        }

        private static PropertyInfo GetPublicProperty(Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new InvalidOperationException(type.FullName + " did not expose " + propertyName + ".");
            }

            return property;
        }

        private static bool IsIntegerType(Type type)
        {
            return type == typeof(byte)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(sbyte)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "src", "Less3", "Less3.csproj"))
                    && Directory.Exists(Path.Combine(directory.FullName, "test", "Test.Shared")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate repository root from " + AppContext.BaseDirectory);
        }

        private static void AssertNoRegexInFiles(string root, Regex regex, string operation)
        {
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(operation + " root not found: " + root);
            }

            foreach (string file in EnumerateSourceFiles(root))
            {
                string text = File.ReadAllText(file);
                Match match = regex.Match(text);
                if (match.Success)
                {
                    throw new InvalidOperationException(operation + " found " + match.Value + " in " + file);
                }
            }
        }

        private static void AssertAtLeastOneRegexInFiles(string root, Regex regex, string operation)
        {
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException(operation + " root not found: " + root);
            }

            foreach (string file in EnumerateSourceFiles(root))
            {
                string text = File.ReadAllText(file);
                if (regex.IsMatch(text))
                {
                    return;
                }
            }

            throw new InvalidOperationException(operation + " did not find an expected match.");
        }

        private static IEnumerable<string> EnumerateSourceFiles(string root)
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                string normalized = file.Replace(Path.DirectorySeparatorChar, '/');
                if (normalized.Contains("/bin/", StringComparison.Ordinal)
                    || normalized.Contains("/obj/", StringComparison.Ordinal)
                    || normalized.Contains("/node_modules/", StringComparison.Ordinal)
                    || normalized.Contains("/test-results/", StringComparison.Ordinal)
                    || normalized.EndsWith("/Less3.xml", StringComparison.Ordinal))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (String.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(extension, ".ts", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(extension, ".tsx", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(extension, ".js", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(extension, ".jsx", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        private static async Task WithDefaultBucketAsync(
            string bucketPrefix,
            Func<Less3TestServer, IAmazonS3, string, Task> action,
            CancellationToken cancellationToken)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            using IAmazonS3 client = server.CreateS3Client("default", "default");
            string bucketName = bucketPrefix + "-" + TestIds.Suffix().Substring(0, 8);

            PutBucketResponse create = await client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, create.HttpStatusCode, "create bucket for " + bucketPrefix);

            await action(server, client, bucketName).ConfigureAwait(false);
        }

        private static async Task<PutObjectResponse> PutTextObjectAsync(
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
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "put text object " + key);
            return response;
        }

        private static async Task EnableVersioningAsync(
            IAmazonS3 client,
            string bucketName,
            CancellationToken cancellationToken)
        {
            PutBucketVersioningResponse response = await client.PutBucketVersioningAsync(new PutBucketVersioningRequest
            {
                BucketName = bucketName,
                VersioningConfig = new S3BucketVersioningConfig
                {
                    Status = VersionStatus.Enabled
                }
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "enable versioning");
        }

        private static async Task<InitiateMultipartUploadResponse> InitiateMultipartUploadAsync(
            IAmazonS3 client,
            string bucketName,
            string key,
            CancellationToken cancellationToken)
        {
            InitiateMultipartUploadResponse response = await client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = "text/plain"
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "initiate multipart upload " + key);
            EnsureTrue(!String.IsNullOrEmpty(response.UploadId), "multipart upload id");
            return response;
        }

        private static async Task<UploadPartResponse> UploadTextPartAsync(
            IAmazonS3 client,
            string bucketName,
            string key,
            string uploadId,
            int partNumber,
            string body,
            CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            using MemoryStream stream = new MemoryStream(bytes);
            UploadPartResponse response = await client.UploadPartAsync(new UploadPartRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId,
                PartNumber = partNumber,
                InputStream = stream
            }, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, response.HttpStatusCode, "upload multipart part " + partNumber);
            EnsureTrue(!String.IsNullOrEmpty(response.ETag), "multipart part etag");
            return response;
        }

        private static async Task<string> ReadResponseStringAsync(GetObjectResponse response)
        {
            byte[] bytes = await ReadResponseBytesAsync(response).ConfigureAwait(false);
            return Encoding.UTF8.GetString(bytes);
        }

        private static async Task<byte[]> ReadResponseBytesAsync(GetObjectResponse response)
        {
            using MemoryStream stream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(stream).ConfigureAwait(false);
            return stream.ToArray();
        }

        private static byte[] BuildBytes(int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i % 256);
            }

            return bytes;
        }

        private static bool MetadataContainsKey(GetObjectMetadataResponse response, string key)
        {
            foreach (string current in response.Metadata.Keys)
            {
                if (current.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsObject(ListObjectsV2Response response, string key)
        {
            if (response.S3Objects == null) return false;

            foreach (S3Object entry in response.S3Objects)
            {
                if (String.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPrefix(ListObjectsV2Response response, string prefix)
        {
            if (response.CommonPrefixes == null) return false;

            foreach (string current in response.CommonPrefixes)
            {
                if (String.Equals(current, prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsMultipartUpload(ListMultipartUploadsResponse response, string uploadId)
        {
            if (response.MultipartUploads == null) return false;

            foreach (MultipartUpload upload in response.MultipartUploads)
            {
                if (String.Equals(upload.UploadId, uploadId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPartEtag(ListPartsResponse response, int partNumber, string etag)
        {
            if (response.Parts == null) return false;
            return response.Parts.Exists(p => p.PartNumber == partNumber && String.Equals(p.ETag, etag, StringComparison.Ordinal));
        }

        private static bool ContainsVersion(ListVersionsResponse response, string key)
        {
            if (response.Versions == null) return false;
            return response.Versions.Exists(v => String.Equals(v.Key, key, StringComparison.Ordinal));
        }

        private static bool ContainsVersionPrefix(ListVersionsResponse response, string prefix)
        {
            if (response.CommonPrefixes == null) return false;
            return response.CommonPrefixes.Exists(p => String.Equals(p, prefix, StringComparison.Ordinal));
        }

        private static int CountBuckets(ListBucketsResponse response)
        {
            if (response.Buckets == null) return 0;
            return response.Buckets.Count;
        }

        private static int CountObjects(ListObjectsV2Response response)
        {
            if (response.S3Objects == null) return 0;
            return response.S3Objects.Count;
        }

        private static int CountMultipartUploads(ListMultipartUploadsResponse response)
        {
            if (response.MultipartUploads == null) return 0;
            return response.MultipartUploads.Count;
        }

        private static int CountParts(ListPartsResponse response)
        {
            if (response.Parts == null) return 0;
            return response.Parts.Count;
        }

        private static int CountTempFiles(Less3TestServer server)
        {
            string tempDirectory = Path.Combine(server.TempDirectory, "temp");
            if (!Directory.Exists(tempDirectory)) return 0;
            return Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories).Length;
        }

        private static int CountGrants(System.Collections.ICollection grants)
        {
            if (grants == null) return 0;
            return grants.Count;
        }

        private static int CountTags(List<Tag> tags)
        {
            if (tags == null) return 0;
            return tags.Count;
        }

        private static int CountVersions(ListVersionsResponse response)
        {
            if (response.Versions == null) return 0;
            return response.Versions.Count;
        }

        private static int CountDeleteMarkers(ListVersionsResponse response)
        {
            if (response.Versions == null) return 0;

            int count = 0;
            foreach (S3ObjectVersion version in response.Versions)
            {
                if (version.IsDeleteMarker == true) count++;
            }

            return count;
        }

        private static async Task AssertRestCrudRoundTripAsync(
            Less3TestServer server,
            string resourceType,
            string id,
            string queryString,
            string operation,
            string updateJson,
            string updateNeedle,
            CancellationToken cancellationToken)
        {
            string query = String.IsNullOrEmpty(queryString) ? "" : "?" + queryString;

            HttpResponseMessage readResponse = await server.RestGetAsync(resourceType + "/" + id + query, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, readResponse.StatusCode, "REST read " + operation);
            EnsureContains(await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), id, "REST read " + operation);

            HttpResponseMessage existsResponse = await server.RestGetAsync(resourceType + "/" + id + "/exists" + query, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, existsResponse.StatusCode, "REST exists " + operation);
            EnsureContains(await existsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": true", "REST exists " + operation);

            HttpResponseMessage enumerateResponse = await server.RestPostAsync(resourceType + "/enumerate" + query, JsonSerializer.Serialize(new
            {
                TenantId = "default",
                Limit = 100,
                Offset = 0,
                SortField = "id"
            }), cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, enumerateResponse.StatusCode, "REST enumerate " + operation);
            EnsureContains(await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), id, "REST enumerate " + operation);

            HttpResponseMessage updateResponse = await server.RestPutAsync(resourceType + "/" + id + query, updateJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, updateResponse.StatusCode, "REST update " + operation);
            EnsureContains(await updateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), updateNeedle, "REST update " + operation);

            HttpResponseMessage deleteResponse = await server.RestDeleteAsync(resourceType + "/" + id + query, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.NoContent, deleteResponse.StatusCode, "REST delete " + operation);

            HttpResponseMessage missingResponse = await server.RestGetAsync(resourceType + "/" + id + "/exists" + query, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.OK, missingResponse.StatusCode, "REST missing exists " + operation);
            EnsureContains(await missingResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false), "\"Exists\": false", "REST missing exists " + operation);
        }

        private static void EnsureStatus(HttpStatusCode expected, HttpStatusCode actual, string operation)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException(operation + " expected " + expected + " but received " + actual);
            }
        }

        private static void EnsureTrue(bool value, string operation)
        {
            if (!value)
            {
                throw new InvalidOperationException(operation + " expected true.");
            }
        }

        private static void EnsureFalse(bool value, string operation)
        {
            if (value)
            {
                throw new InvalidOperationException(operation + " expected false.");
            }
        }

        private static void EnsureStringEqual(string expected, string actual, string operation)
        {
            if (!String.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " expected " + expected + " but received " + actual);
            }
        }

        private static void EnsureNotEqual(string left, string right, string operation)
        {
            if (String.Equals(left, right, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " unexpectedly matched " + left);
            }
        }

        private static void EnsureContains(string haystack, string needle, string operation)
        {
            if (String.IsNullOrEmpty(haystack) || !haystack.Contains(needle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " did not contain " + needle);
            }
        }

        private static void EnsureNotContains(string haystack, string needle, string operation)
        {
            if (!String.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " contained " + needle);
            }
        }

        private static string ExtractString(string json, string propertyName, string operation)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(propertyName, out JsonElement element))
            {
                throw new InvalidOperationException(operation + " did not include property " + propertyName);
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(operation + " property " + propertyName + " was not a string");
            }

            return element.GetString() ?? String.Empty;
        }

        private static string ExtractNestedString(string json, string objectName, string propertyName, string operation)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(objectName, out JsonElement nested)
                || nested.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(operation + " did not include object property " + objectName);
            }

            if (!nested.TryGetProperty(propertyName, out JsonElement element))
            {
                throw new InvalidOperationException(operation + " did not include nested property " + objectName + "." + propertyName);
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(operation + " property " + objectName + "." + propertyName + " was not a string");
            }

            return element.GetString() ?? String.Empty;
        }

        private static string ExtractFirstEnumerationItemString(string json, string propertyName, string operation)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement item = FirstEnumerationItem(document, operation);
            if (!item.TryGetProperty(propertyName, out JsonElement element))
            {
                throw new InvalidOperationException(operation + " did not include first item property " + propertyName);
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(operation + " first item property " + propertyName + " was not a string");
            }

            return element.GetString() ?? String.Empty;
        }

        private static int ExtractEnumerationItemCount(string json, string operation)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("Items", out JsonElement items)
                || items.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(operation + " did not include an Items array");
            }

            return items.GetArrayLength();
        }

        private static long ExtractEnumerationTotal(string json, string operation)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("Total", out JsonElement total))
            {
                throw new InvalidOperationException(operation + " did not include Total");
            }

            if (total.ValueKind == JsonValueKind.Number && total.TryGetInt64(out long value))
            {
                return value;
            }

            throw new InvalidOperationException(operation + " Total was not a number");
        }

        private static JsonElement FirstEnumerationItem(JsonDocument document, string operation)
        {
            if (!document.RootElement.TryGetProperty("Items", out JsonElement items)
                || items.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(operation + " did not include an Items array");
            }

            if (items.GetArrayLength() < 1)
            {
                throw new InvalidOperationException(operation + " did not return any items");
            }

            return items[0];
        }

        private static async Task<string> LoginAndExtractTokenAsync(
            Less3TestServer server,
            string tenantId,
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            string loginJson = JsonSerializer.Serialize(new
            {
                TenantId = tenantId,
                Email = email,
                Password = password,
                ExpirationMinutes = 30
            });

            HttpResponseMessage loginResponse = await server.RestPostUnauthenticatedAsync("authsessions/login", loginJson, cancellationToken).ConfigureAwait(false);
            EnsureStatus(HttpStatusCode.Created, loginResponse.StatusCode, "REST bearer login");
            string loginBody = await loginResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ExtractString(loginBody, "Token", "REST bearer login token");
        }

        private static async Task<HttpResponseMessage> SendBearerRestAsync(
            Less3TestServer server,
            HttpMethod method,
            string path,
            string token,
            string body,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, server.BaseUrl + "/api/v1/" + path);
            request.Headers.TryAddWithoutValidation("x-less3-session-token", token);
            if (body != null)
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            return await server.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static void EnsureId(string id, string prefix, string operation)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException(operation + " was empty");
            }

            if (!id.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(operation + " did not start with " + prefix + ": " + id);
            }

            if (id.Length > IdGenerator.MaximumLength)
            {
                throw new InvalidOperationException(operation + " exceeded maximum length: " + id);
            }
        }

        private static void EnsureEqual(int expected, int actual, string operation)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(operation + " expected " + expected + " but received " + actual);
            }
        }

        private static void EnsureGreaterOrEqual(long expectedMinimum, long actual, string operation)
        {
            if (actual < expectedMinimum)
            {
                throw new InvalidOperationException(operation + " expected at least " + expectedMinimum + " but received " + actual);
            }
        }

        private static async Task EnsureS3FailureAsync(Func<Task> action, string operation)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden
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
            int count = 0;
            if (response.Buckets == null) return count;

            foreach (S3Bucket bucket in response.Buckets)
            {
                if (String.Equals(bucket.BucketName, bucketName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
