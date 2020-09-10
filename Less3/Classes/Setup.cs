using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using SyslogLogging;
using Watson.ORM.Core;
using Less3.Storage;

namespace Less3.Classes
{
    /// <summary>
    /// Setup workflow.
    /// </summary>
    internal class Setup
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories
         
        internal Setup()
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
            Settings settings = new Settings();
             
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
            Console.WriteLine("so you can be up and running quickly.  You'll want to modify the system.json");
            Console.WriteLine("file after to ensure a more secure operating environment.");
            Console.WriteLine("");

            #endregion
             
            #region Temporary-Instances

            LoggingModule logging = new LoggingModule("127.0.0.1", 514);

            #endregion

            #region Initial-Settings

            settings.EnableConsole = true;

            settings.Server = new Settings.SettingsServer();
            settings.Server.DnsHostname = "localhost";
            settings.Server.ListenerPort = 8000;
            settings.Server.Ssl = false; 
            settings.Server.HeaderApiKey = "x-api-key"; 
            settings.Server.AdminApiKey = "less3admin";
            settings.Server.RegionString = "us-west-1";
            settings.Server.AuthenticateSignatures = true;
             
            settings.Storage = new Settings.SettingsStorage();
            settings.Storage.TempDirectory = "./Temp/";
            settings.Storage.StorageType = StorageDriverType.Disk;
            settings.Storage.DiskDirectory = "./Storage/";
             
            settings.Logging = new Settings.SettingsLogging();
            settings.Logging.ConsoleLogging = true;
            settings.Logging.Header = "less3";
            settings.Logging.SyslogServerIp = "127.0.0.1";
            settings.Logging.SyslogServerPort = 514;
            settings.Logging.LogHttpRequests = false; 
            settings.Logging.MinimumLevel = Severity.Info;

            settings.Debug = new Settings.SettingsDebug();
            settings.Debug.DatabaseQueries = false;
            settings.Debug.DatabaseResults = false;
            settings.Debug.Authentication = false;
            settings.Debug.S3Requests = false;

            #endregion

            #region Database-and-ORM

            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("");
            Console.WriteLine("Less3 requires access to a database and supports Sqlite, Microsoft SQL Server,");
            Console.WriteLine("MySQL, and PostgreSQL.  Please provide access details for your database.  The");
            Console.WriteLine("user account supplied must have the ability to CREATE and DROP tables along");
            Console.WriteLine("with issue queries containing SELECT, INSERT, UPDATE, and DELETE.  Setup will");
            Console.WriteLine("attempt to create tables on your behalf if they dont exist.");
            Console.WriteLine("");

            bool dbSet = false; 

