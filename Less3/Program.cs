using System;
using System.Diagnostics;
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
        static Settings _Settings;
        static LoggingModule _Logging;
        static ConfigManager _Config;
        static BucketManager _Buckets;
        static ApiHandler _ApiHandler;
        static AdminApiHandler _AdminApiHandler;
        static AuthManager _Auth;
        static S3Server _S3Server;
        static ConsoleManager _Console;

        static bool _Exiting = false;

        static void Main(string[] args)
        { 
            #region Load-Settings

            bool initialSetup = false;
            if (args != null && args.Length >= 1)
            {
                if (String.Compare(args[0], "setup") == 0) initialSetup = true;
            }

            if (!Common.FileExists("System.json")) initialSetup = true;
            if (initialSetup)
            {
                Setup setup = new Setup();
            }

            _Settings = Settings.FromFile("System.json");
            _Settings.Validate();

            Welcome(); 

            #endregion

            #region Initialize-Globals
             
            _Logging = new LoggingModule(
                _Settings.Syslog.ServerIp,
                _Settings.Syslog.ServerPort,
                _Settings.Syslog.ConsoleLogging,
                (LoggingModule.Severity)_Settings.Syslog.MinimumLevel,
                false,
                true,
                true,
                false,
                false,
                false);

            _Config = new ConfigManager(_Settings, _Logging);

            _Buckets = new BucketManager(_Settings, _Logging, _Config);

            _Auth = new AuthManager(_Settings, _Logging, _Config, _Buckets);

            _ApiHandler = new ApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            _AdminApiHandler = new AdminApiHandler(_Settings, _Logging, _Config, _Buckets, _Auth);

            _Console = new ConsoleManager(_Settings, _Logging);

            _S3Server = new S3Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.ListenerPort,
                _Settings.Server.Ssl,
                PreRequestHandler,
                DefaultRequestHandler);
            
            _S3Server.ConsoleDebug.Exceptions = true; 

            _S3Server.PostRequestHandler = PostRequestHandler;

            _S3Server.Service.ListBuckets = _ApiHandler.ServiceListBuckets;

            _S3Server.Bucket.Delete = _ApiHandler.BucketDelete; 
            _S3Server.Bucket.DeleteTags = _ApiHandler.BucketDeleteTags;
            _S3Server.Bucket.Exists = _ApiHandler.BucketExists;
            _S3Server.Bucket.Read = _ApiHandler.BucketRead;
            _S3Server.Bucket.ReadAcl = _ApiHandler.BucketReadAcl;
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

            #endregion

            #region Wait-for-Server-Thread

            if (_Settings.EnableConsole)
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

            _Logging.Log(LoggingModule.Severity.Info, "Less3 exiting");

            #endregion
        }

        static void Welcome()
        {
            // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            string msg = 
                Logo() + 
                Environment.NewLine +
                "  <3 :: Less3 :: S3-Compatible Object Storage :: v" + version + Environment.NewLine +
                Environment.NewLine;

            Console.WriteLine(msg); 
        }

        static string Logo()
        {
            // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 

            string ret =
                Environment.NewLine +
                @"   _           ____  " + Environment.NewLine +
                @"  | |___ _____|__ /  " + Environment.NewLine +
                @"  | / -_|_-<_-<|_ \  " + Environment.NewLine +
                @"  |_\___/__/__/___/  " + Environment.NewLine +
                @"                     " + Environment.NewLine;

            return ret;
        }

        static string DefaultPage(string link)
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
                WebUtility.HtmlEncode(Logo()) +
                "      </pre>" + Environment.NewLine +
                "      <p>Congratulations, your Less3 node is running!</p>" + Environment.NewLine +
                "      <p>" + Environment.NewLine +
                "        <a href='" + link + "' target='_blank'>Source Code</a>" + Environment.NewLine +
                "      </p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        static string HostnameRequired()
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
                WebUtility.HtmlEncode(Logo()) +
                "      </pre>" + Environment.NewLine +
                "      <h3>Bad Request</h3>" + Environment.NewLine +
                "      <p>Less3 must be accessed using a hostname, not an IP address.</p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        static bool ExitApplication()
        {
            _Logging.Log(LoggingModule.Severity.Info, "Less3 exiting due to console request");
            _Exiting = true; 
            return true;
        }

        static S3Response PreRequestHandler(S3Request req)
        {
            S3Response resp = null;

            while (req.RawUrl.Contains("\\\\")) req.RawUrl.Replace("\\\\", "\\");

            #region Favicon-and-Robots

            if (req.RawUrlEntries.Count == 1)
            {
                if (req.RawUrlEntries[0].Equals("favicon.ico"))
                {
                    byte[] favicon = Common.ReadBinaryFile("assets/favicon.ico");
                    resp = new S3Response(req, 200, "image/x-icon", null, favicon);
                    return resp;
                }
                else if (req.RawUrlEntries[0].Equals("robots.txt"))
                {
                    resp = new S3Response(req, 200, "text/plain", null, Encoding.UTF8.GetBytes("User-Agent: *\r\nDisallow:\r\n"));
                    return resp;
                }
            }

            #endregion

            #region Check-for-IP-Address

            if (req.Headers.ContainsKey("Host"))
            {
                string val = req.Headers["Host"]; 
                if (val.Contains(":")) val = val.Substring(0, val.LastIndexOf(":"));   
                IPAddress ipAddress;
                if (IPAddress.TryParse(val, out ipAddress))
                {
                    resp = new S3Response(req, 400, "text/html", null, Encoding.UTF8.GetBytes(HostnameRequired()));
                    return resp;
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
                        resp = new S3Response(req, 200, "text/html", null, Encoding.UTF8.GetBytes(DefaultPage("https://github.com/jchristn/less3")));
                        return resp;
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
                        _Logging.Warn("PreRequestHandler invalid admin API key supplied: " + _Settings.Server.AdminApiKey +
                            " from " + req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl);
                        resp = new S3Response(req, 401, "text/plain", null, null);
                        return resp;
                    }

                    resp = new S3Response(req, 400, "text/plain", null, null);

                    switch (req.Method)
                    {
                        case HttpMethod.GET:
                        case HttpMethod.PUT:
                        case HttpMethod.POST:
                        case HttpMethod.DELETE:
                            resp = _AdminApiHandler.Process(req);
                            break;
                    } 
                }
            }

            #endregion

            return resp;
        }

        static bool PostRequestHandler(S3Request req, S3Response resp)
        {
            TimeSpan span = resp.TimestampUtc - req.TimestampUtc;
            int ms = (int)span.TotalMilliseconds;
            _Logging.Debug(req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + " " + resp.StatusCode + " [" + ms + "ms]");
             
            return true;
        }

        static S3Response DefaultRequestHandler(S3Request req)
        {
            S3ServerInterface.S3Objects.Error error = new S3ServerInterface.S3Objects.Error(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
            return new S3Response(req, error);
        }
    }
}
