namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
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
        private CleanupManager _Cleanup;

        #endregion

        #region Constructors-and-Factories

        internal GetHandler(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth,
            CleanupManager cleanup)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth));
            if (cleanup == null) throw new ArgumentNullException(nameof(cleanup));

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
            _Auth = auth;
            _Cleanup = cleanup;
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("reports"))
            {
                await GetReports(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("maintenance"))
            {
                await GetMaintenance(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("effectivepermissions")
                || ctx.Http.Request.Url.Elements[1].Equals("effective-permissions"))
            {
                await GetEffectivePermissions(ctx);
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
                    await ctx.Response.Send(SerializationHelper.SerializeJson(CredentialResponseSanitizer.ForResponse(cred, false), true));
                    return;
                }
            }
            else
            {
                List<Credential> creds = _Config.GetCredentials();
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(CredentialResponseSanitizer.ForResponse(creds, false), true));
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
            string tenantId = GetTenantId(ctx);

            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                if (ctx.Http.Request.Url.Elements[2].Equals("summary"))
                {
                    await GetRequestHistorySummary(ctx);
                    return;
                }

                RequestHistory entry = _Config.GetRequestHistoryById(ctx.Http.Request.Url.Elements[2]);
                if (entry == null || !String.Equals(entry.TenantId, tenantId, StringComparison.Ordinal))
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
                EnumerationQuery query = QueryFromRequest(ctx);
                query.TenantId = tenantId;
                List<RequestHistory> entries = _Config.EnumerateRequestHistories(query).Items;
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

                result.Data.Add(bucket);

                bucketStart = bucketEnd;
            }

            string tenantId = GetTenantId(ctx);
            ForEachRequestHistory(tenantId, startUtc, endUtc, delegate (RequestHistory entry)
            {
                if (entry.CreatedUtc < startUtc || entry.CreatedUtc >= endUtc) return;

                int index = (int)((entry.CreatedUtc - startUtc).TotalSeconds / intervalSeconds);
                if (index < 0 || index >= result.Data.Count) return;

                RequestHistorySummaryBucket bucket = result.Data[index];
                if (entry.Success)
                {
                    bucket.SuccessCount++;
                    result.TotalSuccess++;
                }
                else
                {
                    bucket.FailureCount++;
                    result.TotalFailure++;
                }
            });

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
            result.RequestHistoryRetentionDays = _Settings.RequestHistoryRetentionDays;
            result.LastCleanupRunUtc = _Cleanup.LastCleanupRunUtc;
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

        private async Task GetReports(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length < 3
                || ctx.Http.Request.Url.Elements[2].Equals("requests", StringComparison.OrdinalIgnoreCase))
            {
                await SendJson(ctx, BuildRequestReport(ctx)).ConfigureAwait(false);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
        }

        private async Task GetMaintenance(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length == 2
                || ctx.Http.Request.Url.Elements[2].Equals("status", StringComparison.OrdinalIgnoreCase)
                || ctx.Http.Request.Url.Elements[2].Equals("settings", StringComparison.OrdinalIgnoreCase)
                || ctx.Http.Request.Url.Elements[2].Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                await SendJson(ctx, BuildMaintenanceStatus()).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements[2].Equals("migrationstatus", StringComparison.OrdinalIgnoreCase)
                || ctx.Http.Request.Url.Elements[2].Equals("migration-status", StringComparison.OrdinalIgnoreCase))
            {
                await SendJson(ctx, BuildMigrationStatus()).ConfigureAwait(false);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
        }

        private async Task GetEffectivePermissions(S3Context ctx)
        {
            string principalType = GetQueryValue(ctx, "principalType");
            string principalId = GetQueryValue(ctx, "principalId");
            string resourceType = GetQueryValue(ctx, "resourceType");
            string resourceId = GetQueryValue(ctx, "resourceId");
            string operation = GetQueryValue(ctx, "operation");

            if (String.IsNullOrEmpty(principalType)
                || String.IsNullOrEmpty(principalId)
                || String.IsNullOrEmpty(resourceType)
                || String.IsNullOrEmpty(operation))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
                return;
            }

            await SendJson(
                ctx,
                BuildEffectivePermission(GetTenantId(ctx), principalType, principalId, resourceType, resourceId, operation))
                .ConfigureAwait(false);
        }

        private RequestReportingResult BuildRequestReport(S3Context ctx)
        {
            DateTime startUtc = ParseUtc(GetQueryValue(ctx, "startUtc"), DateTime.UtcNow.AddHours(-1));
            DateTime endUtc = ParseUtc(GetQueryValue(ctx, "endUtc"), DateTime.UtcNow);
            if (endUtc < startUtc)
            {
                DateTime swap = startUtc;
                startUtc = endUtc;
                endUtc = swap;
            }

            string tenantId = GetTenantId(ctx);

            RequestReportingResult result = new RequestReportingResult();
            result.TenantId = tenantId;
            result.StartUtc = startUtc;
            result.EndUtc = endUtc;

            List<long> latencies = new List<long>();
            Dictionary<string, long> failedRequestTypes = new Dictionary<string, long>(StringComparer.Ordinal);
            Dictionary<string, long> accessKeys = new Dictionary<string, long>(StringComparer.Ordinal);
            Dictionary<string, long> bucketRequestCounts = new Dictionary<string, long>(StringComparer.Ordinal);

            ForEachRequestHistory(tenantId, startUtc, endUtc, delegate (RequestHistory entry)
            {
                result.RequestCount++;
                if (entry.Success)
                    result.SuccessCount++;
                else
                    result.FailureCount++;

                if (entry.DurationMs >= 0) latencies.Add(entry.DurationMs);

                if (!entry.Success && !String.IsNullOrEmpty(entry.RequestType))
                    Increment(failedRequestTypes, entry.RequestType);

                if (!String.IsNullOrEmpty(entry.AccessKey))
                    Increment(accessKeys, entry.AccessKey);

                string bucketName = ExtractBucketName(entry.RequestUrl);
                if (!String.IsNullOrEmpty(bucketName))
                    Increment(bucketRequestCounts, bucketName);
            });

            result.FailureRate = result.RequestCount == 0 ? 0 : (double)result.FailureCount / result.RequestCount;

            double minutes = Math.Max(1, (endUtc - startUtc).TotalMinutes);
            result.RequestsPerMinute = result.RequestCount / minutes;

            latencies.Sort();
            result.P50LatencyMs = Percentile(latencies, 0.50);
            result.P95LatencyMs = Percentile(latencies, 0.95);

            result.TopFailedRequestTypes = failedRequestTypes
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Take(10)
                .Select(kvp => new RequestReportingTopItem { Name = kvp.Key, Count = kvp.Value })
                .ToList();

            result.TopAccessKeys = accessKeys
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Take(10)
                .Select(kvp => new RequestReportingTopItem { Name = kvp.Key, Count = kvp.Value })
                .ToList();

            result.TopBucketsByRequestCount = bucketRequestCounts
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Take(10)
                .Select(kvp => new RequestReportingTopItem { Name = kvp.Key, Count = kvp.Value })
                .ToList();

            foreach (Bucket bucket in _Config.GetBuckets(tenantId))
            {
                BucketStatistics stats = new BucketStatistics(bucket.Name, bucket.Id, 0, 0);
                BucketClient client = _Buckets.GetClient(tenantId, bucket.Name);
                if (client != null)
                {
                    stats = client.GetFullStatistics();
                }

                result.TopBucketsByBytes.Add(new RequestReportingTopItem
                {
                    Id = bucket.Id,
                    Name = bucket.Name,
                    Count = stats.Objects,
                    Bytes = stats.Bytes
                });
            }

            result.TopBucketsByBytes = result.TopBucketsByBytes
                .OrderByDescending(b => b.Bytes)
                .ThenBy(b => b.Name)
                .Take(10)
                .ToList();

            result.GeneratedUtc = DateTime.UtcNow;
            return result;
        }

        private MaintenanceStatus BuildMaintenanceStatus()
        {
            MaintenanceStatus status = new MaintenanceStatus();
            status.RequestHistoryRetentionDays = _Settings.RequestHistoryRetentionDays;
            status.CleanupIntervalMs = _Cleanup.CleanupIntervalMs;
            status.LastCleanupRunUtc = _Cleanup.LastCleanupRunUtc;
            status.RuntimeEditableSettings.AddRange(MaintenanceSettingsMetadata.RuntimeEditableSettings());
            status.RestartRequiredSettings.AddRange(MaintenanceSettingsMetadata.RestartRequiredSettings());
            status.Configuration = EditableConfigurationSnapshot();
            status.GeneratedUtc = DateTime.UtcNow;
            return status;
        }

        private Dictionary<string, object> BuildMigrationStatus()
        {
            return new Dictionary<string, object>
            {
                { "DatabaseType", _Settings.Database.Type.ToString() },
                { "MigrationsAppliedOnStartup", true },
                { "IdempotentStartupMigrations", true },
                { "DefaultTenantSeeded", _Config.TenantExists("default") },
                { "DefaultAdminUserSeeded", _Config.GetUserById("default", "usr_default_admin") != null },
                { "DefaultCredentialSeeded", _Config.GetCredentialByAccessKey("default") != null },
                { "GeneratedUtc", DateTime.UtcNow }
            };
        }

        private Dictionary<string, object> EditableConfigurationSnapshot()
        {
            string json = SerializationHelper.SerializeJson(_Settings, true);
            JsonNode node = JsonNode.Parse(json);
            JsonObject obj = node as JsonObject ?? new JsonObject();
            RedactJsonPath(obj, "AdminApiKey");
            RedactJsonPath(obj, "Database", "Password");
            RedactJsonPath(obj, "Webserver", "Ssl", "PfxCertificatePassword");
            return JsonObjectToDictionary(obj);
        }

        private static void RedactJsonPath(JsonObject obj, params string[] path)
        {
            if (obj == null || path == null || path.Length < 1) return;

            JsonObject current = obj;
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (!current.TryGetPropertyValue(path[i], out JsonNode child)) return;
                current = child as JsonObject;
                if (current == null) return;
            }

            if (current.ContainsKey(path[path.Length - 1]))
            {
                current[path[path.Length - 1]] = MaintenanceSettingsMetadata.RedactedValue;
            }
        }

        private static Dictionary<string, object> JsonObjectToDictionary(JsonObject obj)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (KeyValuePair<string, JsonNode> curr in obj)
            {
                ret[curr.Key] = JsonNodeToObject(curr.Value);
            }

            return ret;
        }

        private static object JsonNodeToObject(JsonNode node)
        {
            if (node == null) return null;
            if (node is JsonObject obj) return JsonObjectToDictionary(obj);
            if (node is JsonArray array)
            {
                List<object> ret = new List<object>();
                foreach (JsonNode curr in array)
                {
                    ret.Add(JsonNodeToObject(curr));
                }

                return ret;
            }

            JsonValue value = node.AsValue();
            if (value.TryGetValue<JsonElement>(out JsonElement element))
            {
                return JsonElementToObject(element);
            }

            if (value.TryGetValue<string>(out string stringValue)) return stringValue;
            if (value.TryGetValue<int>(out int intValue)) return intValue;
            if (value.TryGetValue<long>(out long longValue)) return longValue;
            if (value.TryGetValue<decimal>(out decimal decimalValue)) return decimalValue;
            if (value.TryGetValue<double>(out double doubleValue)) return doubleValue;
            if (value.TryGetValue<bool>(out bool boolValue)) return boolValue;
            return value.ToJsonString();
        }

        private static object JsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue)) return intValue;
                    if (element.TryGetInt64(out long longValue)) return longValue;
                    if (element.TryGetDecimal(out decimal decimalValue)) return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.GetRawText();
            }
        }

        private EffectivePermissionResult BuildEffectivePermission(
            string tenantId,
            string principalType,
            string principalId,
            string resourceType,
            string resourceId,
            string operation)
        {
            EffectivePermissionResult result = new EffectivePermissionResult();
            result.TenantId = tenantId;
            result.PrincipalType = principalType;
            result.PrincipalId = principalId;
            result.ResourceType = resourceType;
            result.ResourceId = resourceId;
            result.Operation = operation;

            User user = null;
            Credential credential = null;

            if (principalType.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                user = _Config.GetUserById(tenantId, principalId);
                if (user == null)
                {
                    result.Reason = "User was not found.";
                    return result;
                }
            }
            else if (principalType.Equals("Credential", StringComparison.OrdinalIgnoreCase))
            {
                credential = _Config.GetCredentialById(tenantId, principalId);
                if (credential == null)
                {
                    result.Reason = "Credential was not found.";
                    return result;
                }

                user = _Config.GetUserById(tenantId, credential.UserId);
            }
            else
            {
                result.Reason = "Unsupported principal type.";
                return result;
            }

            List<RoleAssignment> assignments = _Config.EnumerateRoleAssignments(new EnumerationQuery
            {
                TenantId = tenantId,
                Limit = 1000,
                Filters = new Dictionary<string, string>
                {
                    { "principalType", principalType },
                    { "principalId", principalId }
                }
            }).Items;

            List<Permission> permissions = _Config.EnumeratePermissions(new EnumerationQuery
            {
                TenantId = tenantId,
                Limit = 1000
            }).Items;

            if (assignments != null)
            {
                foreach (RoleAssignment assignment in assignments.Where(a => a.Active))
                {
                    if (!AuthManager.AssignmentScopeMatches(tenantId, assignment, resourceType, resourceId)) continue;

                    Role role = _Config.GetRoleById(tenantId, assignment.RoleId);
                    if (role == null || !role.Active) continue;

                    result.MatchingAssignments.Add(assignment);

                    foreach (Permission permission in permissions.Where(p => p.Active && p.RoleId.Equals(role.Id, StringComparison.Ordinal)))
                    {
                        if (!AuthManager.PermissionResourceMatches(permission.ResourceType, resourceType)) continue;
                        if (!AuthManager.PermissionOperationMatches(permission.Operation, operation)) continue;

                        result.MatchingPermissions.Add(permission);
                        result.HasDecision = true;

                        if (!permission.Permit)
                        {
                            result.Permitted = false;
                            result.Reason = "RBAC deny from role " + role.Id + " permission " + permission.Id + ".";
                            result.GeneratedUtc = DateTime.UtcNow;
                            return result;
                        }

                        if (!result.Permitted)
                        {
                            result.Permitted = true;
                            result.Reason = "RBAC permit from role " + role.Id + " permission " + permission.Id + ".";
                        }
                    }
                }
            }

            if (result.HasDecision)
            {
                result.GeneratedUtc = DateTime.UtcNow;
                return result;
            }

            RequestContext requestContext = new RequestContext();
            requestContext.IsAuthenticated = true;
            requestContext.TenantId = tenantId;
            requestContext.UserId = user?.Id;
            requestContext.CredentialId = credential?.Id;
            requestContext.IsAdmin = user?.IsAdmin == true;
            requestContext.IsTenantAdmin = user?.IsTenantAdmin == true;

            if (requestContext.IsAdmin)
            {
                result.HasDecision = true;
                result.Permitted = true;
                result.IsAdminBypass = true;
                result.Reason = "Principal is a global administrator.";
            }
            else if (requestContext.IsTenantAdmin
                && AuthManager.CanTenantAdminBypass(requestContext, resourceType, resourceId))
            {
                result.HasDecision = true;
                result.Permitted = true;
                result.IsTenantAdminBypass = true;
                result.Reason = "Principal is a tenant administrator.";
            }
            else
            {
                result.Reason = "No matching RBAC permission.";
            }

            result.GeneratedUtc = DateTime.UtcNow;
            return result;
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

        private void ForEachRequestHistory(string tenantId, DateTime startUtc, DateTime endUtc, Action<RequestHistory> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            const int pageSize = 1000;
            int offset = 0;

            while (true)
            {
                EnumerationQuery query = new EnumerationQuery();
                query.TenantId = tenantId;
                query.StartUtc = startUtc;
                query.EndUtc = endUtc;
                query.Limit = pageSize;
                query.Offset = offset;
                query.SortField = "createdUtc";
                query.SortDirection = "asc";

                EnumerationResult<RequestHistory> page = _Config.EnumerateRequestHistories(query);
                if (page == null || page.Items == null || page.Items.Count < 1) break;

                foreach (RequestHistory entry in page.Items)
                {
                    action(entry);
                }

                if (!page.HasMore) break;
                offset += page.Items.Count;
            }
        }

        private static DateTime ParseUtc(string value, DateTime fallback)
        {
            if (String.IsNullOrEmpty(value)) return fallback;

            if (DateTime.TryParse(
                value,
                null,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static double Percentile(List<long> sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0) return 0;
            if (sortedValues.Count == 1) return sortedValues[0];

            double index = (sortedValues.Count - 1) * percentile;
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);
            if (lower == upper) return sortedValues[lower];

            double weight = index - lower;
            return sortedValues[lower] + ((sortedValues[upper] - sortedValues[lower]) * weight);
        }

        private static void Increment(Dictionary<string, long> values, string key)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (String.IsNullOrEmpty(key)) return;

            if (values.ContainsKey(key))
                values[key]++;
            else
                values[key] = 1;
        }

        private static string ExtractBucketName(string requestUrl)
        {
            if (String.IsNullOrEmpty(requestUrl)) return null;

            string path = requestUrl;
            int queryIndex = path.IndexOf("?", StringComparison.Ordinal);
            if (queryIndex >= 0) path = path.Substring(0, queryIndex);

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                path = uri.AbsolutePath;
            }

            path = path.Trim('/');
            if (String.IsNullOrEmpty(path)) return null;

            string firstSegment = path.Split('/')[0];
            if (String.IsNullOrEmpty(firstSegment)) return null;

            string normalized = firstSegment.ToLowerInvariant();
            if (normalized.Equals("api")
                || normalized.Equals("admin")
                || normalized.Equals("openapi.json")
                || normalized.Equals("favicon.ico")
                || normalized.Equals("robots.txt"))
            {
                return null;
            }

            return firstSegment;
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

            string startUtc = GetQueryValue(ctx, "startUtc");
            if (!String.IsNullOrEmpty(startUtc) && DateTime.TryParse(
                startUtc,
                null,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedStart))
            {
                query.StartUtc = parsedStart;
            }

            string endUtc = GetQueryValue(ctx, "endUtc");
            if (!String.IsNullOrEmpty(endUtc) && DateTime.TryParse(
                endUtc,
                null,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedEnd))
            {
                query.EndUtc = parsedEnd;
            }

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
