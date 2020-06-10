﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using S3ServerInterface; 
using SyslogLogging;
using Watson.ORM;
using WatsonWebserver;

using Less3.Api.Admin;
using Less3.Api.S3; 
using Less3.Classes;

namespace Less3
{
    /// <summary>
    /// Less3 is an S3-compatible object storage server.
    /// </summary>
    class Program
    {
        private static string _Version;
        private static Settings _Settings;
        private static LoggingModule _Logging;
        private static WatsonORM _ORM;
        private static ConfigManager _Config;
        private static BucketManager _Buckets;
        private static ApiHandler _ApiHandler;
        private static AdminApiHandler _AdminApiHandler;
        private static AuthManager _Auth;
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

            if (_Settings.Server.DnsHostname.Equals("localhost") || _Settings.Server.DnsHostname.Equals("127.0.0.1"))
            {
                //                          1         2         3         4         5         6         7         8
                //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                Console.ForegroundColor = ConsoleColor.Yellow; 
                Console.WriteLine("WARNING: Less3 started on '" + _Settings.Server.DnsHostname + "'");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Less3 can only service requests from the local machine.  If you wish to serve");
                Console.WriteLine("external requests, edit the system.json file and specify a DNS-resolvable");
                Console.WriteLine("hostname in the Server.DnsHostname field.");
                Console.WriteLine("");
            }

            List<string> adminListeners = new List<string> { "*", "+", "0.0.0.0" };

            if (adminListeners.Contains(_Settings.Server.DnsHostname))
            {
                //                          1         2         3         4         5         6         7         8
                //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                Console.ForegroundColor = ConsoleColor.Cyan; 
                Console.WriteLine("NOTICE: Less3 listening on a wildcard hostname: '" + _Settings.Server.DnsHostname + "'");
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

            if (!Common.FileExists("system.json")) initialSetup = true;
            if (initialSetup)
            {
                Setup setup = new Setup();
            }

            _Settings = Settings.FromFile("system.json");
            _Settings.Validate(); 
        }

        private static void InitializeGlobals()
        {
            ConsoleColor prior = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkGray;

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing logging                           : ");
            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                _Settings.Logging.ConsoleLogging,
                (Severity)_Settings.Logging.MinimumLevel,
                false,
                true,
                true,
                false,
                false,
                false); 

            if (_Settings.Logging.DiskLogging && !String.IsNullOrEmpty(_Settings.Logging.DiskDirectory))
            {
                _Settings.Logging.DiskDirectory = _Settings.Logging.DiskDirectory.Replace("\\", "/");
                if (!_Settings.Logging.DiskDirectory.EndsWith("/")) _Settings.Logging.DiskDirectory += "/";
                if (!Directory.Exists(_Settings.Logging.DiskDirectory)) Directory.CreateDirectory(_Settings.Logging.DiskDirectory);

                _Logging.FileLogging = FileLoggingMode.FileWithDate;
                _Logging.LogFilename = _Settings.Logging.DiskDirectory + "Less3.Log";
            } 
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing database                          : ");
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

            if (_Settings.Debug.DatabaseQueries) _ORM.Debug.DatabaseQueries = true;
            if (_Settings.Debug.DatabaseResults) _ORM.Debug.DatabaseResults = true;
            if (_Settings.Debug.DatabaseQueries || _Settings.Debug.DatabaseResults) _ORM.Logger = DatabaseLogger;
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing configuration manager             : ");
            _Config = new ConfigManager(_Settings, _Logging, _ORM);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing bucket manager                    : ");
            _Buckets = new BucketManager(_Settings, _Logging, _Config, _ORM);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing authentication manager            : ");
            _Auth = new AuthManager(_Settings, _Logging, _Config, _Buckets);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing API handler                       : ");
            _ApiHandler = new ApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing admin API handler                 : ");
            _AdminApiHandler = new AdminApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing console manager                   : ");
            _Console = new ConsoleManager(_Settings, _Logging);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing S3 server interface               : ");
            _S3Server = new S3Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.ListenerPort,
                _Settings.Server.Ssl,
                DefaultRequestHandler);
            Console.WriteLine("[success]");

            //             0        1         2         3         4         5
            //             123456789012345678901234567890123456789012345678901234567890
            Console.Write("| Initializing S3 server APIs                    : ");
            _S3Server.ConsoleDebug.Exceptions = true;
            _S3Server.ConsoleDebug.S3Requests = _Settings.Debug.S3Requests;
            _S3Server.BaseDomain = _Settings.Server.BaseDomain;
            _S3Server.PreRequestHandler = PreRequestHandler;

            _S3Server.Service.ListBuckets = _ApiHandler.ServiceListBuckets;

            _S3Server.Bucket.Delete = _ApiHandler.BucketDelete;
            _S3Server.Bucket.DeleteTags = _ApiHandler.BucketDeleteTags;
            _S3Server.Bucket.Exists = _ApiHandler.BucketExists;
            _S3Server.Bucket.Read = _ApiHandler.BucketRead;
            _S3Server.Bucket.ReadAcl = _ApiHandler.BucketReadAcl;
            _S3Server.Bucket.ReadLocation = _ApiHandler.BucketReadLocation;
            _S3Server.Bucket.ReadTags = _ApiHandler.BucketReadTags;
            _S3Server.Bucket.ReadVersions = _ApiHandler.BucketReadVersions;
            _S3Server.Bucket.ReadVersioning = _ApiHandler.BucketReadVersioning;
            _S3Server.Bucket.Write = _ApiHandler.BucketWrite;
            _S3Server.Bucket.WriteAcl = _ApiHandler.BucketWriteAcl;
            _S3Server.Bucket.WriteTags = _ApiHandler.BucketWriteTags;
            _S3Server.Bucket.WriteVersioning = _ApiHandler.BucketWriteVersioning;

