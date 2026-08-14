namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using Less3.Database;
    using Less3.Settings;
    using Less3.Storage;
    using S3ServerLibrary;
    using SyslogLogging;

    /// <summary>
    /// Seeds the default user, credential, bucket, and sample content used by setup and Docker bootstrap.
    /// </summary>
    internal static class DefaultDataSeeder
    {
        private const string DefaultTenantId = "default";
        private const string DefaultUserId = "usr_default_admin";
        private const string DefaultCredentialId = "crd_default";
        private const string DefaultBucketId = "bkt_default";
        private const string DefaultUserEmail = "admin@less3";
        private const string DefaultUserPassword = "password";
        private const string DefaultAccessKey = "default";
        private const string DefaultSecretKey = "default";
        private const string DefaultBucketName = "default";
        private const string SourceLink = "http://github.com/jchristn/less3";
        private const string TenantAdminRoleId = "rol_builtin_tenantadmin";
        private const string SecurityAdminRoleId = "rol_builtin_securityadmin";
        private const string AuditorRoleId = "rol_builtin_auditor";
        private const string OperatorRoleId = "rol_builtin_operator";
        private const string TenantMemberRoleId = "rol_builtin_tenantmember";
        private const string CustomRoleId = "rol_builtin_custom";

        internal static void Seed(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database, ConfigManager config, Less3.Locking.ILockManager lockManager)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (lockManager == null) throw new ArgumentNullException(nameof(lockManager));

            Directory.CreateDirectory(settings.Storage.DiskDirectory);
            Directory.CreateDirectory(settings.Storage.TempDirectory);

            SeedCore(settings, logging, database, config);

            if (config.BucketExists(DefaultTenantId, DefaultBucketName))
            {
                return;
            }

            Bucket bucketConfig = new Bucket(
                DefaultBucketId,
                DefaultBucketName,
                DefaultUserId,
                StorageDriverType.Disk,
                settings.Storage.DiskDirectory + DefaultBucketName + "/Objects/");
            bucketConfig.TenantId = DefaultTenantId;
            bucketConfig.EnablePublicRead = true;
            bucketConfig.EnablePublicWrite = false;
            bucketConfig.EnableVersioning = false;

            config.AddBucket(bucketConfig);

            BucketClient bucket = new BucketClient(settings, logging, bucketConfig, database, lockManager);

            DateTime ts = DateTime.Now.ToUniversalTime();

            string htmlFile = SampleHtmlFile(SourceLink);
            string textFile = SampleTextFile(SourceLink);
            string jsonFile = SampleJsonFile(SourceLink);

            Obj obj1 = new Obj
            {
                TenantId = DefaultTenantId,
                OwnerId = DefaultUserId,
                AuthorId = DefaultUserId,
                BlobFilename = Less3.Helpers.IdGenerator.GenerateObjectId(),
                ContentLength = htmlFile.Length,
                ContentType = "text/html",
                Key = "hello.html",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(htmlFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            Obj obj2 = new Obj
            {
                TenantId = DefaultTenantId,
                OwnerId = DefaultUserId,
                AuthorId = DefaultUserId,
                BlobFilename = Less3.Helpers.IdGenerator.GenerateObjectId(),
                ContentLength = textFile.Length,
                ContentType = "text/plain",
                Key = "hello.txt",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(textFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            Obj obj3 = new Obj
            {
                TenantId = DefaultTenantId,
                OwnerId = DefaultUserId,
                AuthorId = DefaultUserId,
                BlobFilename = Less3.Helpers.IdGenerator.GenerateObjectId(),
                ContentLength = jsonFile.Length,
                ContentType = "application/json",
                Key = "hello.json",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(jsonFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            bucket.AddObject(obj1, Encoding.UTF8.GetBytes(htmlFile));
            bucket.AddObject(obj2, Encoding.UTF8.GetBytes(textFile));
            bucket.AddObject(obj3, Encoding.UTF8.GetBytes(jsonFile));
        }

        internal static void SeedCore(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database, ConfigManager config)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Directory.CreateDirectory(settings.Storage.DiskDirectory);
            Directory.CreateDirectory(settings.Storage.TempDirectory);

            // Idempotent: if the default tenant already exists the control plane has been seeded
            // (by this node on a prior run, or by another node in the cluster), so skip re-inserting
            // the built-in roles/permissions/users, which would violate their primary keys.
            if (config.TenantExists(DefaultTenantId))
            {
                logging.Debug("DefaultDataSeeder control plane already seeded; skipping");
                return;
            }

            Tenant tenant = new Tenant(DefaultTenantId, "Default");
            tenant.Active = true;
            config.AddTenant(tenant);

            SeedBuiltInRole(config, TenantAdminRoleId, "TenantAdmin", "Tenant administrator with all tenant permissions.");
            SeedBuiltInRole(config, SecurityAdminRoleId, "SecurityAdmin", "Security administrator for tenant IAM and audit surfaces.");
            SeedBuiltInRole(config, AuditorRoleId, "Auditor", "Read-only auditor for tenant security and activity records.");
            SeedBuiltInRole(config, OperatorRoleId, "Operator", "Operator for bucket and object operations.");
            SeedBuiltInRole(config, TenantMemberRoleId, "TenantMember", "Minimal tenant membership role.");
            SeedBuiltInRole(config, CustomRoleId, "Custom", "Template role for tenant-defined custom access.");

            SeedPermission(config, "per_builtin_tenantadmin_all", TenantAdminRoleId, "All", "All", true);
            SeedPermission(config, "per_builtin_security_admin", SecurityAdminRoleId, "Security", "Admin", true);
            SeedPermission(config, "per_builtin_auditor_read", AuditorRoleId, "All", "Read", true);
            SeedPermission(config, "per_builtin_operator_rw", OperatorRoleId, "Storage", "Write", true);
            SeedPermission(config, "per_builtin_tenantmember_read", TenantMemberRoleId, "Tenant", "Read", true);

            User adminUser = new User(DefaultUserId, "Less3 administrator", DefaultUserEmail);
            adminUser.TenantId = DefaultTenantId;
            adminUser.PasswordHash = DefaultUserPassword;
            adminUser.IsAdmin = true;
            adminUser.IsTenantAdmin = true;
            adminUser.Active = true;

            config.AddUser(adminUser);

            Credential defaultCredential = new Credential(DefaultCredentialId, DefaultUserId, "Default development access key", DefaultAccessKey, DefaultSecretKey, false);
            defaultCredential.TenantId = DefaultTenantId;
            defaultCredential.Active = true;
            config.AddCredential(defaultCredential);

            SeedAssignment(config, "asn_default_tenantadmin", TenantAdminRoleId, "User", DefaultUserId);
            SeedAssignment(config, "asn_default_credential_admin", TenantAdminRoleId, "Credential", DefaultCredentialId);
        }

        private static void SeedBuiltInRole(ConfigManager config, string id, string name, string description)
        {
            Role existing = config.GetRoleById(null, id);
            if (existing != null) return;

            Role role = new Role();
            role.Id = id;
            role.TenantId = null;
            role.Name = name;
            role.Description = description;
            role.IsBuiltIn = true;
            role.InheritsToChildren = true;
            role.Active = true;
            config.AddRole(role);
        }

        private static void SeedPermission(ConfigManager config, string id, string roleId, string resourceType, string operation, bool permit)
        {
            Permission existing = config.GetPermissionById(null, id);
            if (existing != null) return;

            Permission permission = new Permission();
            permission.Id = id;
            permission.TenantId = null;
            permission.RoleId = roleId;
            permission.ResourceType = resourceType;
            permission.Operation = operation;
            permission.Permit = permit;
            permission.Active = true;
            config.AddPermission(permission);
        }

        private static void SeedAssignment(
            ConfigManager config,
            string id,
            string roleId,
            string principalType,
            string principalId)
        {
            RoleAssignment existing = config.GetRoleAssignmentById(DefaultTenantId, id);
            if (existing != null) return;

            RoleAssignment assignment = new RoleAssignment();
            assignment.Id = id;
            assignment.TenantId = DefaultTenantId;
            assignment.RoleId = roleId;
            assignment.PrincipalType = principalType;
            assignment.PrincipalId = principalId;
            assignment.ResourceType = "Tenant";
            assignment.ResourceId = DefaultTenantId;
            assignment.Active = true;
            config.AddRoleAssignment(assignment);
        }

        private static string SampleHtmlFile(string link)
        {
            string html =
                "<html>" + Environment.NewLine +
                "   <head>" + Environment.NewLine +
                "      <title>&lt;3 :: Less3 :: S3-Compatible Object Storage</title>" + Environment.NewLine +
                "      <style>" + Environment.NewLine +
                "          body {" + Environment.NewLine +
                "            font-family: arial;" + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          pre {" + Environment.NewLine +
                "            background-color: #e5e7ea;" + Environment.NewLine +
                "            color: #333333; " + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          h3 {" + Environment.NewLine +
                "            color: #333333; " + Environment.NewLine +
                "            padding: 4px;" + Environment.NewLine +
                "            border: 4px;" + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          p {" + Environment.NewLine +
                "            color: #333333; " + Environment.NewLine +
                "            padding: 4px;" + Environment.NewLine +
                "            border: 4px;" + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          a {" + Environment.NewLine +
                "            background-color: #4cc468;" + Environment.NewLine +
                "            color: white;" + Environment.NewLine +
                "            padding: 4px;" + Environment.NewLine +
                "            border: 4px;" + Environment.NewLine +
                "         text-decoration: none; " + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          li {" + Environment.NewLine +
                "            padding: 6px;" + Environment.NewLine +
                "            border: 6px;" + Environment.NewLine +
                "          }" + Environment.NewLine +
                "      </style>" + Environment.NewLine +
                "   </head>" + Environment.NewLine +
                "   <body>" + Environment.NewLine +
                "      <pre>" + Environment.NewLine +
                WebUtility.HtmlEncode(Constants.Logo) +
                "      </pre>" + Environment.NewLine +
                "      <p>Congratulations, your Less3 node is running!</p>" + Environment.NewLine +
                "      <p>" + Environment.NewLine +
                "        <a href='" + link + "' target='_blank'>Source Code</a>" + Environment.NewLine +
                "      </p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        private static string SampleJsonFile(string link)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            ret.Add("Title", "Welcome to Less3");
            ret.Add("Body", "If you can see this file, your Less3 node is running!");
            ret.Add("Github", link);
            return SerializationHelper.SerializeJson(ret, true);
        }

        private static string SampleTextFile(string link)
        {
            string text =
                "Welcome to Less3!" + Environment.NewLine + Environment.NewLine +
                "If you can see this file, your Less3 node is running!  Now try " +
                "accessing this same URL in your browser, but use the .html extension!" + Environment.NewLine + Environment.NewLine +
                "Find us on Github here: " + link + Environment.NewLine + Environment.NewLine;

            return text;
        }
    }
}
