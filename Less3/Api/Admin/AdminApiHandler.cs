using System;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param> 
        /// <param name="config">ConfigManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param> 
        public AdminApiHandler(
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

        #region Public-Methods

        /// <summary>
        /// Process administrative API requests.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Process(S3Request req)
        {
            S3Response resp = new S3Response(req, 400, "text/plain", null, null);

            switch (req.Method)
            {
                case HttpMethod.GET:
                    resp = _GetHandler.Process(req);
                    return resp; 
                case HttpMethod.POST:
                    resp = _PostHandler.Process(req);
                    return resp;
                case HttpMethod.DELETE:
                    resp = _DeleteHandler.Process(req);
                    return resp;
            }

            return resp;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
