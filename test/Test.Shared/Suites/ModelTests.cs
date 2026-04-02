namespace Test.Shared.Suites
{
    using System;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Storage;

    /// <summary>
    /// Tests for Less3 data model classes: Bucket, User, Credential, Obj, BucketAcl, ObjectAcl, BucketTag, ObjectTag, etc.
    /// </summary>
    public class ModelTests : TestSuite
    {
        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Model Tests";

        /// <summary>
        /// Runs all model tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            #region Bucket

            await RunTest("Bucket_DefaultConstructor", () =>
            {
                Bucket bucket = new Bucket();
                AssertNotNull(bucket.GUID, "GUID should be set");
                AssertNotNull(bucket.OwnerGUID, "OwnerGUID should be set");
                AssertEqual("us-west-1", bucket.RegionString);
                AssertEqual(StorageDriverType.Disk, bucket.StorageType);
                AssertEqual("./disk/", bucket.DiskDirectory);
                AssertFalse(bucket.EnableVersioning);
                AssertFalse(bucket.EnablePublicWrite);
                AssertFalse(bucket.EnablePublicRead);
            });

            await RunTest("Bucket_ParameterizedConstructor", () =>
            {
                Bucket bucket = new Bucket("test-bucket", "owner-guid", StorageDriverType.Disk, "/data/", "eu-west-1");
                AssertEqual("test-bucket", bucket.Name);
                AssertEqual("owner-guid", bucket.OwnerGUID);
                AssertEqual(StorageDriverType.Disk, bucket.StorageType);
                AssertEqual("/data/", bucket.DiskDirectory);
                AssertEqual("eu-west-1", bucket.RegionString);
            });

            await RunTest("Bucket_ParameterizedConstructorWithGuid", () =>
            {
                string guid = Guid.NewGuid().ToString();
                Bucket bucket = new Bucket(guid, "test-bucket", "owner-guid", StorageDriverType.Disk, "/data/");
                AssertEqual(guid, bucket.GUID);
                AssertEqual("test-bucket", bucket.Name);
            });

            #endregion

            #region User

            await RunTest("User_DefaultConstructor", () =>
            {
                User user = new User();
                AssertNotNull(user.GUID, "GUID should be set");
            });

            await RunTest("User_ParameterizedConstructor", () =>
            {
                User user = new User("Test User", "test@example.com");
                AssertEqual("Test User", user.Name);
                AssertEqual("test@example.com", user.Email);
                AssertNotNull(user.GUID);
            });

            await RunTest("User_ParameterizedConstructorWithGuid", () =>
            {
                string guid = Guid.NewGuid().ToString();
                User user = new User(guid, "Test User", "test@example.com");
                AssertEqual(guid, user.GUID);
                AssertEqual("Test User", user.Name);
                AssertEqual("test@example.com", user.Email);
            });

            #endregion

            #region Credential

            await RunTest("Credential_DefaultConstructor", () =>
            {
                Credential cred = new Credential();
                AssertNotNull(cred.GUID);
                AssertNotNull(cred.UserGUID);
                AssertFalse(cred.IsBase64);
            });

            await RunTest("Credential_ParameterizedConstructor", () =>
            {
                Credential cred = new Credential("user-guid", "My credential", "AKIAEXAMPLE", "secretkey", false);
                AssertEqual("user-guid", cred.UserGUID);
                AssertEqual("My credential", cred.Description);
                AssertEqual("AKIAEXAMPLE", cred.AccessKey);
                AssertEqual("secretkey", cred.SecretKey);
                AssertFalse(cred.IsBase64);
            });

            await RunTest("Credential_ParameterizedConstructorWithGuid", () =>
            {
                string guid = Guid.NewGuid().ToString();
                Credential cred = new Credential(guid, "user-guid", "desc", "ak", "sk", true);
                AssertEqual(guid, cred.GUID);
                AssertTrue(cred.IsBase64);
            });

            #endregion

            #region Obj

            await RunTest("Obj_DefaultConstructor", () =>
            {
                Obj obj = new Obj();
                AssertNotNull(obj.GUID);
                AssertEqual("application/octet-stream", obj.ContentType);
                AssertEqual(0L, obj.ContentLength);
                AssertEqual(1L, obj.Version);
                AssertFalse(obj.IsFolder);
                AssertFalse(obj.DeleteMarker);
                AssertEqual(RetentionType.NONE, obj.Retention);
            });

            await RunTest("Obj_PropertyAssignment", () =>
            {
                Obj obj = new Obj();
                obj.Key = "folder/file.txt";
                obj.ContentType = "text/plain";
                obj.ContentLength = 1024;
                obj.Version = 3;
                obj.BucketGUID = "bucket-guid";
                obj.OwnerGUID = "owner-guid";

                AssertEqual("folder/file.txt", obj.Key);
                AssertEqual("text/plain", obj.ContentType);
                AssertEqual(1024L, obj.ContentLength);
                AssertEqual(3L, obj.Version);
            });

            #endregion

            #region BucketAcl

            await RunTest("BucketAcl_DefaultConstructor", () =>
            {
                BucketAcl acl = new BucketAcl();
                AssertNotNull(acl.GUID);
                AssertFalse(acl.PermitRead);
                AssertFalse(acl.PermitWrite);
                AssertFalse(acl.PermitReadAcp);
                AssertFalse(acl.PermitWriteAcp);
                AssertFalse(acl.FullControl);
            });

            await RunTest("BucketAcl_GroupAclFactory", () =>
            {
                BucketAcl acl = BucketAcl.GroupAcl("AllUsers", "issuer-guid", "bucket-guid", true, false, false, false, false);
                AssertEqual("AllUsers", acl.UserGroup);
                AssertEqual("issuer-guid", acl.IssuedByUserGUID);
                AssertEqual("bucket-guid", acl.BucketGUID);
                AssertTrue(acl.PermitRead);
                AssertFalse(acl.PermitWrite);
            });

            await RunTest("BucketAcl_UserAclFactory", () =>
            {
                BucketAcl acl = BucketAcl.UserAcl("user-guid", "issuer-guid", "bucket-guid", true, true, true, true, true);
                AssertEqual("user-guid", acl.UserGUID);
                AssertTrue(acl.FullControl);
            });

            await RunTest("BucketAcl_ToString", () =>
            {
                BucketAcl acl = BucketAcl.GroupAcl("AllUsers", "issuer", "bucket", true, false, false, false, false);
                string result = acl.ToString();
                AssertNotNull(result);
                AssertTrue(result.Length > 0, "ToString should return non-empty string");
            });

            #endregion

            #region ObjectAcl

            await RunTest("ObjectAcl_DefaultConstructor", () =>
            {
                ObjectAcl acl = new ObjectAcl();
                AssertNotNull(acl.GUID);
                AssertFalse(acl.PermitRead);
                AssertFalse(acl.PermitWrite);
                AssertFalse(acl.FullControl);
            });

            await RunTest("ObjectAcl_GroupAclFactory", () =>
            {
                ObjectAcl acl = ObjectAcl.GroupAcl("AuthenticatedUsers", "issuer", "bucket", "object", false, true, false, false, false);
                AssertEqual("AuthenticatedUsers", acl.UserGroup);
                AssertEqual("bucket", acl.BucketGUID);
                AssertEqual("object", acl.ObjectGUID);
                AssertFalse(acl.PermitRead);
                AssertTrue(acl.PermitWrite);
            });

            await RunTest("ObjectAcl_UserAclFactory", () =>
            {
                ObjectAcl acl = ObjectAcl.UserAcl("user", "issuer", "bucket", "object", true, true, true, true, true);
                AssertEqual("user", acl.UserGUID);
                AssertTrue(acl.FullControl);
            });

            #endregion

            #region BucketTag

            await RunTest("BucketTag_DefaultConstructor", () =>
            {
                BucketTag tag = new BucketTag();
                AssertNotNull(tag.GUID);
            });

            await RunTest("BucketTag_ParameterizedConstructor", () =>
            {
                BucketTag tag = new BucketTag("bucket-guid", "env", "production");
                AssertEqual("bucket-guid", tag.BucketGUID);
                AssertEqual("env", tag.Key);
                AssertEqual("production", tag.Value);
            });

            await RunTest("BucketTag_ParameterizedConstructorWithGuid", () =>
            {
                string guid = Guid.NewGuid().ToString();
                BucketTag tag = new BucketTag(guid, "bucket-guid", "env", "staging");
                AssertEqual(guid, tag.GUID);
            });

            #endregion

            #region ObjectTag

            await RunTest("ObjectTag_DefaultConstructor", () =>
            {
                ObjectTag tag = new ObjectTag();
                AssertNotNull(tag.GUID);
            });

            await RunTest("ObjectTag_ParameterizedConstructor", () =>
            {
                ObjectTag tag = new ObjectTag("bucket-guid", "object-guid", "status", "active");
                AssertEqual("bucket-guid", tag.BucketGUID);
                AssertEqual("object-guid", tag.ObjectGUID);
                AssertEqual("status", tag.Key);
                AssertEqual("active", tag.Value);
            });

            await RunTest("ObjectTag_ParameterizedConstructorWithGuid", () =>
            {
                string guid = Guid.NewGuid().ToString();
                ObjectTag tag = new ObjectTag(guid, "bucket-guid", "object-guid", "k", "v");
                AssertEqual(guid, tag.GUID);
            });

            #endregion

            #region Upload

            await RunTest("Upload_DefaultConstructor", () =>
            {
                Upload upload = new Upload();
                AssertNotNull(upload.GUID);
                AssertTrue(upload.ExpirationUtc > DateTime.UtcNow, "Expiration should be in the future");
            });

            #endregion

            #region UploadPart

            await RunTest("UploadPart_DefaultConstructor", () =>
            {
                UploadPart part = new UploadPart();
                AssertNotNull(part.GUID);
                AssertEqual(1, part.PartNumber);
                AssertEqual(0, part.PartLength);
            });

            await RunTest("UploadPart_PartNumberValidation", () =>
            {
                UploadPart part = new UploadPart();
                part.PartNumber = 5000;
                AssertEqual(5000, part.PartNumber);

                AssertThrows<ArgumentOutOfRangeException>(() =>
                {
                    part.PartNumber = 0;
                });

                AssertThrows<ArgumentOutOfRangeException>(() =>
                {
                    part.PartNumber = 10001;
                });
            });

            await RunTest("UploadPart_PartLengthValidation", () =>
            {
                UploadPart part = new UploadPart();
                part.PartLength = 1024;
                AssertEqual(1024, part.PartLength);

                AssertThrows<ArgumentOutOfRangeException>(() =>
                {
                    part.PartLength = -1;
                });
            });

            #endregion

            #region BucketStatistics

            await RunTest("BucketStatistics_DefaultConstructor", () =>
            {
                BucketStatistics stats = new BucketStatistics();
                AssertEqual(0L, stats.Objects);
                AssertEqual(0L, stats.Bytes);
            });

            await RunTest("BucketStatistics_ParameterizedConstructor", () =>
            {
                // Note: the parameterized constructor does not assign its parameters to properties.
                BucketStatistics stats = new BucketStatistics("mybucket", "guid-123", 100, 2048);
                AssertNotNull(stats);
                AssertEqual(0L, stats.Objects);
                AssertEqual(0L, stats.Bytes);
            });

            #endregion

            #region HashResult

            await RunTest("HashResult_DefaultConstructor", () =>
            {
                HashResult hash = new HashResult();
                AssertNull(hash.MD5);
                AssertNull(hash.SHA1);
                AssertNull(hash.SHA256);
            });

            await RunTest("HashResult_PropertyAssignment", () =>
            {
                HashResult hash = new HashResult();
                hash.MD5 = "abc123";
                hash.SHA1 = "def456";
                hash.SHA256 = "ghi789";
                AssertEqual("abc123", hash.MD5);
                AssertEqual("def456", hash.SHA1);
                AssertEqual("ghi789", hash.SHA256);
            });

            #endregion

            #region ObjectStream

            await RunTest("ObjectStream_Constructor", () =>
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(new byte[] { 1, 2, 3 }))
                {
                    ObjectStream os = new ObjectStream("mykey", 3, ms);
                    AssertEqual("mykey", os.Key);
                    AssertEqual(3L, os.ContentLength);
                    AssertNotNull(os.Data);
                }
            });

            #endregion

            #region Enums

            await RunTest("AuthenticationResult_Enum", () =>
            {
                AssertTrue(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.Authenticated));
                AssertTrue(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.NotAuthenticated));
                AssertTrue(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.NoMaterialSupplied));
                AssertTrue(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.AccessKeyNotFound));
                AssertTrue(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.UserNotFound));
            });

            await RunTest("AuthorizationResult_Enum", () =>
            {
                AssertTrue(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.AdminAuthorized));
                AssertTrue(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.NotAuthorized));
                AssertTrue(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.PermitBucketOwnership));
                AssertTrue(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.PermitObjectOwnership));
                AssertTrue(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.PermitBucketGlobalConfig));
            });

            await RunTest("RetentionType_Enum", () =>
            {
                AssertTrue(Enum.IsDefined(typeof(RetentionType), RetentionType.NONE));
                AssertTrue(Enum.IsDefined(typeof(RetentionType), RetentionType.GOVERNANCE));
                AssertTrue(Enum.IsDefined(typeof(RetentionType), RetentionType.COMPLIANCE));
            });

            await RunTest("StorageDriverType_Enum", () =>
            {
                AssertTrue(Enum.IsDefined(typeof(StorageDriverType), StorageDriverType.Disk));
            });

            #endregion

            #region RequestMetadata

            await RunTest("RequestMetadata_DefaultConstructor", () =>
            {
                RequestMetadata md = new RequestMetadata();
                AssertNull(md.User);
                AssertNull(md.Credential);
                AssertNull(md.Bucket);
                AssertNull(md.Obj);
            });

            await RunTest("RequestMetadata_PropertyAssignment", () =>
            {
                RequestMetadata md = new RequestMetadata();
                md.User = new User("Test", "test@test.com");
                md.Authentication = AuthenticationResult.Authenticated;
                md.Authorization = AuthorizationResult.PermitBucketOwnership;

                AssertNotNull(md.User);
                AssertEqual("Test", md.User.Name);
                AssertEqual(AuthenticationResult.Authenticated, md.Authentication);
                AssertEqual(AuthorizationResult.PermitBucketOwnership, md.Authorization);
            });

            #endregion
        }
    }
}
