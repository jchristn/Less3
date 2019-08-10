using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Setup workflow.
    /// </summary>
    public class Setup
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Setup()
        {
            RunSetup();
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private void RunSetup()
        {
            #region Variables

            DateTime timestamp = DateTime.Now;
            Settings currSettings = new Settings();
             
            #endregion

            #region Welcome

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(Environment.NewLine +
                @"   _           ____  " + Environment.NewLine +
                @"  | |___ _____|__ /  " + Environment.NewLine +
                @"  | / -_|_-<_-<|_ \  " + Environment.NewLine +
                @"  |_\___/__/__/___/  " + Environment.NewLine +
                @"                     " + Environment.NewLine +
                Environment.NewLine);

            Console.ResetColor();

            Console.WriteLine("");
            Console.WriteLine("<3 :: Less3 :: S3-Compatible Object Storage");
            Console.WriteLine("");
            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("Thank you for using Less3!  We're putting together a basic system configuration");
            Console.WriteLine("so you can be up and running quickly.  You'll want to modify the System.json");
            Console.WriteLine("file after to ensure a more secure operating environment.");
            Console.WriteLine("");

            #endregion

            #region Temporary-Instances

            LoggingModule logging = new LoggingModule("127.0.0.1", 514, false, LoggingModule.Severity.Alert, false, false, false, false, false, false);

            #endregion

            #region Initial-Settings

            currSettings.EnableConsole = true;

            currSettings.Files = new Settings.SettingsFiles();
            currSettings.Files.ConfigDatabase = "./Less3.db";

            currSettings.Server = new Settings.SettingsServer();
            currSettings.Server.DnsHostname = "localhost";
            currSettings.Server.ListenerPort = 8000;
            currSettings.Server.Ssl = false;

            currSettings.Server.HeaderApiKey = "x-api-key"; 
            currSettings.Server.AdminApiKey = "less3admin";   
             
            currSettings.Storage = new Settings.SettingsStorage(); 
            currSettings.Storage.Directory = "./Storage/";
             
            currSettings.Syslog = new Settings.SettingsSyslog();
            currSettings.Syslog.ConsoleLogging = true;
            currSettings.Syslog.Header = "less3";
            currSettings.Syslog.ServerIp = "127.0.0.1";
            currSettings.Syslog.ServerPort = 514;
            currSettings.Syslog.LogHttpRequests = false;
            currSettings.Syslog.LogHttpResponses = false;
            currSettings.Syslog.MinimumLevel = 1;

            currSettings.Debug = new Settings.SettingsDebug();
            currSettings.Debug.Database = false;
            currSettings.Debug.Authentication = false;

            if (!Common.WriteFile("System.json", Common.SerializeJson(currSettings, true), false))
            {
                Common.ExitApplication("setup", "Unable to write System.json", -1);
                return;
            }
             
            if (!Directory.Exists(currSettings.Storage.Directory))
                Directory.CreateDirectory(currSettings.Storage.Directory);

            #endregion

            #region Create-Configuration-Database

            ConfigManager config = new ConfigManager(currSettings, logging);

            string userGuid = Guid.NewGuid().ToString();
            config.AddUser(new User(userGuid, "default", "default@default.com"));
            config.AddCredential(userGuid, "My first access key", "default", "default");

            BucketConfiguration bucketConfig = new BucketConfiguration(
                "default",
                userGuid,
                currSettings.Storage.Directory + "default/default.db",
                currSettings.Storage.Directory + "default/Objects/");
            bucketConfig.EnablePublicRead = true;
            bucketConfig.EnablePublicWrite = false;
            bucketConfig.EnableVersioning = false;

            config.AddBucket(bucketConfig);

            #endregion

            #region Write-Sample-Objects

            BucketClient bucket = new BucketClient(currSettings, logging, bucketConfig);

            DateTime ts = DateTime.Now.ToUniversalTime();

            string htmlFile = SampleHtmlFile("http://github.com/jchristn/less3");
            string textFile = SampleTextFile("http://github.com/jchristn/less3");
            string jsonFile = SampleJsonFile("http://github.com/jchristn/less3");

            Obj obj1 = new Obj();
            obj1.Owner = "default";
            obj1.Author = "default";
            obj1.BlobFilename = Guid.NewGuid().ToString();
            obj1.ContentLength = htmlFile.Length;
            obj1.ContentType = "text/html";
            obj1.Key = "hello.html";
            obj1.Md5 = Common.Md5(Encoding.UTF8.GetBytes(htmlFile));
            obj1.Version = 1;
            obj1.CreatedUtc = ts;
            obj1.LastUpdateUtc = ts;
            obj1.LastAccessUtc = ts;

            Obj obj2 = new Obj();
            obj2.Owner = "default";
            obj2.Author = "default";
            obj2.BlobFilename = Guid.NewGuid().ToString();
            obj2.ContentLength = htmlFile.Length;
            obj2.ContentType = "text/plain";
            obj2.Key = "hello.txt";
            obj2.Md5 = Common.Md5(Encoding.UTF8.GetBytes(textFile));
            obj2.Version = 1;
            obj2.CreatedUtc = ts;
            obj2.LastUpdateUtc = ts;
            obj2.LastAccessUtc = ts;

            Obj obj3 = new Obj();
            obj3.Owner = "default";
            obj3.Author = "default";
            obj3.BlobFilename = Guid.NewGuid().ToString();
            obj3.ContentLength = htmlFile.Length;
            obj3.ContentType = "application/json";
            obj3.Key = "hello.json";
            obj3.Md5 = Common.Md5(Encoding.UTF8.GetBytes(jsonFile));
            obj3.Version = 1;
            obj3.CreatedUtc = ts;
            obj3.LastUpdateUtc = ts;
            obj3.LastAccessUtc = ts;

            bucket.AddObject(obj1, Encoding.UTF8.GetBytes(htmlFile));
            bucket.AddObject(obj2, Encoding.UTF8.GetBytes(textFile));
            bucket.AddObject(obj3, Encoding.UTF8.GetBytes(jsonFile));

            #endregion 

            #region Wrap-Up

            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890 
            Console.WriteLine("");
            Console.WriteLine("All finished!");
            Console.WriteLine("");
            Console.WriteLine("If you ever want to return to this setup wizard, just re-run the application");
            Console.WriteLine("from the terminal with the 'setup' argument.");
            Console.WriteLine("");
            Console.WriteLine("We created a bucket containing a few sample files for you so that you can see");
            Console.WriteLine("your node in action.  Access these files in the 'default' bucket using the");
            Console.WriteLine("AWS SDK or your favorite S3 browser tool.");
            Console.WriteLine(""); 
            Console.WriteLine("  http://" + currSettings.Server.DnsHostname + ":" + currSettings.Server.ListenerPort + "/default/hello.html");
            Console.WriteLine("  http://" + currSettings.Server.DnsHostname + ":" + currSettings.Server.ListenerPort + "/default/hello.txt");
            Console.WriteLine("  http://" + currSettings.Server.DnsHostname + ":" + currSettings.Server.ListenerPort + "/default/hello.json");
            Console.WriteLine("");
            Console.WriteLine("  Access key  : default");
            Console.WriteLine("  Secret key  : default");
            Console.WriteLine("  Bucket name : default (public read enabled!)");
            Console.WriteLine("  S3 endpoint : http://" + currSettings.Server.DnsHostname + ":" + currSettings.Server.ListenerPort);
            Console.WriteLine("");
            Console.WriteLine("IMPORTANT: be sure to supply a hostname in the System.json Server.DnsHostname");
            Console.WriteLine("field if you wish to allow access from other machines.  Your node is currently");
            Console.WriteLine("only accessible via localhost.  Do not use an IP address for this value.");
            Console.WriteLine("");

            #endregion
        }

        private string SampleHtmlFile(string link)
        {
            string html =
                "<html>" + Environment.NewLine +
                "   <head>" + Environment.NewLine +
                "      <title>&lt;3 :: Less3 :: S3-Compatible Object Storage</title>" + Environment.NewLine +
                "      <style>" + Environment.NewLine +
                "          body {" + Environment.NewLine +
                "            font-family: arial;" + Environment.NewLine +
                "          }" + Environment.NewLine +
                "          h3 {" + Environment.NewLine +
                "            background-color: #e5e7ea;" + Environment.NewLine +
                "            color: #333333; " + Environment.NewLine +
                "            padding: 16px;" + Environment.NewLine +
                "            border: 16px;" + Environment.NewLine +
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
                "      <h3>&lt;3 :: Less3</h3>" + Environment.NewLine +
                "      <p>Congratulations, your Less3 node is running!</p>" + Environment.NewLine +
                "      <p>" + Environment.NewLine +
                "        <a href='" + link + "' target='_blank'>SDKs and Source Code</a>" + Environment.NewLine +
                "      </p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        private string SampleJsonFile(string link)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            ret.Add("Title", "Welcome to Less3");
            ret.Add("Body", "If you can see this file, your Less3 node is running!");
            ret.Add("Github", link);
            return Common.SerializeJson(ret, true);
        }

        private string SampleTextFile(string link)
        {
            string text =
                "Welcome to Less3!" + Environment.NewLine + Environment.NewLine +
                "If you can see this file, your Less3 node is running!  Now try " +
                "accessing this same URL in your browser, but use the .html extension!" + Environment.NewLine + Environment.NewLine +
                "Find us on Github here: " + link + Environment.NewLine + Environment.NewLine;

            return text;
        }

        #endregion
    }
}