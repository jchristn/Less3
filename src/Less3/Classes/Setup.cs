namespace Less3.Classes
{
    using System;
    using System.IO;
    using SyslogLogging;
    using GetSomeInput;
    using Less3.Database;
    using Less3.Settings;
    using S3ServerLibrary;

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
            SettingsBase settings = new SettingsBase();
             
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
                string userInput = Inputty.GetString("Database type [sqlite|sqlserver|mysql|postgresql]:", "sqlite", false);
                switch (userInput)
                {
                    case "sqlite":
                        settings.Database = new DatabaseSettings(
                            Inputty.GetString("Filename:", "./less3.db", false)
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
                            Inputty.GetString("Hostname:", "localhost", false),
                            Inputty.GetInteger("Port:", 1433, true, false),
                            Inputty.GetString("Username:", "sa", false),
                            Inputty.GetString("Password:", null, false),
                            Inputty.GetString("Instance (for SQLEXPRESS):", null, true),
                            Inputty.GetString("Database name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                    case "mysql": 
                        settings.Database = new DatabaseSettings(
                            DatabaseTypeEnum.Mysql,
                            Inputty.GetString("Hostname:", "localhost", false),
                            Inputty.GetInteger("Port:", 3306, true, false),
                            Inputty.GetString("Username:", "root", false),
                            Inputty.GetString("Password:", null, false),
                            Inputty.GetString("Schema name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                    case "postgresql":
                        settings.Database = new DatabaseSettings(
                            DatabaseTypeEnum.Postgresql,
                            Inputty.GetString("Hostname:", "localhost", false),
                            Inputty.GetInteger("Port:", 5432, true, false),
                            Inputty.GetString("Username:", "postgres", false),
                            Inputty.GetString("Password:", null, false),
                            Inputty.GetString("Schema name:", "less3", false)
                            );
                        dbSet = true;
                        break;
                }
            }

            if (!Common.WriteFile("system.json", SerializationHelper.SerializeJson(settings, true), false))
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

            DatabaseDriverBase database = DatabaseDriverFactory.Create(settings.Database, logging);

            ConfigManager config = new ConfigManager(settings, logging, database);

            DefaultDataSeeder.Seed(settings, logging, database, config);

            #endregion
            
            Common.WriteFile("./system.json", SerializationHelper.SerializeJson(settings, true), false);

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
            Console.WriteLine("  http://" + settings.Webserver.Hostname + ":" + settings.Webserver.Port + "/default/hello.html");
            Console.WriteLine("  http://" + settings.Webserver.Hostname + ":" + settings.Webserver.Port + "/default/hello.txt");
            Console.WriteLine("  http://" + settings.Webserver.Hostname + ":" + settings.Webserver.Port + "/default/hello.json");
            Console.WriteLine("");
            Console.WriteLine("  Access key  : default");
            Console.WriteLine("  Secret key  : default");
            Console.WriteLine("  Bucket name : default (public read enabled!)");
            Console.WriteLine("  S3 endpoint : http://" + settings.Webserver.Hostname + ":" + settings.Webserver.Port);
            Console.WriteLine("");
            Console.WriteLine("IMPORTANT: be sure to supply a hostname in the system.json Webserver.Hostname");
            Console.WriteLine("property if you wish to allow access from other machines.  Your node is currently");
            Console.WriteLine("only accessible via localhost.  Do not use an IP address for this value.");
            Console.WriteLine("");

            #endregion
        }
         
        #endregion
    }
}
