using System;
using System.Collections.Generic;
using System.Text;

using SqliteWrapper;

using Less3.Classes;

namespace Less3.Database.Configuration
{
    /// <summary>
    /// Configuration database queries.
    /// </summary>
    internal static class DatabaseQueries
    {
        #region Table-Creation
         
        internal static string CreateUsersTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS Users " +
                "(" +
                "  Id                INTEGER PRIMARY KEY, " +
                "  GUID              VARCHAR(64), " +
                "  Name              VARCHAR(256), " +
                "  Email             VARCHAR(256) " +
                ")";
            return query;
        }

        internal static string CreateCredentialsTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS Credentials " +
                "(" +
                "  Id                INTEGER PRIMARY KEY, " +
                "  GUID              VARCHAR(64), " +
                "  UserGUID          VARCHAR(64), " +
                "  AccessKey         VARCHAR(256), " +
                "  SecretKey         VARCHAR(256) " +
                ")";
            return query;
        }

        internal static string CreateBucketsTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS Buckets " +
                "(" +
                "  Id                  INTEGER PRIMARY KEY, " +
                "  GUID                VARCHAR(64), " +
                "  OwnerGUID           VARCHAR(64), " +
                "  Name                VARCHAR(128), " +
                "  DatabaseFilename    VARCHAR(256), " + 
                "  ObjectsDirectory    VARCHAR(256), " +
                "  EnableVersioning    VARCHAR(8), " +
                "  EnablePublicWrite   VARCHAR(8), " +
                "  EnablePublicRead    VARCHAR(8), " +
                "  CreatedUtc          VARCHAR(32) " +
                ")";
            return query;
        }

        #endregion

        #region Users-Queries

        internal static string GetUsers()
        {
            string query = "SELECT * FROM Users";
            return query;
        }

        internal static string GetUserByGuid(string guid)
        {
            string query =
                "SELECT * FROM Users WHERE " +
                "  GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        internal static string GetUserByName(string name)
        {
            string query =
                "SELECT * FROM Users WHERE " +
                "  Name = '" + Sanitize(name) + "'";
            return query;
        }

        internal static string GetUserByEmail(string email)
        {
            string query =
                "SELECT * FROM Users WHERE " +
                "  Email = '" + Sanitize(email) + "'";
            return query;
        }

        internal static string InsertUser(User user)
        {
            string query =
                "INSERT INTO Users " +
                "(" +
                "  GUID, " +
                "  Name, " +
                "  Email " +
                ") " +
                "VALUES " +
                "(" +
                "  '" + Sanitize(user.GUID) + "', " +
                "  '" + Sanitize(user.Name) + "', " +
                "  '" + Sanitize(user.Email) + "' " +
                ")";
            return query;
        }

        internal static string DeleteUser(string guid)
        {
            string query = "DELETE FROM Users WHERE GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        #endregion

        #region Credentials-Queries

        internal static string GetCredentials()
        {
            string query = "SELECT * FROM Credentials";
            return query;
        }

        internal static string GetCredentialsByUser(string userGuid)
        {
            string query = "SELECT * FROM Credentials WHERE UserGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }

        internal static string GetCredentialsByGuid(string guid)
        {
            string query = "SELECT * FROM Credentials WHERE GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        internal static string GetCredentialsByAccessKey(string accessKey)
        {
            string query = "SELECT * FROM Credentials WHERE AccessKey = '" + Sanitize(accessKey) + "'";
            return query;
        }

        internal static string InsertCredentials(Credential cred)
        {
            string query =
                "INSERT INTO Credentials " +
                "(" +
                "  GUID, " +
                "  UserGUID, " +
                "  AccessKey, " +
                "  SecretKey " +
                ") " +
                "VALUES " +
                "(" +
                "  '" + Sanitize(cred.GUID) + "', " +
                "  '" + Sanitize(cred.UserGUID) + "', " +
                "  '" + Sanitize(cred.AccessKey) + "', " +
                "  '" + Sanitize(cred.SecretKey) + "' " +
                ")";
            return query;
        }

        internal static string DeleteCredentials(string guid)
        {
            string query = "DELETE FROM Credentials WHERE GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        internal static string DeleteCredentialsByUserGuid(string userGuid)
        {
            string query = "DELETE FROM Credentials WHERE UserGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }

        #endregion

        #region Buckets-Queries

        internal static string GetBuckets()
        {
            string query = "SELECT * FROM Buckets";
            return query;
        }

        internal static string GetBucketsByUser(string userGuid)
        {
            string query = "SELECT * FROM Buckets WHERE OwnerGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }

        internal static string GetBucketByName(string name)
        {
            string query = "SELECT * FROM Buckets WHERE Name = '" + Sanitize(name) + "'";
            return query;
        }

        internal static string GetBucketByGuid(string guid)
        {
            string query = "SELECT * FROM Buckets WHERE GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        internal static string InsertBucket(BucketConfiguration bucket)
        {
            string query =
                "INSERT INTO Buckets " +
                "(" +
                "  GUID, " +
                "  OwnerGUID, " +
                "  Name, " +
                "  DatabaseFilename, " +
                "  ObjectsDirectory, " +
                "  EnableVersioning, " +
                "  EnablePublicWrite, " +
                "  EnablePublicRead, " +
                "  CreatedUtc " +
                ") " +
                "VALUES " +
                "(" +
                "  '" + Sanitize(bucket.GUID) + "', " +
                "  '" + Sanitize(bucket.OwnerGUID) + "', " +
                "  '" + Sanitize(bucket.Name) + "', " +
                "  '" + Sanitize(bucket.DatabaseFilename) + "', " +
                "  '" + Sanitize(bucket.ObjectsDirectory) + "', " +
                "  '" + bucket.EnableVersioning.ToString() + "', " +
                "  '" + bucket.EnablePublicWrite.ToString() + "', " +
                "  '" + bucket.EnablePublicRead.ToString() + "', " +
                "  '" + TimestampUtc(bucket.CreatedUtc) + "' " +
                ")";
            return query;
        }

        internal static string DeleteBucket(string guid)
        {
            string query = "DELETE FROM Buckets WHERE GUID = '" + Sanitize(guid) + "'";
            return query;
        }

        internal static string DeleteBucketsByUserGuid(string userGuid)
        {
            string query = "DELETE FROM Buckets WHERE OwnerGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }

        #endregion

        #region General-Queries
         
        internal static string UpdateRecord(string table, string key, string val, Dictionary<string, object> vals)
        {
            int added = 0;
            string query =
                "UPDATE " + Sanitize(table) + " SET ";

            foreach (KeyValuePair<string, object> curr in vals)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;

                if (added == 0)
                {
                    query += Sanitize(curr.Key) + " = ";
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                else
                {
                    query += "," + Sanitize(curr.Key) + " = ";
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                added++;
            }

            query +=
                "WHERE " + Sanitize(key) + " = '" + Sanitize(val) + "'";

            return query;
        }
         
        #endregion

        #region Internal

        internal static string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        internal static string Sanitize(string str)
        {
            return DatabaseClient.SanitizeString(str);
        }

        internal static string TimestampUtc()
        {
            return DateTime.Now.ToUniversalTime().ToString(TimestampFormat);
        }

        internal static string TimestampUtc(DateTime? ts)
        {
            if (ts == null) return null;
            return Convert.ToDateTime(ts).ToUniversalTime().ToString(TimestampFormat);
        }

        internal static string TimestampUtc(DateTime ts)
        {
            return ts.ToUniversalTime().ToString(TimestampFormat);
        }

        #endregion
    }
}
