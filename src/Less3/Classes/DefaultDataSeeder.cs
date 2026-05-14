namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using Less3.Database;
    using Less3.Settings;
    using Less3.Storage;
    using S3ServerLibrary;
    using SyslogLogging;

    /// <summary>
    /// Seeds the default user, credential, bucket, and sample content used by setup and Docker bootstrap.
    /// </summary>
    internal static class DefaultDataSeeder
    {
        private const string DefaultUserGuid = "default";
        private const string DefaultAccessKey = "default";
        private const string DefaultSecretKey = "default";
        private const string DefaultBucketName = "default";
        private const string SourceLink = "http://github.com/jchristn/less3";

        internal static void Seed(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database, ConfigManager config)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Directory.CreateDirectory(settings.Storage.DiskDirectory);
            Directory.CreateDirectory(settings.Storage.TempDirectory);

            config.AddUser(new User(DefaultUserGuid, "Default user", "default@default.com"));
            config.AddCredential(DefaultUserGuid, "My first access key", DefaultAccessKey, DefaultSecretKey, false);

            Bucket bucketConfig = new Bucket(
                DefaultBucketName,
                DefaultUserGuid,
                DefaultUserGuid,
                StorageDriverType.Disk,
                settings.Storage.DiskDirectory + DefaultBucketName + "/Objects/");
            bucketConfig.EnablePublicRead = true;
            bucketConfig.EnablePublicWrite = false;
            bucketConfig.EnableVersioning = false;

            config.AddBucket(bucketConfig);

            BucketClient bucket = new BucketClient(settings, logging, bucketConfig, database);

            DateTime ts = DateTime.Now.ToUniversalTime();

            string htmlFile = SampleHtmlFile(SourceLink);
            string textFile = SampleTextFile(SourceLink);
            string jsonFile = SampleJsonFile(SourceLink);

            Obj obj1 = new Obj
            {
                OwnerGUID = DefaultUserGuid,
                AuthorGUID = DefaultUserGuid,
                BlobFilename = Guid.NewGuid().ToString(),
                ContentLength = htmlFile.Length,
                ContentType = "text/html",
                Key = "hello.html",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(htmlFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            Obj obj2 = new Obj
            {
                OwnerGUID = DefaultUserGuid,
                AuthorGUID = DefaultUserGuid,
                BlobFilename = Guid.NewGuid().ToString(),
                ContentLength = textFile.Length,
                ContentType = "text/plain",
                Key = "hello.txt",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(textFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            Obj obj3 = new Obj
            {
                OwnerGUID = DefaultUserGuid,
                AuthorGUID = DefaultUserGuid,
                BlobFilename = Guid.NewGuid().ToString(),
                ContentLength = jsonFile.Length,
                ContentType = "application/json",
                Key = "hello.json",
                Md5 = Common.BytesToHexString(Common.Md5(Encoding.UTF8.GetBytes(jsonFile))),
                Version = 1,
                IsFolder = false,
                DeleteMarker = false,
                CreatedUtc = ts,
                LastUpdateUtc = ts,
                LastAccessUtc = ts
            };

            bucket.AddObject(obj1, Encoding.UTF8.GetBytes(htmlFile));
            bucket.AddObject(obj2, Encoding.UTF8.GetBytes(textFile));
            bucket.AddObject(obj3, Encoding.UTF8.GetBytes(jsonFile));
        }

        private static string SampleHtmlFile(string link)
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
                WebUtility.HtmlEncode(Constants.Logo) +
                "      </pre>" + Environment.NewLine +
                "      <p>Congratulations, your Less3 node is running!</p>" + Environment.NewLine +
                "      <p>" + Environment.NewLine +
                "        <a href='" + link + "' target='_blank'>Source Code</a>" + Environment.NewLine +
                "      </p>" + Environment.NewLine +
                "   </body>" + Environment.NewLine +
                "</html>";

            return html;
        }

        private static string SampleJsonFile(string link)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            ret.Add("Title", "Welcome to Less3");
            ret.Add("Body", "If you can see this file, your Less3 node is running!");
            ret.Add("Github", link);
            return SerializationHelper.SerializeJson(ret, true);
        }

        private static string SampleTextFile(string link)
        {
            string text =
                "Welcome to Less3!" + Environment.NewLine + Environment.NewLine +
                "If you can see this file, your Less3 node is running!  Now try " +
                "accessing this same URL in your browser, but use the .html extension!" + Environment.NewLine + Environment.NewLine +
                "Find us on Github here: " + link + Environment.NewLine + Environment.NewLine;

            return text;
        }
    }
}
