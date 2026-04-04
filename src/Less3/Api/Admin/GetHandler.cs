namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("requesthistory"))
            {
                await GetRequestHistory(ctx);
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
                Bucket bucket = _Buckets.GetByGuid(ctx.Http.Request.Url.Elements[2]);
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
                User user = _Config.GetUserByGuid(ctx.Http.Request.Url.Elements[2]);
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
                Credential cred = _Config.GetCredentialByGuid(ctx.Http.Request.Url.Elements[2]);
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

        private async Task GetRequestHistory(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                if (ctx.Http.Request.Url.Elements[2].Equals("summary"))
                {
                    await GetRequestHistorySummary(ctx);
                    return;
                }

                RequestHistory entry = _Config.GetRequestHistoryByGuid(ctx.Http.Request.Url.Elements[2]);
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

        #endregion
    }
}
