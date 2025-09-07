namespace Less3
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;
    using Watson.ORM;
    using WatsonWebserver;

    using Less3.Api.Admin;
    using Less3.Api.S3;
    using Less3.Classes;
    using System.Linq;

    /// <summary>
    /// Less3 is an S3-compatible object storage server.
    /// </summary>
    public class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static string _Version;
        private static Settings _Settings;
        private static LoggingModule _Logging;
        private static WatsonORM _ORM;
        private static ConfigManager _Config;
        private static BucketManager _Buckets;
        private static ApiHandler _ApiHandler;
        private static AdminApiHandler _AdminApiHandler;
        private static AuthManager _Auth;

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

            #region Wait-for-Server-Thread

            if (_Settings.EnableConsole && Environment.UserInteractive)
            {
                _Console.Worker();
            }
            else
            {
                EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);
                bool waitHandleSignal = false;
                do
                {
                    if (_Exiting) break;
                    waitHandleSignal = waitHandle.WaitOne(1000);
                }
                while (!waitHandleSignal);
            }

            _S3Server.Stop();
            _Logging.Info("Less3 exiting");

            #endregion
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

            if (!File.Exists("system.json")) initialSetup = true;
            if (initialSetup)
            {
                Setup setup = new Setup();
            }

            _Settings = Settings.FromFile("system.json");
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
            _ORM = new WatsonORM(_Settings.Database);
            _ORM.InitializeDatabase();
            _ORM.InitializeTable(typeof(Bucket));
            _ORM.InitializeTable(typeof(BucketAcl));
            _ORM.InitializeTable(typeof(BucketTag));
            _ORM.InitializeTable(typeof(Credential));
            _ORM.InitializeTable(typeof(Obj));
            _ORM.InitializeTable(typeof(ObjectAcl));
            _ORM.InitializeTable(typeof(ObjectTag));
            _ORM.InitializeTable(typeof(User));

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing configuration manager");
            _Config = new ConfigManager(_Settings, _Logging, _ORM);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing bucket manager");
            _Buckets = new BucketManager(_Settings, _Logging, _Config, _ORM);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing authentication manager");
            _Auth = new AuthManager(_Settings, _Logging, _Config, _Buckets);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing API handler");
            _ApiHandler = new ApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing admin API handler");
            _AdminApiHandler = new AdminApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing console manager");
            _Console = new ConsoleManager(_Settings, _Logging);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("| Initializing S3 server interface");
            _S3Settings = new S3ServerSettings();
            _S3Settings.Logging.HttpRequests = _Settings.Logging.LogHttpRequests;
            _S3Settings.Logging.S3Requests = _Settings.Logging.LogS3Requests;
            _S3Settings.Logging.SignatureV4Validation = _Settings.Logging.LogSignatureValidation;
            _S3Settings.Logger = Console.WriteLine;
            _S3Settings.EnableSignatures = _Settings.ValidateSignatures;
            _S3Settings.Webserver = _Settings.Webserver;

            _S3Server = new S3Server(_S3Settings);

            Console.WriteLine("| " + _Settings.Webserver.Prefix);

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
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

            _S3Server.Settings.PreRequestHandler = PreRequestHandler;
            _S3Server.Settings.PostRequestHandler = PostRequestHandler;
            _S3Server.Settings.DefaultRequestHandler = DefaultRequestHandler;

            _S3Server.Service.ListBuckets = _ApiHandler.ServiceListBuckets;
            _S3Server.Service.ServiceExists = _ApiHandler.ServiceExists;
            _S3Server.Service.FindMatchingBaseDomain = _ApiHandler.FindMatchingBaseDomain;

            _S3Server.Bucket.Delete = _ApiHandler.BucketDelete;
            _S3Server.Bucket.DeleteTagging = _ApiHandler.BucketDeleteTagging;
            _S3Server.Bucket.Exists = _ApiHandler.BucketExists;
            _S3Server.Bucket.Read = _ApiHandler.BucketRead;
            _S3Server.Bucket.ReadAcl = _ApiHandler.BucketReadAcl;
            _S3Server.Bucket.ReadLocation = _ApiHandler.BucketReadLocation;
            _S3Server.Bucket.ReadTagging = _ApiHandler.BucketReadTagging;
            _S3Server.Bucket.ReadVersions = _ApiHandler.BucketReadVersions;
            _S3Server.Bucket.ReadVersioning = _ApiHandler.BucketReadVersioning;
            _S3Server.Bucket.Write = _ApiHandler.BucketWrite;
            _S3Server.Bucket.WriteAcl = _ApiHandler.BucketWriteAcl;
            _S3Server.Bucket.WriteTagging = _ApiHandler.BucketWriteTagging;
            _S3Server.Bucket.WriteVersioning = _ApiHandler.BucketWriteVersioning;

            _S3Server.Object.Delete = _ApiHandler.ObjectDelete;
            _S3Server.Object.DeleteMultiple = _ApiHandler.ObjectDeleteMultiple;
            _S3Server.Object.DeleteTagging = _ApiHandler.ObjectDeleteTagging;
            _S3Server.Object.Exists = _ApiHandler.ObjectExists;
            _S3Server.Object.Read = _ApiHandler.ObjectRead;
            _S3Server.Object.ReadAcl = _ApiHandler.ObjectReadAcl;
            _S3Server.Object.ReadRange = _ApiHandler.ObjectReadRange;
            _S3Server.Object.ReadTagging = _ApiHandler.ObjectReadTagging;
            _S3Server.Object.Write = _ApiHandler.ObjectWrite;
            _S3Server.Object.WriteAcl = _ApiHandler.ObjectWriteAcl;
            _S3Server.Object.WriteTagging = _ApiHandler.ObjectWriteTagging;
            _S3Server.Start();

            Console.ForegroundColor = prior;
            Console.WriteLine("");
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

        private static bool ExitApplication()
        {
            _Logging.Info("Less3 exiting due to console request");
            _Exiting = true; 
            return true;
        }

        private static async Task<bool> PreRequestHandler(S3Context ctx)
        {
            /*
             * Return true if a response was sent
             * 
             */
                        
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Http.Request.Method.ToString() + " " + ctx.Http.Request.Url.RawWithoutQuery + "] ";

            while (ctx.Http.Request.Url.RawWithoutQuery.Contains("\\\\")) ctx.Http.Request.Url.RawWithoutQuery.Replace("\\\\", "\\");

            #region Enumerate

            if (_Settings.Logging.LogHttpRequests || ctx.Http.Request.QuerystringExists("logrequest"))
            {
                _Logging.Debug(Environment.NewLine + ctx.Http.Request.ToString());
            }

            #endregion

            #region Misc-URLs
              
            if (ctx.Http.Request.Url.Elements.Length == 1)
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
            }

            #endregion
             
            #region Unauthenticated-Requests

            if (!ctx.Http.Request.Headers.AllKeys.Contains("Authorization"))
            { 
                if (ctx.Http.Request.Method == WatsonWebserver.Core.HttpMethod.GET)
                {
                    if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 1)
                    {
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = "text/html";
                        await ctx.Response.Send(DefaultPage("https://github.com/jchristn/less3"));
                        return true;
                    } 
                } 
            }

            #endregion
             
            #region Admin-Requests

            if (ctx.Http.Request.Url.Elements.Length >= 2 && ctx.Http.Request.Url.Elements[0].Equals("admin"))
            {
                if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey)) 
                {
                    if (!ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                    {
                        _Logging.Warn(header + "invalid admin API key supplied: " + ctx.Http.Request.Headers[_Settings.HeaderApiKey]);
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

        private static async Task DefaultRequestHandler(S3Context ctx)
        {
            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        private static async Task PostRequestHandler(S3Context ctx)
        {
            ctx.Http.Timestamp.End = DateTime.UtcNow;
            _Logging.Debug(
                ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " 
                + ctx.Http.Request.Method.ToString() + " " 
                + ctx.Http.Request.Url.RawWithQuery + " "
                + ctx.Request.RequestType.ToString() + " "
                + ctx.Http.Response.StatusCode + " " 
                + ctx.Http.Timestamp.TotalMs + "ms");
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