            _S3Server.Object.Delete = _ApiHandler.ObjectDelete;
            _S3Server.Object.DeleteMultiple = _ApiHandler.ObjectDeleteMultiple;
            _S3Server.Object.DeleteTags = _ApiHandler.ObjectDeleteTags;
            _S3Server.Object.Exists = _ApiHandler.ObjectExists;
            _S3Server.Object.Read = _ApiHandler.ObjectRead;
            _S3Server.Object.ReadAcl = _ApiHandler.ObjectReadAcl;
            _S3Server.Object.ReadRange = _ApiHandler.ObjectReadRange;
            _S3Server.Object.ReadTags = _ApiHandler.ObjectReadTags;
            _S3Server.Object.Write = _ApiHandler.ObjectWrite;
            _S3Server.Object.WriteAcl = _ApiHandler.ObjectWriteAcl;
            _S3Server.Object.WriteTags = _ApiHandler.ObjectWriteTags;
            Console.WriteLine("[success]");

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

        private static async Task<bool> PreRequestHandler(S3Request req, S3Response resp)
        {
            /*
             * Return true if a response was sent
             * 
             */

            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + "] ";

            while (req.RawUrl.Contains("\\\\")) req.RawUrl.Replace("\\\\", "\\");

            #region Enumerate

            if (_Settings.Logging.LogHttpRequests)
            {
                _Logging.Debug(Environment.NewLine + req.ToString());
            }

            #endregion

            #region Misc-URLs
              
            if (req.RawUrlEntries.Count == 1)
            { 
                if (req.RawUrlEntries[0].Equals("favicon.ico"))
                { 
                    byte[] favicon = Common.ReadBinaryFile("assets/favicon.ico");
                    resp.ContentType = "image/x-icon";
                    resp.StatusCode = 200;
                    await resp.Send(favicon);
                    return true;
                }
                else if (req.RawUrlEntries[0].Equals("robots.txt"))
                {
                    resp.ContentType = "text/plain";
                    resp.StatusCode = 200;
                    await resp.Send("User-Agent: *\r\nDisallow:\r\n");
                    return true;
                }
            }

            #endregion
             
            #region Unauthenticated-Requests

            if (!req.Headers.ContainsKey("Authorization"))
            { 
                if (req.Method == WatsonWebserver.HttpMethod.GET)
                {
                    if (req.RawUrlEntries == null || req.RawUrlEntries.Count < 1)
                    {
                        resp.StatusCode = 200;
                        resp.ContentType = "text/html";
                        await resp.Send(DefaultPage("https://github.com/jchristn/less3"));
                        return true;
                    } 
                } 
            }

            #endregion
             
            #region Admin-Requests

            if (req.RawUrlEntries.Count >= 2 && req.RawUrlEntries[0].Equals("admin"))
            {
                if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
                {
                    if (!req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
                    {
                        _Logging.Warn(header + "invalid admin API key supplied: " + req.Headers[_Settings.Server.HeaderApiKey]);
                        resp.StatusCode = 401;
                        req.ContentType = "text/plain";
                        await resp.Send();
                        return true;
                    }

                    switch (req.Method)
                    {
                        case HttpMethod.GET:
                        case HttpMethod.PUT:
                        case HttpMethod.POST:
                        case HttpMethod.DELETE:
                            await _AdminApiHandler.Process(req, resp);
                            return true;
                    } 
                }
            }

            #endregion

            #region Authenticate-and-Authorize

            RequestMetadata md = _Auth.AuthenticateAndBuildMetadata(req);

            switch (req.RequestType)
            {
                case S3RequestType.ListBuckets:
                    md = _Auth.AuthorizeServiceRequest(req, md);
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
                    md = _Auth.AuthorizeBucketRequest(req, md);
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
                    md = _Auth.AuthorizeObjectRequest(req, md);
                    break; 
            }

            if (_Settings.Debug.Authentication)
            {
                resp.Headers.Add("X-Request-Type", req.RequestType.ToString());
                resp.Headers.Add("X-Authentication-Result", md.Authentication.ToString());
                resp.Headers.Add("X-Authorized-By", md.Authorization.ToString());

                _Logging.Info(
                    header + req.RequestType.ToString() + " " +
                    "auth result: " + 
                    md.Authentication.ToString() + "/" + md.Authorization.ToString());
            }

            req.UserMetadata.Add("RequestMetadata", md);

            #endregion

            if (req.Querystring != null && req.Querystring.ContainsKey("metadata"))
            {
                resp.ContentType = "application/json";
                await resp.Send(Common.SerializeJson(md, true));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static async Task DefaultRequestHandler(S3Request req, S3Response resp)
        {
            await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
        }

        private static void DatabaseLogger(string msg)
        {
            _Logging.Debug(msg);
        }
    }
}
