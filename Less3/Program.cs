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

using Less3.Api;
using Less3.Classes;

namespace Less3
{
    class Program
    {
        static Settings _Settings;
        static LoggingModule _Logging;
        static UserManager _Users;
        static CredentialManager _Credentials;
        static BucketManager _Buckets;
        static ApiHandler _Api;
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

            Welcome(); 

            #endregion

            #region Initialize-Globals
             
            _Logging = new LoggingModule(
                _Settings.Syslog.ServerIp,
                _Settings.Syslog.ServerPort,
                _Settings.EnableConsole,
                (LoggingModule.Severity)_Settings.Syslog.MinimumLevel,
                false,
                true,
                true,
                false,
                false,
                false);

            _Credentials = new CredentialManager(_Settings, _Logging);

            _Buckets = new BucketManager(_Settings, _Logging);

            _Users = new UserManager(_Settings, _Logging);

            _Auth = new AuthManager(_Settings, _Logging, _Users, _Credentials, _Buckets);

            _Api = new ApiHandler(_Settings, _Logging, _Credentials, _Buckets, _Auth, _Users);

            _S3Server = new S3Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.ListenerPort,
                _Settings.Server.Ssl,
                PreRequestHandler,
                DefaultRequestHandler);

            _S3Server.ConsoleDebug.Exceptions = true; 

            _S3Server.PostRequestHandler = PostRequestHandler;

            _S3Server.Service.ListBuckets = _Api.ServiceListBuckets;

            _S3Server.Bucket.Delete = _Api.BucketDelete;
            _S3Server.Bucket.DeleteTags = _Api.BucketDeleteTags;
            _S3Server.Bucket.Exists = _Api.BucketExists;
            _S3Server.Bucket.Read = _Api.BucketRead; 
            _S3Server.Bucket.ReadTags = _Api.BucketReadTags;
            _S3Server.Bucket.ReadVersions = _Api.BucketReadVersions;
            _S3Server.Bucket.ReadVersioning = _Api.BucketReadVersioning;
            _S3Server.Bucket.Write = _Api.BucketWrite; 
            _S3Server.Bucket.WriteTags = _Api.BucketWriteTags;
            _S3Server.Bucket.WriteVersioning = _Api.BucketWriteVersioning;

            _S3Server.Object.Delete = _Api.ObjectDelete;
            _S3Server.Object.DeleteMultiple = _Api.ObjectDeleteMultiple;
            _S3Server.Object.DeleteTags = _Api.ObjectDeleteTags;
            _S3Server.Object.Exists = _Api.ObjectExists;
            _S3Server.Object.Read = _Api.ObjectRead; 
            _S3Server.Object.ReadRange = _Api.ObjectReadRange; 
            _S3Server.Object.ReadTags = _Api.ObjectReadTags;
            _S3Server.Object.Write = _Api.ObjectWrite; 
            _S3Server.Object.WriteTags = _Api.ObjectWriteTags;

            #endregion

            #region Start-Console

            if (_Settings.EnableConsole)
            {
                _Console = new ConsoleManager(ExitApplication);
            }

            #endregion

            #region Wait-for-Server-Thread

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);
            bool waitHandleSignal = false;
            do
            { 
                if (_Exiting) break; 
                waitHandleSignal = waitHandle.WaitOne(1000);
            }
            while (!waitHandleSignal);

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
                "  " + _Settings.ProductName + " v" + version + Environment.NewLine +
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

        static bool ExitApplication()
        {
            _Logging.Log(LoggingModule.Severity.Info, "Less3 exiting due to console request");
            _Exiting = true; 
            return true;
        }

        static S3Response PreRequestHandler(S3Request req)
        {
            /*
            Console.WriteLine(req.ToString());
            if (req.Data != null)
            {
                if (req.Data.Length < 1024) Console.WriteLine(Encoding.UTF8.GetString(req.Data));
                else Console.WriteLine("Data greater than 1KB, not displayed");
            }
            */

            return null;
        }

        static bool PostRequestHandler(S3Request req, S3Response resp)
        {
            TimeSpan span = resp.TimestampUtc - req.TimestampUtc;
            int ms = (int)span.TotalMilliseconds;
            _Logging.Log(LoggingModule.Severity.Debug, req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + " " + resp.StatusCode + " [" + ms + "ms]");
            
            /*
            Console.WriteLine(resp.ToString());
            if (resp.Data != null)
            {
                if (resp.Data.Length < 1024) Console.WriteLine(Encoding.UTF8.GetString(resp.Data));
                else Console.WriteLine("Data greater than 1KB, not displayed");
            }
            */

            return true;
        }

        static S3Response DefaultRequestHandler(S3Request req)
        {
            return new S3Response(req, 400, "text/plain", null, Encoding.UTF8.GetBytes("Unknown endpoint"));
        }
    }
}
