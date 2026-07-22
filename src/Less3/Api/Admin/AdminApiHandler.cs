namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    using Less3.Classes;
    using Less3.Settings;

    /// <summary>
    /// Admin API handler.
    /// </summary>
    public class AdminApiHandler
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

        private GetHandler _GetHandler;
        private PostHandler _PostHandler;
        private PutHandler _PutHandler;
        private DeleteHandler _DeleteHandler;

        #endregion

        #region Constructors-and-Factories

        internal AdminApiHandler(
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

            _GetHandler = new GetHandler(_Settings, _Logging, _Config, _Buckets, _Auth, _Cleanup);
            _PostHandler = new PostHandler(_Settings, _Logging, _Config, _Buckets, _Auth, _Cleanup);
            _PutHandler = new PutHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            _DeleteHandler = new DeleteHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
        }

        #endregion

        #region Internal-Methods

        internal async Task Process(S3Context ctx)
        {
            switch (ctx.Http.Request.Method)
            {
                case WatsonWebserver.Core.HttpMethod.GET:
                    await _GetHandler.Process(ctx);
                    return;
                case WatsonWebserver.Core.HttpMethod.POST:
                    await _PostHandler.Process(ctx);
                    return;
                case WatsonWebserver.Core.HttpMethod.PUT:
                    await _PutHandler.Process(ctx);
                    return;
                case WatsonWebserver.Core.HttpMethod.DELETE:
                    await _DeleteHandler.Process(ctx);
                    return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
            return;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