            while (!dbSet)
            {
                string userInput = Common.InputString("Database type [sqlite|sqlserver|mysql|postgresql]:", "sqlite", false);
                switch (userInput)
                {
                    case "sqlite":
                        settings.Database = new DatabaseSettings(
                            Common.InputString("Filename:", "./less3.db", false)
                            );

                        //                          1         2         3         4         5         6         7
                        //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                        Console.WriteLine("");
                        Console.WriteLine("IMPORTANT: Using Sqlite in production is not recommended if deploying within a");
                        Console.WriteLine("containerized environment and the database file is stored within the container.");
                        Console.WriteLine("Store the database file in external storage to ensure persistence.");
                        Console.WriteLine("");
                        dbSet = true;
                        break;

                    case "sqlserver":
                        settings.Database = new DatabaseSettings(
                            Common.InputString("Hostname:", "localhost", false),
                            Common.InputInteger("Port:", 1433, true, false),
                            Common.InputString("Username:", "sa", false),
                            Common.InputString("Password:", null, false),
                            Common.InputString("Instance (for SQLEXPRESS):", null, true),
                            Common.InputString("Database name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                    case "mysql": 
                        settings.Database = new DatabaseSettings(
                            DbTypes.Mysql,
                            Common.InputString("Hostname:", "localhost", false),
                            Common.InputInteger("Port:", 3306, true, false),
                            Common.InputString("Username:", "root", false),
                            Common.InputString("Password:", null, false),
                            Common.InputString("Schema name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                    case "postgresql":
                        settings.Database = new DatabaseSettings(
                            DbTypes.Postgresql,
                            Common.InputString("Hostname:", "localhost", false),
                            Common.InputInteger("Port:", 5432, true, false),
                            Common.InputString("Username:", "postgres", false),
                            Common.InputString("Password:", null, false),
                            Common.InputString("Schema name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                }
            }

            if (!Common.WriteFile("system.json", Common.SerializeJson(settings, true), false))
            {
                Common.ExitApplication("setup", "Unable to write system.json", -1);
                return;
            }

            if (!Directory.Exists(settings.Storage.DiskDirectory))
                Directory.CreateDirectory(settings.Storage.DiskDirectory);

            if (!Directory.Exists(settings.Storage.TempDirectory))
                Directory.CreateDirectory(settings.Storage.TempDirectory);

            #endregion

            #region Create-Configuration-Database

            Watson.ORM.WatsonORM orm = new Watson.ORM.WatsonORM(settings.Database);

            orm.InitializeDatabase();
            
            orm.InitializeTable(typeof(Bucket));
            orm.InitializeTable(typeof(BucketAcl));
            orm.InitializeTable(typeof(BucketTag));
            orm.InitializeTable(typeof(Credential));
            orm.InitializeTable(typeof(Obj));
            orm.InitializeTable(typeof(ObjectAcl));
            orm.InitializeTable(typeof(ObjectTag));
            orm.InitializeTable(typeof(User));
             
            ConfigManager config = new ConfigManager(settings, logging, orm);

            string userGuid = "default";
            config.AddUser(new User(userGuid, "default", "default@default.com"));
            config.AddCredential(userGuid, "My first access key", "default", "default", false);

            Bucket bucketConfig = new Bucket(
                "default",
                userGuid,
                userGuid,
                StorageDriverType.Disk,
                settings.Storage.DiskDirectory + "default/Objects/");
            bucketConfig.EnablePublicRead = true;
            bucketConfig.EnablePublicWrite = false;
            bucketConfig.EnableVersioning = false;

            config.AddBucket(bucketConfig);

            #endregion

            #region Write-Sample-Objects

            BucketClient bucket = new BucketClient(settings, logging, bucketConfig, orm);

            DateTime ts = DateTime.Now.ToUniversalTime();

            string htmlFile = SampleHtmlFile("http://github.com/jchristn/less3");
            string textFile = SampleTextFile("http://github.com/jchristn/less3");
            string jsonFile = SampleJsonFile("http://github.com/jchristn/less3");

            Obj obj1 = new Obj();
            obj1.OwnerGUID = "default";
            obj1.AuthorGUID = "default";
            obj1.BlobFilename = Guid.NewGuid().ToString();
            obj1.ContentLength = htmlFile.Length;
            obj1.ContentType = "text/html";
            obj1.Key = "hello.html";
            obj1.Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(htmlFile)));
            obj1.Version = 1;
            obj1.IsFolder = false;
            obj1.DeleteMarker = false;
            obj1.CreatedUtc = ts;
            obj1.LastUpdateUtc = ts;
            obj1.LastAccessUtc = ts;

            Obj obj2 = new Obj();
            obj2.OwnerGUID = "default";
            obj2.AuthorGUID = "default";
            obj2.BlobFilename = Guid.NewGuid().ToString();
            obj2.ContentLength = htmlFile.Length;
            obj2.ContentType = "text/plain";
            obj2.Key = "hello.txt";
            obj2.Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(textFile)));
            obj2.Version = 1;
            obj2.IsFolder = false;
            obj2.DeleteMarker = false;
            obj2.CreatedUtc = ts;
            obj2.LastUpdateUtc = ts;
            obj2.LastAccessUtc = ts;

            Obj obj3 = new Obj();
            obj3.OwnerGUID = "default";
            obj3.AuthorGUID = "default";
            obj3.BlobFilename = Guid.NewGuid().ToString();
            obj3.ContentLength = htmlFile.Length;
            obj3.ContentType = "application/json";
            obj3.Key = "hello.json";
            obj3.Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(jsonFile)));
            obj3.Version = 1;
            obj3.IsFolder = false;
            obj3.DeleteMarker = false;
            obj3.CreatedUtc = ts;
            obj3.LastUpdateUtc = ts;
            obj3.LastAccessUtc = ts;
             
            bucket.AddObject(obj1, Encoding.UTF8.GetBytes(htmlFile));
            bucket.AddObject(obj2, Encoding.UTF8.GetBytes(textFile));
            bucket.AddObject(obj3, Encoding.UTF8.GetBytes(jsonFile)); 

            Common.WriteFile("./system.json", Encoding.UTF8.GetBytes(Common.SerializeJson(settings, true)));

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
            Console.WriteLine("  http://" + settings.Server.DnsHostname + ":" + settings.Server.ListenerPort + "/default/hello.html");
            Console.WriteLine("  http://" + settings.Server.DnsHostname + ":" + settings.Server.ListenerPort + "/default/hello.txt");
            Console.WriteLine("  http://" + settings.Server.DnsHostname + ":" + settings.Server.ListenerPort + "/default/hello.json");
            Console.WriteLine("");
            Console.WriteLine("  Access key  : default");
            Console.WriteLine("  Secret key  : default");
            Console.WriteLine("  Bucket name : default (public read enabled!)");
            Console.WriteLine("  S3 endpoint : http://" + settings.Server.DnsHostname + ":" + settings.Server.ListenerPort);
            Console.WriteLine("");
            Console.WriteLine("IMPORTANT: be sure to supply a hostname in the system.json Server.DnsHostname");
            Console.WriteLine("field if you wish to allow access from other machines.  Your node is currently");
            Console.WriteLine("only accessible via localhost.  Do not use an IP address for this value.");
            Console.WriteLine("");

            #endregion
        }
         
        static string LogoPlain()
        {
            // http://loveascii.com/hearts.html
            // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 

            string ret = Environment.NewLine;
            ret +=
                "  ,d88b.d88b,   " + @"   _           ____  " + Environment.NewLine +
                "  88888888888   " + @"  | |___ _____|__ /  " + Environment.NewLine +
                "  `Y8888888Y'   " + @"  | / -_|_-<_-<|_ \  " + Environment.NewLine +
                "    `Y888Y'     " + @"  |_\___/__/__/___/  " + Environment.NewLine +
                "      `Y'       " + Environment.NewLine;

            return ret;
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

        private void Logger(string msg)
        {
            Console.WriteLine(msg);
        }

        #endregion
    }
}