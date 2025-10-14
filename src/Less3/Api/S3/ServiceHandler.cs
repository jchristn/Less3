namespace Less3.Api.S3
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SyslogLogging;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;

    using Less3.Classes;
    using Less3.Settings;

    /// <summary>
    /// Service APIs.
    /// </summary>
    public class ServiceHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private ConfigManager _Config = null;
        private BucketManager _Buckets = null;
        private AuthManager _Auth = null;

        #endregion

        #region Constructors-and-Factories

        internal ServiceHandler(
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

        internal async Task<ListAllMyBucketsResult> ListBuckets(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authentication != AuthenticationResult.Authenticated)
            {
                _Logging.Warn(header + "requestor not authenticated");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            } 
            else
            {
                md.Authorization = AuthorizationResult.PermitService;
            }

            List<Classes.Bucket> buckets = _Buckets.GetUserBuckets(md.User.GUID);

            ListAllMyBucketsResult listBucketsResult = new ListAllMyBucketsResult();
            listBucketsResult.Owner = new S3ServerLibrary.S3Objects.Owner();
            listBucketsResult.Owner.DisplayName = md.User.Name;
            listBucketsResult.Owner.ID = md.User.Name;

            listBucketsResult.Buckets = new Buckets();
            listBucketsResult.Buckets.BucketList = new List<S3ServerLibrary.S3Objects.Bucket>();

            foreach (Classes.Bucket curr in buckets)
            {
                S3ServerLibrary.S3Objects.Bucket b = new S3ServerLibrary.S3Objects.Bucket();
                b.Name = curr.Name;
                b.CreationDate = curr.CreatedUtc;
                listBucketsResult.Buckets.BucketList.Add(b);
            }

            return listBucketsResult;
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
