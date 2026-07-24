namespace Less3
{
    using Less3.Api.Admin;
    using Less3.Api.Rest;
    using Less3.Api.S3;
    using Less3.Classes;
    using Less3.Helpers;
    using Less3.Settings;
    using S3ServerLibrary;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Database;
    using Timestamps;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Less3 is an S3-compatible object storage server.
    /// </summary>
    public class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static string _Header = "[Less3] ";
        private static string _Version;
        private static SettingsBase _Settings;
        private static LoggingModule _Logging;
        private static DatabaseDriverBase _Database;
        private static ConfigManager _Config;
        private static BucketManager _Buckets;
        private static ApiHandler _ApiHandler;
        private static AdminApiHandler _AdminApiHandler;
        private static RestApiHandler _RestApiHandler;
        private static AuthManager _Auth;
        private static CleanupManager _Cleanup;

        private static S3ServerSettings _S3Settings;
        private static S3Server _S3Server;
        private static ConsoleManager _Console;

        private static bool _Exiting = false;
        static void Main(string[] args)
        {
            _Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            LoadSettings(args);
            Welcome();
            InitializeGlobals();

            if (_Settings.EnableConsole && Environment.UserInteractive)
            {
                _Console.Worker();
            }
            else
            {
                using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset))
                {
                    AssemblyLoadContext.Default.Unloading += (ctx) => waitHandle.Set();
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        if (!_Exiting)
                        {
                            _Logging.Info(_Header + "termination signal received");
                            _Exiting = true;
                            waitHandle.Set();
                            eventArgs.Cancel = true;
                        }
                    };

                    bool waitHandleSignal = false;
                    do
                    {
                        waitHandleSignal = waitHandle.WaitOne(1000);
                    }
                    while (!waitHandleSignal);
                }

                _Logging.Info(_Header + "stopping at " + DateTime.UtcNow);
            }

            _Logging.Info(_Header + "disposing cleanup manager");
            if (_Cleanup != null) _Cleanup.Dispose();

            _S3Server.Stop();
            _Logging.Info("Less3 exiting");
        }

        private static void Welcome()
        { 
            ConsoleColor prior = Console.ForegroundColor;

            LogoColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Less3 | S3-Compatible Object Storage | v" + _Version);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("");

            if (_Settings.Webserver.Hostname.Equals("localhost") || _Settings.Webserver.Hostname.Equals("127.0.0.1"))
            {
                //                          1         2         3         4         5         6         7         8
                //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                Console.ForegroundColor = ConsoleColor.Yellow; 
                Console.WriteLine("WARNING: Less3 started on '" + _Settings.Webserver.Hostname + "'");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Less3 can only service requests from the local machine.  If you wish to serve");
                Console.WriteLine("external requests, edit the system.json file and specify a DNS-resolvable");
                Console.WriteLine("hostname in the Webserver.Hostname property.");
                Console.WriteLine("");
            }

            List<string> adminListeners = new List<string> { "*", "+", "0.0.0.0" };

            if (adminListeners.Contains(_Settings.Webserver.Hostname))
            {
                //                          1         2         3         4         5         6         7         8
                //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                Console.ForegroundColor = ConsoleColor.Cyan; 
                Console.WriteLine("NOTICE: Less3 listening on a wildcard hostname: '" + _Settings.Webserver.Hostname + "'");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Less3 must be run with administrative privileges, otherwise it will not be able");
                Console.WriteLine("to respond to incoming requests.");
                Console.WriteLine("");
            }
             
            Console.ForegroundColor = prior;
        }

        private static string LogoPlain()
        {
            // http://loveascii.com/hearts.html
            // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 

            string ret = Environment.NewLine;
            ret +=
                "  ,d88b.d88b,  " + @"  _           ____  " + Environment.NewLine +
                "  88888888888  " + @" | |___ _____|__ /  " + Environment.NewLine +
                "  `Y8888888Y'  " + @" | / -_|_-<_-<|_ \  " + Environment.NewLine +
                "    `Y888Y'    " + @" |_\___/__/__/___/  " + Environment.NewLine +
                "      `Y'      " + Environment.NewLine;

            return ret;
        }

        private static void LogoColor()
        {
            // http://loveascii.com/hearts.html
            // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 

            ConsoleColor prior = Console.ForegroundColor;

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("  ,d88b.d88b,  ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(@"  _           ____  ");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("  88888888888  ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(@" | |___ _____|__ /  ");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("  `Y8888888Y'  ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(@" | / -_|_-<_-<|_ \  ");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("    `Y888Y'    ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(@" |_\___/__/__/___/  ");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("      `Y'      ");

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.ForegroundColor = prior;
            return;
        }

        private static void LoadSettings(string[] args)
        { 
            bool initialSetup = false;
            if (args != null && args.Length >= 1)
            {
                if (String.Compare(args[0], "setup") == 0) initialSetup = true;
            }

            if (!File.Exists("system.json"))
            {
                if (IsRunningInContainer() && !initialSetup)
                {
                    BootstrapContainerSettings();
                }

                if (!File.Exists("system.json")) initialSetup = true;
            }

            if (initialSetup)
            {
                Setup setup = new Setup();
            }

            _Settings = SerializationHelper.DeserializeJson<SettingsBase>(File.ReadAllText("./system.json"));
        }

        private static void InitializeGlobals()
        {
            ConsoleColor prior = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkGray;

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing logging");
            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                _Settings.Logging.ConsoleLogging); 

            if (_Settings.Logging.DiskLogging && !String.IsNullOrEmpty(_Settings.Logging.DiskDirectory))
            {
                _Settings.Logging.DiskDirectory = _Settings.Logging.DiskDirectory.Replace("\\", "/");
                if (!_Settings.Logging.DiskDirectory.EndsWith("/")) _Settings.Logging.DiskDirectory += "/";
                if (!Directory.Exists(_Settings.Logging.DiskDirectory)) Directory.CreateDirectory(_Settings.Logging.DiskDirectory);

                _Logging.Settings.FileLogging = FileLoggingMode.FileWithDate;
                _Logging.Settings.LogFilename = _Settings.Logging.DiskDirectory + "less3.log";
            } 

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing database");
            _Database = DatabaseDriverFactory.Create(_Settings.Database, _Logging);

            Console.WriteLine("| Initializing configuration manager");
            _Config = new ConfigManager(_Settings, _Logging, _Database);
            EnsureDefaultBootstrapData();
            EnsureContainerBootstrapData();

            Console.WriteLine("| Initializing bucket manager");
            _Buckets = new BucketManager(_Settings, _Logging, _Config, _Database);

            Console.WriteLine("| Initializing authentication manager");
            _Auth = new AuthManager(_Settings, _Logging, _Config, _Buckets);

            Console.WriteLine("| Initializing cleanup manager");
            _Cleanup = new CleanupManager(_Settings, _Logging, _Config);

            Console.WriteLine("| Initializing API handler");
            _ApiHandler = new ApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            Console.WriteLine("| Initializing admin API handler");
            _AdminApiHandler = new AdminApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth, _Cleanup);

            Console.WriteLine("| Initializing REST API handler");
            _RestApiHandler = new RestApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            Console.WriteLine("| Initializing console manager");
            _Console = new ConsoleManager(_Settings, _Logging);

            Console.WriteLine("| Initializing S3 server interface");
            _S3Settings = new S3ServerSettings();
            _S3Settings.Logging.HttpRequests = _Settings.Logging.LogHttpRequests;
            _S3Settings.Logging.S3Requests = _Settings.Logging.LogS3Requests;
            _S3Settings.Logging.SignatureV4Validation = _Settings.Logging.LogSignatureValidation;
            _S3Settings.Logger = message => Console.WriteLine(LogSanitizer.Redact(message));
            _S3Settings.EnableSignatures = _Settings.ValidateSignatures;
            _S3Settings.Webserver = _Settings.Webserver;

            _S3Server = new S3Server(_S3Settings);
            _S3Server.Webserver.Routes.Preflight = PreflightRoute;

            Console.WriteLine("| " + _Settings.Webserver.Prefix);

            Console.WriteLine("| Initializing S3 server APIs");

            if (!String.IsNullOrEmpty(_Settings.BaseDomain))
            {
                Console.WriteLine("| Configured for virtual hosted URLs, base domain set to " + _Settings.BaseDomain);
                Console.WriteLine("  | Requests must follow the virtual hosted URL pattern, i.e. [bucket]." + _Settings.BaseDomain + ":" + _Settings.Webserver.Port + "/[key]");
                Console.WriteLine("  | Run as administrator/root and listen on a wildcard hostname, i.e. '*'");
            }
            else
            {
                Console.WriteLine("| No base domain specified");
                Console.WriteLine("  | Requests must use path-style hosted URLs, i.e. [hostname]/[bucket]/[key]");
            }

            _S3Server.Settings.PreRequestHandler = ctx => ExecuteWithExceptionLogging(ctx, () => PreRequestHandler(ctx));
            _S3Server.Settings.PostRequestHandler = ctx => ExecuteWithExceptionLogging(ctx, () => PostRequestHandler(ctx));
            _S3Server.Settings.DefaultRequestHandler = ctx => ExecuteWithExceptionLogging(ctx, () => DefaultRequestHandler(ctx));

            _S3Server.Service.ListBuckets = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ServiceListBuckets(ctx));
            _S3Server.Service.ServiceExists = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ServiceExists(ctx));
            _S3Server.Service.GetSecretKey = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.GetSecretKey(ctx));
            _S3Server.Service.FindMatchingBaseDomain = hostname => ExecuteWithExceptionLogging(() => _ApiHandler.FindMatchingBaseDomain(hostname));

            _S3Server.Bucket.Delete = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketDelete(ctx));
            _S3Server.Bucket.DeleteTagging = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketDeleteTagging(ctx));
            _S3Server.Bucket.Exists = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketExists(ctx));
            _S3Server.Bucket.Read = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketRead(ctx));
            _S3Server.Bucket.ReadAcl = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketReadAcl(ctx));
            _S3Server.Bucket.ReadLocation = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketReadLocation(ctx));
            _S3Server.Bucket.ReadTagging = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketReadTagging(ctx));
            _S3Server.Bucket.ReadVersions = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketReadVersions(ctx));
            _S3Server.Bucket.ReadVersioning = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketReadVersioning(ctx));
            _S3Server.Bucket.Write = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketWrite(ctx));
            _S3Server.Bucket.WriteAcl = (ctx, acp) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketWriteAcl(ctx, acp));
            _S3Server.Bucket.WriteTagging = (ctx, tagging) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketWriteTagging(ctx, tagging));
            _S3Server.Bucket.WriteVersioning = (ctx, versioning) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.BucketWriteVersioning(ctx, versioning));
            _S3Server.Bucket.ReadMultipartUploads = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ReadMultipartUploads(ctx));

            _S3Server.Object.Delete = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectDelete(ctx));
            _S3Server.Object.DeleteMultiple = (ctx, dm) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectDeleteMultiple(ctx, dm));
            _S3Server.Object.DeleteTagging = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectDeleteTagging(ctx));
            _S3Server.Object.Exists = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectExists(ctx));
            _S3Server.Object.Read = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectRead(ctx));
            _S3Server.Object.ReadAcl = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectReadAcl(ctx));
            _S3Server.Object.ReadRange = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectReadRange(ctx));
            _S3Server.Object.ReadTagging = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectReadTagging(ctx));
            _S3Server.Object.Write = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectWrite(ctx));
            _S3Server.Object.WriteAcl = (ctx, acp) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectWriteAcl(ctx, acp));
            _S3Server.Object.WriteTagging = (ctx, tagging) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ObjectWriteTagging(ctx, tagging));
            _S3Server.Object.UploadPart = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.UploadPart(ctx));
            _S3Server.Object.AbortMultipartUpload = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.AbortMultipartUpload(ctx));
            _S3Server.Object.CompleteMultipartUpload = (ctx, upload) => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.CompleteMultipartUpload(ctx, upload));
            _S3Server.Object.CreateMultipartUpload = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.CreateMultipartUpload(ctx));
            _S3Server.Object.ReadParts = ctx => ExecuteWithExceptionLogging(ctx, () => _ApiHandler.ReadParts(ctx));

            _S3Server.Start();

            Console.ForegroundColor = prior;
            Console.WriteLine("");
        }

        private static void EnsureContainerBootstrapData()
        {
            if (!IsRunningInContainer()) return;

            if (_Config.BucketExists("default", "default")) return;

            Console.WriteLine("| Seeding default container data");
            _Logging.Info(_Header + "detected empty configuration database in container, seeding default Docker data");
            DefaultDataSeeder.Seed(_Settings, _Logging, _Database, _Config);
        }

        private static void EnsureDefaultBootstrapData()
        {
            Console.WriteLine("| Seeding default tenant and control plane data");
            DefaultDataSeeder.SeedCore(_Settings, _Logging, _Database, _Config);
        }

        private static bool IsRunningInContainer()
        {
            string runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            return String.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void BootstrapContainerSettings()
        {
            Console.WriteLine("| system.json not found; generating default container configuration");

            SettingsBase settings = ContainerBootstrapSettingsFactory.CreateDefaults();
            ContainerBootstrapSettingsFactory.EnsureDirectories(settings);

            File.WriteAllText("./system.json", SerializationHelper.SerializeJson(settings, true));
        }

        private static string DefaultPage(string link)
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
                WebUtility.HtmlEncode(LogoPlain()) +
                "      </pre>" + Environment.NewLine +
                "      <p>Congratulations, your Less3 node is running!</p>" + Environment.NewLine +
                "      <p>" + Environment.NewLine +
                "        <a href='" + link + "' target='_blank'>Source Code</a>" + Environment.NewLine +
                "      </p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        private static string SwaggerUiPage()
        {
            return "<!doctype html>" + Environment.NewLine +
                "<html lang=\"en\">" + Environment.NewLine +
                "  <head>" + Environment.NewLine +
                "    <meta charset=\"utf-8\" />" + Environment.NewLine +
                "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />" + Environment.NewLine +
                "    <title>Less3 API Reference</title>" + Environment.NewLine +
                "    <link rel=\"stylesheet\" href=\"https://unpkg.com/swagger-ui-dist@5/swagger-ui.css\" />" + Environment.NewLine +
                "    <style>body{margin:0;background:#fff}.swagger-ui .topbar{display:none}</style>" + Environment.NewLine +
                "  </head>" + Environment.NewLine +
                "  <body>" + Environment.NewLine +
                "    <div id=\"swagger-ui\"></div>" + Environment.NewLine +
                "    <script src=\"https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js\"></script>" + Environment.NewLine +
                "    <script>" + Environment.NewLine +
                "      window.onload = function() {" + Environment.NewLine +
                "        SwaggerUIBundle({ url: '/openapi.json', dom_id: '#swagger-ui', deepLinking: true });" + Environment.NewLine +
                "      };" + Environment.NewLine +
                "    </script>" + Environment.NewLine +
                "  </body>" + Environment.NewLine +
                "</html>";
        }

        private static async Task PreflightRoute(HttpContextBase ctx)
        {
            NameValueCollection responseHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            string[] requestedHeaders = null;
            string headers = "";

            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string value = ctx.Request.Headers.Get(i);
                    if (String.IsNullOrEmpty(key)) continue;
                    if (String.IsNullOrEmpty(value)) continue;
                    if (String.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        requestedHeaders = value.Split(',');
                        break;
                    }
                }
            }

            if (requestedHeaders != null)
            {
                foreach (string curr in requestedHeaders)
                {
                    headers += ", " + curr;
                }
            }

            responseHeaders.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Allow-Origin", "*");
            responseHeaders.Add("Connection", "keep-alive");

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = responseHeaders;
            await ctx.Response.Send().ConfigureAwait(false);
            return;
        }

        private static async Task<bool> PreRequestHandler(S3Context ctx)
        {
            /*
             * Return true if a response was sent
             *
             */

            ctx.Http.Timestamp = new Timestamp();

            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Http.Request.Method.ToString() + " " + ctx.Http.Request.Url.RawWithoutQuery + "] ";

            while (ctx.Http.Request.Url.RawWithoutQuery.Contains("\\\\")) ctx.Http.Request.Url.RawWithoutQuery.Replace("\\\\", "\\");

            #region CORS-Headers

            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, Authorization, x-api-key, x-less3-session-token, x-amz-content-sha256, x-amz-date");
            ctx.Response.Headers.Add("Access-Control-Expose-Headers", "ETag, Content-Length, Content-Type, x-amz-request-id, x-amz-version-id");

            #endregion

            #region Enumerate

            if (_Settings.Logging.LogHttpRequests || ctx.Http.Request.QuerystringExists("logrequest"))
            {
                _Logging.Debug(Environment.NewLine + RedactSensitiveText(ctx.Http.Request.ToString()));
            }

            #endregion

            #region Misc-URLs

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET
                && ctx.Http.Request.Url.Elements.Length >= 1
                && ctx.Http.Request.Url.Elements[0].Equals("swagger", StringComparison.OrdinalIgnoreCase)
                && (ctx.Http.Request.Url.Elements.Length == 1
                    || (ctx.Http.Request.Url.Elements.Length == 2
                        && ctx.Http.Request.Url.Elements[1].Equals("index.html", StringComparison.OrdinalIgnoreCase))))
            {
                ctx.Response.ContentType = "text/html";
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(SwaggerUiPage());
                return true;
            }

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET
                && ctx.Http.Request.Url.Elements.Length == 1)
            { 
                if (ctx.Http.Request.Url.Elements[0].Equals("favicon.ico"))
                { 
                    byte[] favicon = Common.ReadBinaryFile("assets/favicon.ico");
                    ctx.Response.ContentType = "image/x-icon";
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send(favicon);
                    return true;
                }
                else if (ctx.Http.Request.Url.Elements[0].Equals("robots.txt"))
                {
                    ctx.Response.ContentType = "text/plain";
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send("User-Agent: *\r\nDisallow:\r\n");
                    return true;
                }
                else if (ctx.Http.Request.Url.Elements[0].Equals("openapi.json"))
                {
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send(OpenApiDocument());
                    return true;
                }
            }

            #endregion
             
            #region Unauthenticated-Requests

            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 1)
            {
                if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.HEAD)
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.Send();
                    return true;
                }

                if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET
                    && !ctx.Http.Request.Headers.AllKeys.Contains("Authorization"))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.Send(DefaultPage("https://github.com/jchristn/less3"));
                    return true;
                }
            }

            #endregion
             
            #region Rest-Requests

            if (ctx.Http.Request.Url.Elements.Length >= 3
                && ctx.Http.Request.Url.Elements[0].Equals("api")
                && ctx.Http.Request.Url.Elements[1].Equals("v1"))
            {
                if (IsPublicRestOperation(ctx))
                {
                    await _RestApiHandler.Process(ctx);
                    return true;
                }

                if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey))
                {
                    if (!ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                    {
                        _Logging.Warn(header + "invalid REST API key supplied: [redacted]");
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send();
                        return true;
                    }

                    switch (ctx.Http.Request.Method)
                    {
                        case WatsonWebserver.Core.HttpMethod.GET:
                        case WatsonWebserver.Core.HttpMethod.PUT:
                        case WatsonWebserver.Core.HttpMethod.POST:
                        case WatsonWebserver.Core.HttpMethod.DELETE:
                            await _RestApiHandler.Process(ctx);
                            return true;
                    }
                }

                if (ctx.Http.Request.Headers.AllKeys.Contains("x-less3-session-token"))
                {
                    if (!_Auth.TryAuthenticateBearerToken(
                        ctx.Http.Request.Headers["x-less3-session-token"],
                        ctx.Http.Request.Source.IpAddress,
                        out RequestContext requestContext,
                        out string authenticationReason))
                    {
                        _Logging.Warn(header + "invalid REST session token: " + authenticationReason);
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send(authenticationReason);
                        return true;
                    }

                    if (!AuthorizeRestRequest(ctx, requestContext, out string authorizationReason))
                    {
                        _Logging.Warn(header + "REST RBAC denied: " + authorizationReason);
                        ctx.Metadata = requestContext;
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send(authorizationReason);
                        return true;
                    }

                    ctx.Metadata = requestContext;

                    switch (ctx.Http.Request.Method)
                    {
                        case WatsonWebserver.Core.HttpMethod.GET:
                        case WatsonWebserver.Core.HttpMethod.PUT:
                        case WatsonWebserver.Core.HttpMethod.POST:
                        case WatsonWebserver.Core.HttpMethod.DELETE:
                            await _RestApiHandler.Process(ctx);
                            return true;
                    }
                }

                _Logging.Warn(header + "missing REST API key or session token");
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return true;
            }

            #endregion

            #region Admin-Requests

            if (ctx.Http.Request.Url.Elements.Length >= 2 && ctx.Http.Request.Url.Elements[0].Equals("admin"))
            {
                if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey)) 
                {
                    if (!ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                    {
                        _Logging.Warn(header + "invalid admin API key supplied: [redacted]");
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send();
                        return true;
                    }

                    switch (ctx.Http.Request.Method)
                    {
                        case WatsonWebserver.Core.HttpMethod.GET:
                        case WatsonWebserver.Core.HttpMethod.PUT:
                        case WatsonWebserver.Core.HttpMethod.POST:
                        case WatsonWebserver.Core.HttpMethod.DELETE:
                            await _AdminApiHandler.Process(ctx);
                            return true;
                    } 
                }

                if (ctx.Http.Request.Headers.AllKeys.Contains("x-less3-session-token"))
                {
                    if (!_Auth.TryAuthenticateBearerToken(
                        ctx.Http.Request.Headers["x-less3-session-token"],
                        ctx.Http.Request.Source.IpAddress,
                        out RequestContext requestContext,
                        out string authenticationReason))
                    {
                        _Logging.Warn(header + "invalid admin session token: " + authenticationReason);
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send(authenticationReason);
                        return true;
                    }

                    if (!AuthorizeAdminRequest(ctx, requestContext, out string authorizationReason))
                    {
                        _Logging.Warn(header + "admin RBAC denied: " + authorizationReason);
                        ctx.Metadata = requestContext;
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.Send(authorizationReason);
                        return true;
                    }

                    ctx.Metadata = requestContext;

                    switch (ctx.Http.Request.Method)
                    {
                        case WatsonWebserver.Core.HttpMethod.GET:
                        case WatsonWebserver.Core.HttpMethod.PUT:
                        case WatsonWebserver.Core.HttpMethod.POST:
                        case WatsonWebserver.Core.HttpMethod.DELETE:
                            await _AdminApiHandler.Process(ctx);
                            return true;
                    }
                }

                _Logging.Warn(header + "missing admin API key");
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return true;
            }

            #endregion

            #region Authenticate-and-Authorize

            RequestMetadata md = _Auth.AuthenticateAndBuildMetadata(ctx);

            switch (ctx.Request.RequestType)
            {
                case S3RequestType.ListBuckets:
                    md = _Auth.AuthorizeServiceRequest(ctx, md);
                    break;

                case S3RequestType.BucketDelete:
                case S3RequestType.BucketDeleteTags:
                case S3RequestType.BucketDeleteWebsite:
                case S3RequestType.BucketExists:
                case S3RequestType.BucketRead:
                case S3RequestType.BucketReadAcl:
                case S3RequestType.BucketReadLocation:
                case S3RequestType.BucketReadLogging:
                case S3RequestType.BucketReadTags:
                case S3RequestType.BucketReadVersioning:
                case S3RequestType.BucketReadVersions:
                case S3RequestType.BucketReadWebsite:
                case S3RequestType.BucketWrite:
                case S3RequestType.BucketWriteAcl:
                case S3RequestType.BucketWriteLogging:
                case S3RequestType.BucketWriteTags:
                case S3RequestType.BucketWriteVersioning:
                case S3RequestType.BucketWriteWebsite:
                case S3RequestType.BucketReadMultipartUploads:
                    md = _Auth.AuthorizeBucketRequest(ctx, md);
                    break;

                case S3RequestType.ObjectDelete:
                case S3RequestType.ObjectDeleteMultiple:
                case S3RequestType.ObjectDeleteTags:
                case S3RequestType.ObjectExists:
                case S3RequestType.ObjectRead:
                case S3RequestType.ObjectReadAcl:
                case S3RequestType.ObjectReadLegalHold:
                case S3RequestType.ObjectReadRange:
                case S3RequestType.ObjectReadRetention:
                case S3RequestType.ObjectReadTags:
                case S3RequestType.ObjectWrite:
                case S3RequestType.ObjectWriteAcl:
                case S3RequestType.ObjectWriteLegalHold:
                case S3RequestType.ObjectWriteRetention:
                case S3RequestType.ObjectWriteTags:
                case S3RequestType.ObjectCreateMultipartUpload:
                case S3RequestType.ObjectUploadPart:
                case S3RequestType.ObjectCompleteMultipartUpload:
                case S3RequestType.ObjectAbortMultipartUpload:
                case S3RequestType.ObjectReadParts:
                    md = _Auth.AuthorizeObjectRequest(ctx, md);
                    break; 
            }

            if (_Settings.Debug.Authentication)
            {
                ctx.Response.Headers.Add(Constants.Headers.RequestType, ctx.Request.RequestType.ToString());
                ctx.Response.Headers.Add(Constants.Headers.AuthenticationResult, md.Authentication.ToString());
                ctx.Response.Headers.Add(Constants.Headers.AuthorizedBy, md.Authorization.ToString());

                _Logging.Info(
                    header + ctx.Request.RequestType.ToString() + " " +
                    "auth result: " + 
                    md.Authentication.ToString() + "/" + md.Authorization.ToString());
            }

            ctx.Metadata = md;

            #endregion

            #region Handle-Canned-ACLs

            if ((ctx.Request.RequestType == S3RequestType.ObjectWriteAcl || ctx.Request.RequestType == S3RequestType.BucketWriteAcl) &&
                ctx.Http.Request.ContentLength == 0)
            {
                _Logging.Debug(header + "handling canned ACL request (no body)");

                if (ctx.Request.RequestType == S3RequestType.ObjectWriteAcl)
                {
                    await _ApiHandler.ObjectWriteAcl(ctx, null);
                }
                else if (ctx.Request.RequestType == S3RequestType.BucketWriteAcl)
                {
                    await _ApiHandler.BucketWriteAcl(ctx, null);
                }

                return true;
            }

            #endregion

            if (ctx.Http.Request.Query.Elements != null && ctx.Http.Request.Query.Elements.AllKeys.Contains("metadata"))
            {
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(md, true));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsPublicRestOperation(S3Context ctx)
        {
            if (ctx == null) return false;
            if (ctx.Http.Request.Method != WatsonWebserver.Core.HttpMethod.POST) return false;
            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length != 4) return false;
            if (!ctx.Http.Request.Url.Elements[2].Equals("authsessions", StringComparison.OrdinalIgnoreCase)) return false;
            if (ctx.Http.Request.Url.Elements[3].Equals("login", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctx.Http.Request.Url.Elements[3].Equals("credential-login", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctx.Http.Request.Url.Elements[3].Equals("validate", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctx.Http.Request.Url.Elements[3].Equals("revoke", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool AuthorizeRestRequest(
            S3Context ctx,
            RequestContext requestContext,
            out string reason)
        {
            reason = null;
            if (ctx == null)
            {
                reason = "Request context is missing.";
                return false;
            }

            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 3)
            {
                reason = "REST resource could not be resolved.";
                return false;
            }

            string resourceType = RestResourceType(ctx.Http.Request.Url.Elements[2]);
            string operation = RestOperation(ctx);
            string resourceId = RestResourceId(ctx);

            if (!RestRequestedTenantIsAllowed(ctx, requestContext, resourceType, operation, resourceId, out reason))
            {
                return false;
            }

            return _Auth.Authorize(requestContext, resourceType, operation, resourceId, out reason);
        }

        private static bool AuthorizeAdminRequest(
            S3Context ctx,
            RequestContext requestContext,
            out string reason)
        {
            reason = null;
            if (ctx == null)
            {
                reason = "Request context is missing.";
                return false;
            }

            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 2)
            {
                reason = "Admin resource could not be resolved.";
                return false;
            }

            string resourceType = AdminResourceType(ctx.Http.Request.Url.Elements[1]);
            string operation = AdminOperation(ctx);
            string resourceId = AdminResourceId(ctx);

            return _Auth.Authorize(requestContext, resourceType, operation, resourceId, out reason);
        }

        private static string AdminResourceType(string resourceType)
        {
            if (String.IsNullOrEmpty(resourceType)) return String.Empty;

            string normalized = resourceType.ToLowerInvariant().Replace("-", String.Empty);
            if (normalized.Equals("stats") || normalized.Equals("health")) return "Tenant";
            if (normalized.Equals("reports")) return "RequestHistory";
            if (normalized.Equals("maintenance")) return "Admin";
            if (normalized.Equals("effectivepermissions")) return "Permission";
            return RestResourceType(resourceType);
        }

        private static string AdminOperation(S3Context ctx)
        {
            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET)
            {
                if (ctx.Http.Request.Url.Elements.Length <= 2) return "Enumerate";
                return "Read";
            }

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.POST)
            {
                if (ctx.Http.Request.Url.Elements.Length >= 4
                    && ctx.Http.Request.Url.Elements[3].Equals("rotate", StringComparison.OrdinalIgnoreCase))
                {
                    return "Update";
                }

                if (ctx.Http.Request.Url.Elements.Length >= 4
                    && ctx.Http.Request.Url.Elements[3].Equals("disable", StringComparison.OrdinalIgnoreCase))
                {
                    return "Update";
                }

                if (ctx.Http.Request.Url.Elements.Length >= 3
                    && ctx.Http.Request.Url.Elements[1].Equals("maintenance", StringComparison.OrdinalIgnoreCase))
                {
                    return "Admin";
                }

                return "Create";
            }

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.PUT) return "Update";
            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.DELETE) return "Delete";

            return "Read";
        }

        private static string AdminResourceId(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length < 3) return null;

            string resource = ctx.Http.Request.Url.Elements[1];
            string candidate = ctx.Http.Request.Url.Elements[2];
            if (resource.Equals("reports", StringComparison.OrdinalIgnoreCase)
                || resource.Equals("maintenance", StringComparison.OrdinalIgnoreCase)
                || resource.Equals("effectivepermissions", StringComparison.OrdinalIgnoreCase)
                || resource.Equals("effective-permissions", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return candidate;
        }

        private static string RestResourceType(string resourceType)
        {
            if (String.IsNullOrEmpty(resourceType)) return String.Empty;

            string normalized = resourceType.ToLowerInvariant().Replace("-", String.Empty);
            if (normalized.Equals("tenants")) return "Tenant";
            if (normalized.Equals("buckets")) return "Bucket";
            if (normalized.Equals("objects")) return "Object";
            if (normalized.Equals("buckettags") || normalized.Equals("buckettag")) return "BucketTag";
            if (normalized.Equals("objecttags") || normalized.Equals("objecttag")) return "ObjectTag";
            if (normalized.Equals("bucketacls") || normalized.Equals("bucketacl")) return "BucketAcl";
            if (normalized.Equals("objectacls") || normalized.Equals("objectacl")) return "ObjectAcl";
            if (normalized.Equals("users")) return "User";
            if (normalized.Equals("credentials")) return "Credential";
            if (normalized.Equals("roles")) return "Role";
            if (normalized.Equals("permissions")) return "Permission";
            if (normalized.Equals("roleassignments") || normalized.Equals("assignments")) return "RoleAssignment";
            if (normalized.Equals("authsessions") || normalized.Equals("sessions")) return "AuthSession";
            if (normalized.Equals("authorizationaudit") || normalized.Equals("audit")) return "AuthorizationAudit";
            if (normalized.Equals("requesthistory") || normalized.Equals("requesthistories")) return "RequestHistory";

            return resourceType;
        }

        private static bool RestRequestedTenantIsAllowed(
            S3Context ctx,
            RequestContext requestContext,
            string resourceType,
            string operation,
            string resourceId,
            out string reason)
        {
            reason = null;
            if (requestContext == null)
            {
                reason = "Request context is missing.";
                return false;
            }

            if (requestContext.IsAdmin) return true;

            if (String.Equals(resourceType, "Tenant", StringComparison.OrdinalIgnoreCase)
                && String.Equals(operation, "Enumerate", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Tenant enumeration requires a global administrator.";
                return false;
            }

            foreach (string requestedTenantId in RestRequestedTenantIds(ctx, resourceType, resourceId))
            {
                if (String.IsNullOrEmpty(requestedTenantId)) continue;
                if (String.Equals(requestedTenantId, requestContext.TenantId, StringComparison.Ordinal)) continue;

                reason = "Requested tenant resource is outside the authenticated tenant.";
                return false;
            }

            return true;
        }

        private static IEnumerable<string> RestRequestedTenantIds(S3Context ctx, string resourceType, string resourceId)
        {
            string queryTenantId = QueryValue(ctx, "tenantId");
            if (!String.IsNullOrEmpty(queryTenantId)) yield return queryTenantId;

            string bodyTenantId = BodyStringProperty(ctx, "TenantId");
            if (!String.IsNullOrEmpty(bodyTenantId)) yield return bodyTenantId;

            if (String.Equals(resourceType, "Tenant", StringComparison.OrdinalIgnoreCase))
            {
                string bodyId = BodyStringProperty(ctx, "Id");
                if (!String.IsNullOrEmpty(bodyId)) yield return bodyId;
            }

            if (String.Equals(resourceType, "Tenant", StringComparison.OrdinalIgnoreCase)
                && !String.IsNullOrEmpty(resourceId))
            {
                yield return resourceId;
            }
        }

        private static string QueryValue(S3Context ctx, string name)
        {
            if (ctx?.Http?.Request?.Query?.Elements == null) return null;

            foreach (string key in ctx.Http.Request.Query.Elements.AllKeys)
            {
                if (key != null && key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return ctx.Http.Request.Query.Elements[key];
                }
            }

            return null;
        }

        private static string BodyStringProperty(S3Context ctx, string propertyName)
        {
            string body = ctx?.Request?.DataAsString;
            if (String.IsNullOrWhiteSpace(body)) return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static string RestOperation(S3Context ctx)
        {
            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET)
            {
                if (IsRestExistsOperation(ctx)) return "Exists";
                if (ctx.Http.Request.Url.Elements.Length == 3) return "Enumerate";
                return "Read";
            }

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.POST)
            {
                if (ctx.Http.Request.Url.Elements.Length == 4
                    && ctx.Http.Request.Url.Elements[3].Equals("enumerate", StringComparison.OrdinalIgnoreCase))
                {
                    return "Enumerate";
                }

                if (ctx.Http.Request.Url.Elements.Length == 4
                    && ctx.Http.Request.Url.Elements[3].Equals("exists", StringComparison.OrdinalIgnoreCase))
                {
                    return "Exists";
                }

                return "Create";
            }

            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.PUT) return "Update";
            if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.DELETE) return "Delete";
            return "Read";
        }

        private static string RestResourceId(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 4) return null;
            string id = ctx.Http.Request.Url.Elements[3];
            if (String.IsNullOrEmpty(id)) return null;
            if (id.Equals("enumerate", StringComparison.OrdinalIgnoreCase)) return null;
            if (id.Equals("exists", StringComparison.OrdinalIgnoreCase)) return null;
            return id;
        }

        private static bool IsRestExistsOperation(S3Context ctx)
        {
            return ctx.Http.Request.Url.Elements != null
                && ctx.Http.Request.Url.Elements.Length == 5
                && ctx.Http.Request.Url.Elements[4].Equals("exists", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task DefaultRequestHandler(S3Context ctx)
        {
            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        private static string OpenApiDocument()
        {
            Dictionary<string, object> response200 = new Dictionary<string, object>
            {
                { "description", "OK" }
            };
            Dictionary<string, object> response201 = new Dictionary<string, object>
            {
                { "description", "Created" }
            };
            Dictionary<string, object> response204 = new Dictionary<string, object>
            {
                { "description", "Deleted" }
            };
            Dictionary<string, object> response401 = new Dictionary<string, object>
            {
                { "description", "Unauthorized" }
            };
            Dictionary<string, object> response404 = new Dictionary<string, object>
            {
                { "description", "Not found" }
            };

            Dictionary<string, object> pathItem = new Dictionary<string, object>
            {
                {
                    "get",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "200", response200 } } }
                    }
                }
            };

            Dictionary<string, object> postPathItem = new Dictionary<string, object>
            {
                {
                    "post",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "200", response200 }, { "201", response201 }, { "401", response401 } } }
                    }
                }
            };

            Dictionary<string, object> collectionPathItem = new Dictionary<string, object>
            {
                {
                    "get",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "200", response200 }, { "401", response401 } } }
                    }
                },
                {
                    "post",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "201", response201 }, { "401", response401 } } }
                    }
                }
            };

            Dictionary<string, object> mutablePathItem = new Dictionary<string, object>
            {
                {
                    "get",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "200", response200 }, { "401", response401 }, { "404", response404 } } }
                    }
                },
                {
                    "put",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "200", response200 }, { "401", response401 }, { "404", response404 } } }
                    }
                },
                {
                    "delete",
                    new Dictionary<string, object>
                    {
                        { "responses", new Dictionary<string, object> { { "204", response204 }, { "401", response401 }, { "404", response404 } } }
                    }
                }
            };

            Dictionary<string, object> paths = new Dictionary<string, object>
            {
                { "/openapi.json", pathItem },
                { "/admin/health", pathItem },
                { "/admin/stats", pathItem },
                { "/admin/buckets", pathItem },
                { "/admin/users", pathItem },
                { "/admin/credentials", pathItem },
                { "/admin/tenants", pathItem },
                { "/admin/roles", pathItem },
                { "/admin/permissions", pathItem },
                { "/admin/roleassignments", pathItem },
                { "/admin/authsessions", pathItem },
                { "/admin/authorizationaudit", pathItem },
                { "/admin/requesthistory", pathItem },
                { "/admin/requesthistory/summary", pathItem },
                { "/admin/reports/requests", pathItem },
                { "/admin/maintenance/status", pathItem },
                { "/admin/maintenance/settings", postPathItem },
                { "/admin/maintenance/purge-request-history", postPathItem },
                { "/admin/maintenance/cleanup-temp-uploads", postPathItem },
                { "/admin/maintenance/run-cleanup", postPathItem },
                { "/admin/maintenance/verify-objects", postPathItem },
                { "/admin/maintenance/migration-status", pathItem },
                { "/admin/effectivepermissions", pathItem },
                { "/api/v1/{type}", postPathItem },
                { "/api/v1/{type}/{id}", mutablePathItem },
                { "/api/v1/{type}/enumerate", postPathItem },
                { "/api/v1/{type}/{id}/exists", pathItem },
                { "/api/v1/{type}/{operation}", postPathItem },
                { "/{bucket}", pathItem },
                { "/{bucket}/{key}", mutablePathItem }
            };

            string[] restResources = new string[]
            {
                "tenants",
                "buckets",
                "objects",
                "buckettags",
                "objecttags",
                "bucketacls",
                "objectacls",
                "users",
                "credentials",
                "roles",
                "permissions",
                "roleassignments",
                "authsessions",
                "authorizationaudit",
                "requesthistory"
            };

            foreach (string resource in restResources)
            {
                paths["/api/v1/" + resource] = collectionPathItem;
                paths["/api/v1/" + resource + "/{id}"] = mutablePathItem;
                paths["/api/v1/" + resource + "/enumerate"] = postPathItem;
                paths["/api/v1/" + resource + "/{id}/exists"] = pathItem;
            }

            paths["/api/v1/authsessions/login"] = postPathItem;
            paths["/api/v1/authsessions/credential-login"] = postPathItem;
            paths["/api/v1/authsessions/validate"] = postPathItem;
            paths["/api/v1/authsessions/revoke"] = postPathItem;

            Dictionary<string, object> stringSchema = new Dictionary<string, object>
            {
                { "type", "string" }
            };
            Dictionary<string, object> booleanSchema = new Dictionary<string, object>
            {
                { "type", "boolean" }
            };
            Dictionary<string, object> integerSchema = new Dictionary<string, object>
            {
                { "type", "integer" }
            };
            Dictionary<string, object> dateTimeSchema = new Dictionary<string, object>
            {
                { "type", "string" },
                { "format", "date-time" }
            };

            Dictionary<string, object> tenantScopedSchema = new Dictionary<string, object>
            {
                { "type", "object" },
                {
                    "properties",
                    new Dictionary<string, object>
                    {
                        { "Id", stringSchema },
                        { "TenantId", stringSchema },
                        { "CreatedUtc", dateTimeSchema }
                    }
                }
            };

            Dictionary<string, object> schemas = new Dictionary<string, object>
            {
                {
                    "Tenant",
                    new Dictionary<string, object>
                    {
                        { "type", "object" },
                        {
                            "properties",
                            new Dictionary<string, object>
                            {
                                { "Id", stringSchema },
                                { "Name", stringSchema },
                                { "Active", booleanSchema },
                                { "CreatedUtc", dateTimeSchema }
                            }
                        }
                    }
                },
                { "Bucket", tenantScopedSchema },
                { "Object", tenantScopedSchema },
                { "User", tenantScopedSchema },
                { "Credential", tenantScopedSchema },
                { "Role", tenantScopedSchema },
                { "Permission", tenantScopedSchema },
                { "RoleAssignment", tenantScopedSchema },
                { "AuthSession", tenantScopedSchema },
                { "AuthorizationAudit", tenantScopedSchema },
                { "RequestHistory", tenantScopedSchema },
                { "RequestReportingResult", tenantScopedSchema },
                { "MaintenanceStatus", tenantScopedSchema },
                { "MaintenanceActionResult", tenantScopedSchema },
                { "EffectivePermissionResult", tenantScopedSchema },
                {
                    "EnumerationQuery",
                    new Dictionary<string, object>
                    {
                        { "type", "object" },
                        {
                            "properties",
                            new Dictionary<string, object>
                            {
                                { "TenantId", stringSchema },
                                { "Limit", integerSchema },
                                { "Offset", integerSchema },
                                { "ContinuationToken", stringSchema },
                                { "SortField", stringSchema },
                                { "SortDirection", stringSchema }
                            }
                        }
                    }
                }
            };

            Dictionary<string, object> document = new Dictionary<string, object>
            {
                { "openapi", "3.1.0" },
                {
                    "info",
                    new Dictionary<string, object>
                    {
                        { "title", "Less3 Combined API" },
                        { "version", _Version ?? "3.0.0" },
                        { "description", "Combined S3, Less3 REST, and administrative API document." }
                    }
                },
                { "paths", paths },
                {
                    "components",
                    new Dictionary<string, object>
                    {
                        { "schemas", schemas }
                    }
                }
            };

            return SerializationHelper.SerializeJson(document, true);
        }

        private static async Task PostRequestHandler(S3Context ctx)
        {
            ctx.Http.Timestamp.End = DateTime.UtcNow;

            _Logging.Debug(
                ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " "
                + ctx.Http.Request.Method.ToString() + " "
                + RedactSensitiveText(ctx.Http.Request.Url.RawWithQuery) + " "
                + ctx.Request.RequestType.ToString() + " "
                + ctx.Http.Response.StatusCode + " "
                + ctx.Http.Timestamp.TotalMs + "ms");

            try
            {
                RequestHistory entry = new RequestHistory();
                entry.HttpMethod = ctx.Http.Request.Method.ToString();
                entry.RequestUrl = RedactSensitiveText(ctx.Http.Request.Url.RawWithQuery);
                entry.SourceIp = ctx.Http.Request.Source.IpAddress;
                entry.StatusCode = ctx.Http.Response.StatusCode;
                entry.Success = ctx.Http.Response.StatusCode < 400;
                entry.DurationMs = (long)ctx.Http.Timestamp.TotalMs;
                entry.RequestType = ctx.Request.RequestType.ToString();

                RequestMetadata md = ctx.Metadata as RequestMetadata;
                if (md != null)
                {
                    entry.TenantId = md.TenantId;
                    if (md.User != null) entry.UserId = md.User.Id;
                    if (md.Credential != null) entry.AccessKey = md.Credential.AccessKey;
                }

                RequestContext requestContext = ctx.Metadata as RequestContext;
                if (requestContext != null)
                {
                    entry.TenantId = requestContext.TenantId;
                    entry.UserId = requestContext.UserId;

                    if (!String.IsNullOrEmpty(requestContext.CredentialId))
                    {
                        Credential credential = _Config.GetCredentialById(requestContext.TenantId, requestContext.CredentialId);
                        if (credential != null) entry.AccessKey = credential.AccessKey;
                    }
                }

                try { entry.RequestContentType = ctx.Http.Request.ContentType; } catch { }
                try { entry.RequestBodyLength = ctx.Http.Request.ContentLength; } catch { }
                try { entry.ResponseContentType = ctx.Response.ContentType; } catch { }

                try
                {
                    if (IsTextContentType(entry.RequestContentType))
                    {
                        string reqBody = ctx.Request.DataAsString;
                        if (!String.IsNullOrEmpty(reqBody))
                        {
                            if (reqBody.Length > 16384) reqBody = reqBody.Substring(0, 16384);
                            entry.RequestBody = RedactSensitiveText(reqBody);
                        }
                    }
                }
                catch { }

                try
                {
                    if (IsTextContentType(entry.ResponseContentType))
                    {
                        string respBody = ctx.Response.DataAsString;
                        if (!String.IsNullOrEmpty(respBody))
                        {
                            if (respBody.Length > 16384) respBody = respBody.Substring(0, 16384);
                            entry.ResponseBody = RedactSensitiveText(respBody);
                        }
                    }
                }
                catch { }

                _Config.AddRequestHistory(entry);
            }
            catch (Exception e)
            {
                _Logging.Debug("PostRequestHandler failed to persist request history: " + RedactSensitiveText(e.Message));
            }
        }

        private static async Task ExecuteWithExceptionLogging(S3Context ctx, Func<Task> callback)
        {
            try
            {
                await callback().ConfigureAwait(false);
            }
            catch (S3Exception ex)
            {
                if (ShouldLogException(ex)) LogException(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static async Task<T> ExecuteWithExceptionLogging<T>(S3Context ctx, Func<Task<T>> callback)
        {
            try
            {
                return await callback().ConfigureAwait(false);
            }
            catch (S3Exception ex)
            {
                if (ShouldLogException(ex)) LogException(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static T ExecuteWithExceptionLogging<T>(S3Context ctx, Func<T> callback)
        {
            try
            {
                return callback();
            }
            catch (S3Exception ex)
            {
                if (ShouldLogException(ex)) LogException(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static T ExecuteWithExceptionLogging<T>(Func<T> callback)
        {
            try
            {
                return callback();
            }
            catch (S3Exception ex)
            {
                if (ShouldLogException(ex)) LogException(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static void LogException(Exception ex)
        {
            if (_Logging == null || ex == null) return;
            _Logging.Warn(_Header + $"exception:{Environment.NewLine}{RedactSensitiveText(ex.ToString())}");
        }

        private static bool ShouldLogException(Exception ex)
        {
            if (ex == null) return false;

            if (ex is S3Exception s3Ex)
            {
                return s3Ex.Error != null
                    && s3Ex.Error.Code == S3ServerLibrary.S3Objects.ErrorCode.InternalError;
            }

            return true;
        }

        private static bool IsTextContentType(string contentType)
        {
            if (String.IsNullOrEmpty(contentType)) return false;
            string ct = contentType.ToLower();
            return ct.Contains("text/")
                || ct.Contains("application/json")
                || ct.Contains("application/xml")
                || ct.Contains("application/x-www-form-urlencoded")
                || ct.Contains("+xml")
                || ct.Contains("+json");
        }

        private static string RedactSensitiveText(string value)
        {
            return LogSanitizer.Redact(value);
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
