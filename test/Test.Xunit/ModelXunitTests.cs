namespace Test.Xunit
{
    using System;
    using global::Xunit;
    using Less3.Classes;
    using Less3.Storage;

    /// <summary>
    /// Xunit tests for Less3 data model classes.
    /// </summary>
    public class ModelXunitTests
    {
        #region Bucket

        [Fact]
        public void Bucket_DefaultConstructor_SetsDefaults()
        {
            Bucket bucket = new Bucket();
            Assert.NotNull(bucket.GUID);
            Assert.NotNull(bucket.OwnerGUID);
            Assert.Equal("us-west-1", bucket.RegionString);
            Assert.Equal(StorageDriverType.Disk, bucket.StorageType);
            Assert.Equal("./disk/", bucket.DiskDirectory);
            Assert.False(bucket.EnableVersioning);
            Assert.False(bucket.EnablePublicWrite);
            Assert.False(bucket.EnablePublicRead);
        }

        [Fact]
        public void Bucket_ParameterizedConstructor_SetsProperties()
        {
            Bucket bucket = new Bucket("test-bucket", "owner-guid", StorageDriverType.Disk, "/data/", "eu-west-1");
            Assert.Equal("test-bucket", bucket.Name);
            Assert.Equal("owner-guid", bucket.OwnerGUID);
            Assert.Equal(StorageDriverType.Disk, bucket.StorageType);
            Assert.Equal("/data/", bucket.DiskDirectory);
            Assert.Equal("eu-west-1", bucket.RegionString);
        }

        [Fact]
        public void Bucket_ConstructorWithGuid_SetsGuid()
        {
            string guid = Guid.NewGuid().ToString();
            Bucket bucket = new Bucket(guid, "test-bucket", "owner-guid", StorageDriverType.Disk, "/data/");
            Assert.Equal(guid, bucket.GUID);
            Assert.Equal("test-bucket", bucket.Name);
        }

        #endregion

        #region User

        [Fact]
        public void User_DefaultConstructor_SetsGuid()
        {
            User user = new User();
            Assert.NotNull(user.GUID);
        }

        [Fact]
        public void User_ParameterizedConstructor_SetsProperties()
        {
            User user = new User("Test User", "test@example.com");
            Assert.Equal("Test User", user.Name);
            Assert.Equal("test@example.com", user.Email);
            Assert.NotNull(user.GUID);
        }

        [Fact]
        public void User_ConstructorWithGuid_SetsGuid()
        {
            string guid = Guid.NewGuid().ToString();
            User user = new User(guid, "Test User", "test@example.com");
            Assert.Equal(guid, user.GUID);
        }

        #endregion

        #region Credential

        [Fact]
        public void Credential_DefaultConstructor_SetsDefaults()
        {
            Credential cred = new Credential();
            Assert.NotNull(cred.GUID);
            Assert.NotNull(cred.UserGUID);
            Assert.False(cred.IsBase64);
        }

        [Fact]
        public void Credential_ParameterizedConstructor_SetsProperties()
        {
            Credential cred = new Credential("user-guid", "desc", "AK", "SK", true);
            Assert.Equal("user-guid", cred.UserGUID);
            Assert.Equal("desc", cred.Description);
            Assert.Equal("AK", cred.AccessKey);
            Assert.Equal("SK", cred.SecretKey);
            Assert.True(cred.IsBase64);
        }

        [Fact]
        public void Credential_ConstructorWithGuid_SetsGuid()
        {
            string guid = Guid.NewGuid().ToString();
            Credential cred = new Credential(guid, "user-guid", "desc", "ak", "sk", false);
            Assert.Equal(guid, cred.GUID);
        }

        #endregion

        #region Obj

        [Fact]
        public void Obj_DefaultConstructor_SetsDefaults()
        {
            Obj obj = new Obj();
            Assert.NotNull(obj.GUID);
            Assert.Equal("application/octet-stream", obj.ContentType);
            Assert.Equal(0L, obj.ContentLength);
            Assert.Equal(1L, obj.Version);
            Assert.False(obj.IsFolder);
            Assert.False(obj.DeleteMarker);
            Assert.Equal(RetentionType.NONE, obj.Retention);
        }

        [Fact]
        public void Obj_PropertyAssignment_Works()
        {
            Obj obj = new Obj();
            obj.Key = "folder/file.txt";
            obj.ContentType = "text/plain";
            obj.ContentLength = 1024;
            obj.Version = 3;

            Assert.Equal("folder/file.txt", obj.Key);
            Assert.Equal("text/plain", obj.ContentType);
            Assert.Equal(1024L, obj.ContentLength);
            Assert.Equal(3L, obj.Version);
        }

        #endregion

        #region BucketAcl

        [Fact]
        public void BucketAcl_DefaultConstructor_SetsDefaults()
        {
            BucketAcl acl = new BucketAcl();
            Assert.NotNull(acl.GUID);
            Assert.False(acl.PermitRead);
            Assert.False(acl.PermitWrite);
            Assert.False(acl.FullControl);
        }

        [Fact]
        public void BucketAcl_GroupAclFactory_SetsProperties()
        {
            BucketAcl acl = BucketAcl.GroupAcl("AllUsers", "issuer", "bucket", true, false, false, false, false);
            Assert.Equal("AllUsers", acl.UserGroup);
            Assert.Equal("issuer", acl.IssuedByUserGUID);
            Assert.Equal("bucket", acl.BucketGUID);
            Assert.True(acl.PermitRead);
            Assert.False(acl.PermitWrite);
        }

        [Fact]
        public void BucketAcl_UserAclFactory_SetsProperties()
        {
            BucketAcl acl = BucketAcl.UserAcl("user", "issuer", "bucket", true, true, true, true, true);
            Assert.Equal("user", acl.UserGUID);
            Assert.True(acl.FullControl);
        }

        [Fact]
        public void BucketAcl_ToString_ReturnsNonEmpty()
        {
            BucketAcl acl = BucketAcl.GroupAcl("AllUsers", "issuer", "bucket", true, false, false, false, false);
            string result = acl.ToString();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region ObjectAcl

        [Fact]
        public void ObjectAcl_DefaultConstructor_SetsDefaults()
        {
            ObjectAcl acl = new ObjectAcl();
            Assert.NotNull(acl.GUID);
            Assert.False(acl.PermitRead);
            Assert.False(acl.PermitWrite);
            Assert.False(acl.FullControl);
        }

        [Fact]
        public void ObjectAcl_GroupAclFactory_SetsProperties()
        {
            ObjectAcl acl = ObjectAcl.GroupAcl("AuthenticatedUsers", "issuer", "bucket", "object", false, true, false, false, false);
            Assert.Equal("AuthenticatedUsers", acl.UserGroup);
            Assert.False(acl.PermitRead);
            Assert.True(acl.PermitWrite);
        }

        [Fact]
        public void ObjectAcl_UserAclFactory_SetsProperties()
        {
            ObjectAcl acl = ObjectAcl.UserAcl("user", "issuer", "bucket", "object", true, true, true, true, true);
            Assert.Equal("user", acl.UserGUID);
            Assert.True(acl.FullControl);
        }

        #endregion

        #region BucketTag

        [Fact]
        public void BucketTag_DefaultConstructor_SetsGuid()
        {
            BucketTag tag = new BucketTag();
            Assert.NotNull(tag.GUID);
        }

        [Fact]
        public void BucketTag_ParameterizedConstructor_SetsProperties()
        {
            BucketTag tag = new BucketTag("bucket-guid", "env", "production");
            Assert.Equal("bucket-guid", tag.BucketGUID);
            Assert.Equal("env", tag.Key);
            Assert.Equal("production", tag.Value);
        }

        #endregion

        #region ObjectTag

        [Fact]
        public void ObjectTag_DefaultConstructor_SetsGuid()
        {
            ObjectTag tag = new ObjectTag();
            Assert.NotNull(tag.GUID);
        }

        [Fact]
        public void ObjectTag_ParameterizedConstructor_SetsProperties()
        {
            ObjectTag tag = new ObjectTag("bucket-guid", "object-guid", "status", "active");
            Assert.Equal("bucket-guid", tag.BucketGUID);
            Assert.Equal("object-guid", tag.ObjectGUID);
            Assert.Equal("status", tag.Key);
            Assert.Equal("active", tag.Value);
        }

        #endregion

        #region Upload-UploadPart

        [Fact]
        public void Upload_DefaultConstructor_SetsExpiration()
        {
            Upload upload = new Upload();
            Assert.NotNull(upload.GUID);
            Assert.True(upload.ExpirationUtc > DateTime.UtcNow);
        }

        [Fact]
        public void UploadPart_DefaultConstructor_SetsDefaults()
        {
            UploadPart part = new UploadPart();
            Assert.NotNull(part.GUID);
            Assert.Equal(1, part.PartNumber);
            Assert.Equal(0, part.PartLength);
        }

        [Fact]
        public void UploadPart_PartNumber_Validation()
        {
            UploadPart part = new UploadPart();
            part.PartNumber = 5000;
            Assert.Equal(5000, part.PartNumber);

            Assert.Throws<ArgumentOutOfRangeException>(() => part.PartNumber = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => part.PartNumber = 10001);
        }

        [Fact]
        public void UploadPart_PartLength_Validation()
        {
            UploadPart part = new UploadPart();
            part.PartLength = 1024;
            Assert.Equal(1024, part.PartLength);

            Assert.Throws<ArgumentOutOfRangeException>(() => part.PartLength = -1);
        }

        #endregion

        #region BucketStatistics

        [Fact]
        public void BucketStatistics_DefaultConstructor_SetsZeros()
        {
            BucketStatistics stats = new BucketStatistics();
            Assert.Equal(0L, stats.Objects);
            Assert.Equal(0L, stats.Bytes);
        }

        [Fact]
        public void BucketStatistics_ParameterizedConstructor_CanBeCreated()
        {
            // Note: the parameterized constructor does not assign its parameters to properties.
            BucketStatistics stats = new BucketStatistics("mybucket", "guid-123", 100, 2048);
            Assert.NotNull(stats);
            Assert.Equal(0L, stats.Objects);
            Assert.Equal(0L, stats.Bytes);
        }

        #endregion

        #region HashResult

        [Fact]
        public void HashResult_DefaultConstructor_SetsNulls()
        {
            HashResult hash = new HashResult();
            Assert.Null(hash.MD5);
            Assert.Null(hash.SHA1);
            Assert.Null(hash.SHA256);
        }

        [Fact]
        public void HashResult_PropertyAssignment_Works()
        {
            HashResult hash = new HashResult();
            hash.MD5 = "abc123";
            hash.SHA1 = "def456";
            hash.SHA256 = "ghi789";
            Assert.Equal("abc123", hash.MD5);
            Assert.Equal("def456", hash.SHA1);
            Assert.Equal("ghi789", hash.SHA256);
        }

        #endregion

        #region RequestMetadata

        [Fact]
        public void RequestMetadata_DefaultConstructor_SetsNulls()
        {
            RequestMetadata md = new RequestMetadata();
            Assert.Null(md.User);
            Assert.Null(md.Credential);
            Assert.Null(md.Bucket);
            Assert.Null(md.Obj);
        }

        [Fact]
        public void RequestMetadata_PropertyAssignment_Works()
        {
            RequestMetadata md = new RequestMetadata();
            md.User = new User("Test", "test@test.com");
            md.Authentication = AuthenticationResult.Authenticated;
            md.Authorization = AuthorizationResult.PermitBucketOwnership;

            Assert.NotNull(md.User);
            Assert.Equal("Test", md.User.Name);
            Assert.Equal(AuthenticationResult.Authenticated, md.Authentication);
            Assert.Equal(AuthorizationResult.PermitBucketOwnership, md.Authorization);
        }

        #endregion

        #region Enums

        [Fact]
        public void AuthenticationResult_AllValuesAreDefined()
        {
            Assert.True(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.Authenticated));
            Assert.True(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.NotAuthenticated));
            Assert.True(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.NoMaterialSupplied));
            Assert.True(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.AccessKeyNotFound));
            Assert.True(Enum.IsDefined(typeof(AuthenticationResult), AuthenticationResult.UserNotFound));
        }

        [Fact]
        public void AuthorizationResult_AllValuesAreDefined()
        {
            Assert.True(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.AdminAuthorized));
            Assert.True(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.NotAuthorized));
            Assert.True(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.PermitBucketOwnership));
            Assert.True(Enum.IsDefined(typeof(AuthorizationResult), AuthorizationResult.PermitObjectOwnership));
        }

        [Fact]
        public void RetentionType_AllValuesAreDefined()
        {
            Assert.True(Enum.IsDefined(typeof(RetentionType), RetentionType.NONE));
            Assert.True(Enum.IsDefined(typeof(RetentionType), RetentionType.GOVERNANCE));
            Assert.True(Enum.IsDefined(typeof(RetentionType), RetentionType.COMPLIANCE));
        }

        #endregion
    }
}
