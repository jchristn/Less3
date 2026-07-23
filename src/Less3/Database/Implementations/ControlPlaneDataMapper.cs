namespace Less3.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using Less3.Classes;
    using Less3.Storage;

    internal static class ControlPlaneDataMapper
    {
        internal static string StringValue(DataRow row, string column)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.Table.Columns.Contains(column)) return null;
            if (row[column] == null || row[column] == DBNull.Value) return null;
            return row[column].ToString();
        }

        internal static bool BoolValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return false;

            if (String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;

            return Convert.ToInt32(value) != 0;
        }

        internal static DateTime DateValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return DateTime.UtcNow;
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        internal static DateTime? NullableDateValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return null;
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        internal static int IntValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return 0;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        internal static long LongValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return 0;
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        internal static long Count(DataTable table)
        {
            if (table == null || table.Rows.Count < 1) return 0;
            return Convert.ToInt64(table.Rows[0]["cnt"]);
        }

        internal static Tenant Tenant(DataRow row)
        {
            Tenant tenant = new Tenant();
            tenant.Id = StringValue(row, "id");
            tenant.ParentId = StringValue(row, "parent_id");
            tenant.Name = StringValue(row, "name");
            tenant.Active = BoolValue(row, "active");
            tenant.CreatedUtc = DateValue(row, "createdutc");
            tenant.LastUpdateUtc = DateValue(row, "lastupdateutc");
            return tenant;
        }

        internal static Role Role(DataRow row)
        {
            Role role = new Role();
            role.Id = StringValue(row, "id");
            role.TenantId = StringValue(row, "tenant_id");
            role.Name = StringValue(row, "name");
            role.Description = StringValue(row, "description");
            role.IsBuiltIn = BoolValue(row, "isbuiltin");
            role.InheritsToChildren = BoolValue(row, "inheritstochildren");
            role.Active = BoolValue(row, "active");
            role.CreatedUtc = DateValue(row, "createdutc");
            role.LastUpdateUtc = DateValue(row, "lastupdateutc");
            return role;
        }

        internal static Permission Permission(DataRow row)
        {
            Permission permission = new Permission();
            permission.Id = StringValue(row, "id");
            permission.TenantId = StringValue(row, "tenant_id");
            permission.RoleId = StringValue(row, "role_id");
            permission.ResourceType = StringValue(row, "resourcetype");
            permission.Operation = StringValue(row, "operation");
            permission.Permit = BoolValue(row, "permit");
            permission.Active = BoolValue(row, "active");
            permission.CreatedUtc = DateValue(row, "createdutc");
            return permission;
        }

        internal static RoleAssignment RoleAssignment(DataRow row)
        {
            RoleAssignment assignment = new RoleAssignment();
            assignment.Id = StringValue(row, "id");
            assignment.TenantId = StringValue(row, "tenant_id");
            assignment.RoleId = StringValue(row, "role_id");
            assignment.PrincipalType = StringValue(row, "principaltype");
            assignment.PrincipalId = StringValue(row, "principal_id");
            assignment.ResourceType = StringValue(row, "resourcetype");
            assignment.ResourceId = StringValue(row, "resource_id");
            assignment.Active = BoolValue(row, "active");
            assignment.CreatedUtc = DateValue(row, "createdutc");
            return assignment;
        }

        internal static AuthSession AuthSession(DataRow row)
        {
            AuthSession session = new AuthSession();
            session.Id = StringValue(row, "id");
            session.TenantId = StringValue(row, "tenant_id");
            session.PrincipalType = StringValue(row, "principaltype");
            session.PrincipalId = StringValue(row, "principal_id");
            session.TokenHash = StringValue(row, "tokenhash");
            session.Active = BoolValue(row, "active");
            session.CreatedUtc = DateValue(row, "createdutc");
            session.ExpirationUtc = DateValue(row, "expirationutc");
            session.RevokedUtc = NullableDateValue(row, "revokedutc");
            session.SourceIp = StringValue(row, "sourceip");
            return session;
        }

        internal static AuthorizationAudit AuthorizationAudit(DataRow row)
        {
            AuthorizationAudit audit = new AuthorizationAudit();
            audit.Id = StringValue(row, "id");
            audit.TenantId = StringValue(row, "tenant_id");
            audit.UserId = StringValue(row, "user_id");
            audit.CredentialId = StringValue(row, "credential_id");
            audit.ResourceType = StringValue(row, "resourcetype");
            audit.ResourceId = StringValue(row, "resource_id");
            audit.Operation = StringValue(row, "operation");
            audit.Permitted = BoolValue(row, "permitted");
            audit.Reason = StringValue(row, "reason");
            audit.CreatedUtc = DateValue(row, "createdutc");
            return audit;
        }

        internal static User User(DataRow row)
        {
            User user = new User();
            user.Id = StringValue(row, "id");
            user.TenantId = StringValue(row, "tenant_id") ?? "default";
            user.Name = StringValue(row, "name");
            user.Email = StringValue(row, "email");
            user.PasswordHash = StringValue(row, "passwordhash");
            user.IsAdmin = BoolValue(row, "isadmin");
            user.IsTenantAdmin = BoolValue(row, "istenantadmin");
            user.Active = BoolValue(row, "active");
            user.CreatedUtc = DateValue(row, "createdutc");
            return user;
        }

        internal static Credential Credential(DataRow row)
        {
            Credential cred = new Credential();
            cred.Id = StringValue(row, "id");
            cred.TenantId = StringValue(row, "tenant_id") ?? "default";
            cred.UserId = StringValue(row, "user_id");
            cred.Description = StringValue(row, "description");
            cred.AccessKey = StringValue(row, "accesskey");
            cred.SecretKey = StringValue(row, "secretkey");
            cred.IsBase64 = BoolValue(row, "isbase64");
            cred.Active = BoolValue(row, "active");
            cred.LastUsedUtc = NullableDateValue(row, "lastusedutc");
            cred.LastFailedUtc = NullableDateValue(row, "lastfailedutc");
            cred.CreatedUtc = DateValue(row, "createdutc");
            return cred;
        }

        internal static Bucket Bucket(DataRow row)
        {
            Bucket bucket = new Bucket();
            bucket.Id = StringValue(row, "id");
            bucket.TenantId = StringValue(row, "tenant_id") ?? "default";
            bucket.OwnerId = StringValue(row, "owner_id");
            bucket.Name = StringValue(row, "name");
            bucket.RegionString = StringValue(row, "regionstring");
            bucket.StorageType = ParseEnum(StringValue(row, "storagetype"), StorageDriverType.Disk);
            bucket.DiskDirectory = StringValue(row, "diskdirectory");
            bucket.EnableVersioning = BoolValue(row, "enableversioning");
            bucket.EnablePublicWrite = BoolValue(row, "enablepublicwrite");
            bucket.EnablePublicRead = BoolValue(row, "enablepublicread");
            bucket.CreatedUtc = DateValue(row, "createdutc");
            return bucket;
        }

        internal static Obj Obj(DataRow row)
        {
            Obj obj = new Obj();
            obj.Id = StringValue(row, "id");
            obj.TenantId = StringValue(row, "tenant_id") ?? "default";
            obj.BucketId = StringValue(row, "bucket_id");
            obj.OwnerId = StringValue(row, "owner_id");
            obj.AuthorId = StringValue(row, "author_id");
            obj.Key = StringValue(row, "key");
            obj.ContentType = StringValue(row, "contenttype");
            obj.ContentLength = LongValue(row, "contentlength");
            obj.Version = LongValue(row, "version");
            obj.Etag = StringValue(row, "etag");
            obj.Retention = ParseEnum(StringValue(row, "retention"), RetentionType.NONE);
            obj.BlobFilename = StringValue(row, "blobfilename");
            obj.IsFolder = BoolValue(row, "isfolder");
            obj.DeleteMarker = BoolValue(row, "deletemarker");
            obj.Md5 = StringValue(row, "md5");
            obj.CreatedUtc = DateValue(row, "createdutc");
            obj.LastUpdateUtc = DateValue(row, "lastupdateutc");
            obj.LastAccessUtc = DateValue(row, "lastaccessutc");
            obj.Metadata = StringValue(row, "metadata");
            obj.ExpirationUtc = NullableDateValue(row, "expirationutc");
            return obj;
        }

        internal static BucketTag BucketTag(DataRow row)
        {
            BucketTag tag = new BucketTag();
            tag.Id = StringValue(row, "id");
            tag.TenantId = StringValue(row, "tenant_id") ?? "default";
            tag.BucketId = StringValue(row, "bucket_id");
            tag.Key = StringValue(row, "key");
            tag.Value = StringValue(row, "value");
            tag.CreatedUtc = DateValue(row, "createdutc");
            return tag;
        }

        internal static ObjectTag ObjectTag(DataRow row)
        {
            ObjectTag tag = new ObjectTag();
            tag.Id = StringValue(row, "id");
            tag.TenantId = StringValue(row, "tenant_id") ?? "default";
            tag.BucketId = StringValue(row, "bucket_id");
            tag.ObjectId = StringValue(row, "object_id");
            tag.Key = StringValue(row, "key");
            tag.Value = StringValue(row, "value");
            tag.CreatedUtc = DateValue(row, "createdutc");
            return tag;
        }

        internal static BucketAcl BucketAcl(DataRow row)
        {
            BucketAcl acl = new BucketAcl();
            acl.Id = StringValue(row, "id");
            acl.TenantId = StringValue(row, "tenant_id") ?? "default";
            acl.UserGroup = StringValue(row, "usergroup");
            acl.BucketId = StringValue(row, "bucket_id");
            acl.UserId = StringValue(row, "user_id");
            acl.IssuedByUserId = StringValue(row, "issued_by_user_id");
            acl.PermitRead = BoolValue(row, "permitread");
            acl.PermitWrite = BoolValue(row, "permitwrite");
            acl.PermitReadAcp = BoolValue(row, "permitreadacp");
            acl.PermitWriteAcp = BoolValue(row, "permitwriteacp");
            acl.FullControl = BoolValue(row, "fullcontrol");
            acl.CreatedUtc = DateValue(row, "createdutc");
            return acl;
        }

        internal static ObjectAcl ObjectAcl(DataRow row)
        {
            ObjectAcl acl = new ObjectAcl();
            acl.Id = StringValue(row, "id");
            acl.TenantId = StringValue(row, "tenant_id") ?? "default";
            acl.UserGroup = StringValue(row, "usergroup");
            acl.UserId = StringValue(row, "user_id");
            acl.IssuedByUserId = StringValue(row, "issued_by_user_id");
            acl.BucketId = StringValue(row, "bucket_id");
            acl.ObjectId = StringValue(row, "object_id");
            acl.PermitRead = BoolValue(row, "permitread");
            acl.PermitWrite = BoolValue(row, "permitwrite");
            acl.PermitReadAcp = BoolValue(row, "permitreadacp");
            acl.PermitWriteAcp = BoolValue(row, "permitwriteacp");
            acl.FullControl = BoolValue(row, "fullcontrol");
            acl.CreatedUtc = DateValue(row, "createdutc");
            return acl;
        }

        internal static Upload Upload(DataRow row)
        {
            Upload upload = new Upload();
            upload.Id = StringValue(row, "id");
            upload.TenantId = StringValue(row, "tenant_id") ?? "default";
            upload.BucketId = StringValue(row, "bucket_id");
            upload.OwnerId = StringValue(row, "owner_id");
            upload.AuthorId = StringValue(row, "author_id");
            upload.Key = StringValue(row, "key");
            upload.CreatedUtc = DateValue(row, "createdutc");
            upload.LastAccessUtc = DateValue(row, "lastaccessutc");
            upload.ExpirationUtc = DateValue(row, "expirationutc");
            upload.ContentType = StringValue(row, "contenttype");
            upload.Metadata = StringValue(row, "metadata");
            return upload;
        }

        internal static RequestHistory RequestHistory(DataRow row)
        {
            RequestHistory entry = new RequestHistory();
            entry.Id = StringValue(row, "id");
            entry.TenantId = StringValue(row, "tenant_id") ?? "default";
            entry.HttpMethod = StringValue(row, "httpmethod");
            entry.RequestUrl = StringValue(row, "requesturl");
            entry.SourceIp = StringValue(row, "sourceip");
            entry.StatusCode = IntValue(row, "statuscode");
            entry.Success = BoolValue(row, "success");
            entry.DurationMs = LongValue(row, "durationms");
            entry.RequestType = StringValue(row, "requesttype");
            entry.UserId = StringValue(row, "user_id");
            entry.AccessKey = StringValue(row, "accesskey");
            entry.RequestContentType = StringValue(row, "requestcontenttype");
            entry.RequestBodyLength = LongValue(row, "requestbodylength");
            entry.ResponseContentType = StringValue(row, "responsecontenttype");
            entry.ResponseBodyLength = LongValue(row, "responsebodylength");
            entry.RequestBody = StringValue(row, "requestbody");
            entry.ResponseBody = StringValue(row, "responsebody");
            entry.CreatedUtc = DateValue(row, "createdutc");
            return entry;
        }

        internal static List<T> List<T>(DataTable table, Func<DataRow, T> mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            List<T> results = new List<T>();
            if (table == null) return results;

            foreach (DataRow row in table.Rows)
            {
                results.Add(mapper(row));
            }

            return results;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct
        {
            if (String.IsNullOrWhiteSpace(value)) return defaultValue;
            if (Enum.TryParse<TEnum>(value, true, out TEnum parsed)) return parsed;
            return defaultValue;
        }
    }
}
