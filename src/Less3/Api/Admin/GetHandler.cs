namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;
    using Less3.Helpers;
    using Less3.Settings;

    /// <summary>
    /// Admin API GET handler.
    /// </summary>
    internal class GetHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth;

        #endregion

        #region Constructors-and-Factories

        internal GetHandler(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth));

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
            _Auth = auth;
        }

        #endregion

        #region Internal-Methods
         
        internal async Task Process(S3Context ctx)
        { 
            if (ctx.Http.Request.Url.Elements[1].Equals("buckets"))
            {
                await GetBuckets(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("users"))
            {
                await GetUsers(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("credentials"))
            {
                await GetCredentials(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("tenants"))
            {
                await GetTenants(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roles"))
            {
                await GetRoles(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("permissions"))
            {
                await GetPermissions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roleassignments"))
            {
                await GetRoleAssignments(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authsessions"))
            {
                await GetAuthSessions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authorizationaudit"))
            {
                await GetAuthorizationAudit(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("requesthistory"))
            {
                await GetRequestHistory(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("stats"))
            {
                await GetDashboardStatistics(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("health"))
            {
                await GetHealth(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task GetBuckets(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                Bucket bucket = _Buckets.GetById(ctx.Http.Request.Url.Elements[2]);
                if (bucket == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(bucket, true));
                    return;
                }
            }
            else
            {
                List<Bucket> buckets = _Config.GetBuckets();
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(buckets, true));
                return;
            }
        }

        private async Task GetUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                User user = _Config.GetUserById(ctx.Http.Request.Url.Elements[2]);
                if (user == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(user, true));
                    return;
                }
            }
            else
            {
                List<User> users = _Config.GetUsers(); 
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(users, true));
                return;
            }
        }

        private async Task GetCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                Credential cred = _Config.GetCredentialById(ctx.Http.Request.Url.Elements[2]);
                if (cred == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(cred, true));
                    return;
                }
            }
            else
            {
                List<Credential> creds = _Config.GetCredentials(); 
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(creds, true));
                return;
            }
        }

        private async Task GetTenants(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                string id = ctx.Http.Request.Url.Elements[2];
                if (ctx.Http.Request.Url.Elements.Length >= 4 && ctx.Http.Request.Url.Elements[3].Equals("exists"))
                {
                    await SendExists(ctx, _Config.TenantExists(id)).ConfigureAwait(false);
                    return;
                }

                Tenant tenant = _Config.GetTenantById(id);
                if (tenant == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, tenant).ConfigureAwait(false);
                return;
            }

            await SendJson(ctx, _Config.GetTenants()).ConfigureAwait(false);
        }

        private async Task GetRoles(S3Context ctx)
        {
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                string id = ctx.Http.Request.Url.Elements[2];
                if (ctx.Http.Request.Url.Elements.Length >= 4 && ctx.Http.Request.Url.Elements[3].Equals("exists"))
                {
                    await SendExists(ctx, _Config.RoleExists(tenantId, id)).ConfigureAwait(false);
                    return;
                }

                Role role = _Config.GetRoleById(tenantId, id);
                if (role == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, role).ConfigureAwait(false);
                return;
            }

            await SendJson(ctx, _Config.GetRoles(tenantId)).ConfigureAwait(false);
        }

        private async Task GetPermissions(S3Context ctx)
        {
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                string id = ctx.Http.Request.Url.Elements[2];
                if (ctx.Http.Request.Url.Elements.Length >= 4 && ctx.Http.Request.Url.Elements[3].Equals("exists"))
                {
                    await SendExists(ctx, _Config.PermissionExists(tenantId, id)).ConfigureAwait(false);
                    return;
                }

                Permission permission = _Config.GetPermissionById(tenantId, id);
                if (permission == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, permission).ConfigureAwait(false);
                return;
            }

            await SendJson(ctx, _Config.GetPermissions(tenantId)).ConfigureAwait(false);
        }

        private async Task GetRoleAssignments(S3Context ctx)
        {
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                RoleAssignment assignment = _Config.GetRoleAssignmentById(tenantId, ctx.Http.Request.Url.Elements[2]);
                if (assignment == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, assignment).ConfigureAwait(false);
                return;
            }

            EnumerationQuery query = QueryFromRequest(ctx);
            query.TenantId = tenantId;
            await SendJson(ctx, _Config.EnumerateRoleAssignments(query).Items).ConfigureAwait(false);
        }

        private async Task GetAuthSessions(S3Context ctx)
        {
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                AuthSession session = _Config.GetAuthSessionById(tenantId, ctx.Http.Request.Url.Elements[2]);
                if (session == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, session).ConfigureAwait(false);
                return;
            }

            EnumerationQuery query = QueryFromRequest(ctx);
            query.TenantId = tenantId;
            await SendJson(ctx, _Config.EnumerateAuthSessions(query).Items).ConfigureAwait(false);
        }

        private async Task GetAuthorizationAudit(S3Context ctx)
        {
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                AuthorizationAudit audit = _Config.GetAuthorizationAuditById(tenantId, ctx.Http.Request.Url.Elements[2]);
                if (audit == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, audit).ConfigureAwait(false);
                return;
            }

            EnumerationQuery query = QueryFromRequest(ctx);
            query.TenantId = tenantId;
            await SendJson(ctx, _Config.EnumerateAuthorizationAudit(query).Items).ConfigureAwait(false);
        }

        private async Task GetRequestHistory(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                if (ctx.Http.Request.Url.Elements[2].Equals("summary"))
                {
                    await GetRequestHistorySummary(ctx);
                    return;
                }

                RequestHistory entry = _Config.GetRequestHistoryById(ctx.Http.Request.Url.Elements[2]);
                if (entry == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(entry, true));
                    return;
                }
            }
            else
            {
                List<RequestHistory> entries = _Config.GetRequestHistories();
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(entries, true));
                return;
            }
        }

        private async Task GetRequestHistorySummary(S3Context ctx)
        {
            DateTime startUtc;
            DateTime endUtc;
            string interval = "hour";

            string startParam = null;
            string endParam = null;
            string intervalParam = null;

            if (ctx.Http.Request.Query.Elements != null)
            {
                startParam = ctx.Http.Request.Query.Elements["startUtc"];
                endParam = ctx.Http.Request.Query.Elements["endUtc"];
                intervalParam = ctx.Http.Request.Query.Elements["interval"];
            }

            if (!String.IsNullOrEmpty(intervalParam))
                interval = intervalParam;

            if (!String.IsNullOrEmpty(startParam) && DateTime.TryParse(startParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedStart))
                startUtc = parsedStart.ToUniversalTime();
            else
                startUtc = DateTime.UtcNow.AddHours(-24);

            if (!String.IsNullOrEmpty(endParam) && DateTime.TryParse(endParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedEnd))
                endUtc = parsedEnd.ToUniversalTime();
            else
                endUtc = DateTime.UtcNow;

            int intervalSeconds;
            switch (interval)
            {
                case "minute":
                    intervalSeconds = 60;
                    break;
                case "15minute":
                    intervalSeconds = 900;
                    break;
                case "hour":
                    intervalSeconds = 3600;
                    break;
                case "6hour":
                    intervalSeconds = 21600;
                    break;
                case "day":
                    intervalSeconds = 86400;
                    break;
                default:
                    intervalSeconds = 3600;
                    break;
            }

            List<RequestHistory> entries = _Config.GetRequestHistoriesInRange(startUtc, endUtc);

            RequestHistorySummaryResult result = new RequestHistorySummaryResult();
            result.StartUtc = startUtc;
            result.EndUtc = endUtc;
            result.Interval = interval;

            DateTime bucketStart = startUtc;
            while (bucketStart < endUtc)
            {
                DateTime bucketEnd = bucketStart.AddSeconds(intervalSeconds);
                if (bucketEnd > endUtc) bucketEnd = endUtc;

                RequestHistorySummaryBucket bucket = new RequestHistorySummaryBucket();
                bucket.TimestampUtc = bucketStart;

                if (entries != null)
                {
                    foreach (RequestHistory entry in entries)
                    {
                        if (entry.CreatedUtc >= bucketStart && entry.CreatedUtc < bucketEnd)
                        {
                            if (entry.Success)
                                bucket.SuccessCount++;
                            else
                                bucket.FailureCount++;
                        }
                    }
                }

                result.TotalSuccess += bucket.SuccessCount;
                result.TotalFailure += bucket.FailureCount;
                result.Data.Add(bucket);

                bucketStart = bucketEnd;
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(result, true));
        }

        private async Task GetDashboardStatistics(S3Context ctx)
        {
            DashboardStatistics result = new DashboardStatistics();
            List<Bucket> buckets = _Config.GetBuckets();

            if (buckets != null)
            {
                result.BucketCount = buckets.Count;

                foreach (Bucket bucket in buckets)
                {
                    BucketStatistics stats = new BucketStatistics(bucket.Name, bucket.Id, 0, 0);
                    BucketClient client = _Buckets.GetClient(bucket.TenantId, bucket.Name);

                    if (client != null)
                    {
                        stats = client.GetFullStatistics();
                    }

                    result.Buckets.Add(stats);
                    result.TotalObjectCount += stats.Objects;
                    result.TotalBytes += stats.Bytes;
                }
            }

            result.GeneratedUtc = DateTime.UtcNow;

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(result, true));
        }

        private async Task GetHealth(S3Context ctx)
        {
            AdminHealthStatus result = new AdminHealthStatus();
            result.ServerVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            result.UptimeSeconds = GetUptimeSeconds();
            result.DatabaseType = _Settings.Database.Type.ToString();
            result.StoragePath = _Settings.Storage.DiskDirectory;
            result.TempPath = _Settings.Storage.TempDirectory;
            result.StoragePathWritable = IsWritableDirectory(result.StoragePath);
            result.FreeDiskBytes = GetFreeDiskBytes(result.StoragePath);
            result.TempUploadCount = GetFileCount(result.TempPath);
            result.GeneratedUtc = DateTime.UtcNow;

            try
            {
                _Config.GetBuckets();
                result.DatabaseReachable = true;
            }
            catch
            {
                result.DatabaseReachable = false;
            }

            ctx.Response.StatusCode = result.DatabaseReachable && result.StoragePathWritable ? 200 : 503;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(result, true));
        }

        private static long GetUptimeSeconds()
        {
            try
            {
                return Convert.ToInt64((DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds);
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsWritableDirectory(string path)
        {
            if (String.IsNullOrEmpty(path)) return false;

            try
            {
                Directory.CreateDirectory(path);
                string probe = Path.Combine(path, ".less3-health-" + IdGenerator.GenerateRequestHistoryId() + ".tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static long GetFreeDiskBytes(string path)
        {
            if (String.IsNullOrEmpty(path)) return 0;

            try
            {
                DirectoryInfo directory = new DirectoryInfo(Path.GetFullPath(path));
                DriveInfo drive = new DriveInfo(directory.Root.FullName);
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        private static int GetFileCount(string path)
        {
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;

            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetTenantId(S3Context ctx)
        {
            string tenantId = GetQueryValue(ctx, "tenantId");
            if (String.IsNullOrEmpty(tenantId)) tenantId = "default";
            return tenantId;
        }

        private static string GetQueryValue(S3Context ctx, string name)
        {
            if (ctx.Http.Request.Query.Elements == null) return null;
            if (!ctx.Http.Request.Query.Elements.AllKeys.Contains(name)) return null;
            return ctx.Http.Request.Query.Elements[name];
        }

        private static EnumerationQuery QueryFromRequest(S3Context ctx)
        {
            EnumerationQuery query = new EnumerationQuery();

            string limit = GetQueryValue(ctx, "limit");
            if (!String.IsNullOrEmpty(limit) && Int32.TryParse(limit, out int parsedLimit)) query.Limit = parsedLimit;

            string offset = GetQueryValue(ctx, "offset");
            if (!String.IsNullOrEmpty(offset) && Int32.TryParse(offset, out int parsedOffset)) query.Offset = parsedOffset;

            string sortField = GetQueryValue(ctx, "sortField");
            if (!String.IsNullOrEmpty(sortField)) query.SortField = sortField;

            string sortDirection = GetQueryValue(ctx, "sortDirection");
            if (!String.IsNullOrEmpty(sortDirection)) query.SortDirection = sortDirection;

            return query;
        }

        private static async Task SendJson(S3Context ctx, object obj)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(obj, true)).ConfigureAwait(false);
        }

        private static async Task SendExists(S3Context ctx, bool exists)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(new ExistsResponse(exists), true)).ConfigureAwait(false);
        }

        private static async Task SendNotFound(S3Context ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        #endregion
    }
}
