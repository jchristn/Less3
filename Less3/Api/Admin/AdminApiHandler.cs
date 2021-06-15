using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using S3ServerInterface; 
using SyslogLogging;
using WatsonWebserver;

using Less3.Classes; 

namespace Less3.Api.Admin
{
    /// <summary>
    /// Admin API handler.
    /// </summary>
    public class AdminApiHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth;

        private GetHandler _GetHandler; 
        private PostHandler _PostHandler;
        private DeleteHandler _DeleteHandler;

        #endregion

        #region Constructors-and-Factories

        internal AdminApiHandler(
            Settings settings, 
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

            _GetHandler = new GetHandler(_Settings, _Logging, _Config, _Buckets, _Auth); 
            _PostHandler = new PostHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            _DeleteHandler = new DeleteHandler(_Settings, _Logging, _Config, _Buckets, _Auth); 
        }

        #endregion

        #region Internal-Methods

        internal async Task Process(S3Context ctx)
        {
            switch (ctx.Http.Request.Method)
            {
                case HttpMethod.GET:
                    await _GetHandler.Process(ctx);
                    return;
                case HttpMethod.POST:
                    await _PostHandler.Process(ctx);
                    return;
                case HttpMethod.DELETE:
                    await _DeleteHandler.Process(ctx);
                    return;
            }

            await ctx.Response.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
            return;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
