using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
 
namespace Less3.Classes
{
    /// <summary>
    /// Bucket configuration.
    /// </summary>
    internal class BucketConfiguration
    {
        #region Internal-Members

        internal int Id { get; set; }
        internal string GUID { get; set; }
        internal string OwnerGUID { get; set; }
        internal string Name { get; set; } 
        internal string DatabaseFilename { get; set; }
        internal string ObjectsDirectory { get; set; }
        internal bool EnableVersioning { get; set; } 
        internal bool EnablePublicWrite { get; set; }
        internal bool EnablePublicRead { get; set; }
        internal DateTime CreatedUtc { get; set; }

        #endregion

        #region Private-Members
         
        #endregion

        #region Constructors-and-Factories

        internal BucketConfiguration()
        {

        }

        internal BucketConfiguration(
            string name,
            string owner,
            string databaseFilename,
            string objectsDirectory)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(databaseFilename)) throw new ArgumentNullException(nameof(databaseFilename));
            if (String.IsNullOrEmpty(objectsDirectory)) throw new ArgumentNullException(nameof(objectsDirectory));

            GUID = Guid.NewGuid().ToString();
            Name = name;
            DatabaseFilename = databaseFilename;
            ObjectsDirectory = objectsDirectory;
            OwnerGUID = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }

        internal BucketConfiguration(
            string guid,
            string name,
            string owner,
            string databaseFilename,
            string objectsDirectory)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(databaseFilename)) throw new ArgumentNullException(nameof(databaseFilename));
            if (String.IsNullOrEmpty(objectsDirectory)) throw new ArgumentNullException(nameof(objectsDirectory));

            GUID = guid;
            Name = name;
            DatabaseFilename = databaseFilename;
            ObjectsDirectory = objectsDirectory;
            OwnerGUID = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }

        internal static BucketConfiguration FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            BucketConfiguration ret = new BucketConfiguration();
             
            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                ret.Id = Convert.ToInt32(row["Id"]);

            if (row.Table.Columns.Contains("GUID") && row["GUID"] != DBNull.Value && row["GUID"] != null)
                ret.GUID = row["GUID"].ToString();

            if (row.Table.Columns.Contains("OwnerGUID") && row["OwnerGUID"] != DBNull.Value && row["OwnerGUID"] != null)
                ret.OwnerGUID = row["OwnerGUID"].ToString();

            if (row.Table.Columns.Contains("Name") && row["Name"] != DBNull.Value && row["Name"] != null)
                ret.Name = row["Name"].ToString();

            if (row.Table.Columns.Contains("DatabaseFilename") && row["DatabaseFilename"] != DBNull.Value && row["DatabaseFilename"] != null)
                ret.DatabaseFilename = row["DatabaseFilename"].ToString();
             
            if (row.Table.Columns.Contains("ObjectsDirectory") && row["ObjectsDirectory"] != DBNull.Value && row["ObjectsDirectory"] != null)
                ret.ObjectsDirectory = row["ObjectsDirectory"].ToString();

            if (row.Table.Columns.Contains("EnableVersioning") && row["EnableVersioning"] != DBNull.Value && row["EnableVersioning"] != null)
                ret.EnableVersioning = Convert.ToBoolean(row["EnableVersioning"]);

            if (row.Table.Columns.Contains("EnablePublicWrite") && row["EnablePublicWrite"] != DBNull.Value && row["EnablePublicWrite"] != null)
                ret.EnablePublicWrite = Convert.ToBoolean(row["EnablePublicWrite"]);

            if (row.Table.Columns.Contains("EnablePublicRead") && row["EnablePublicRead"] != DBNull.Value && row["EnablePublicRead"] != null)
                ret.EnablePublicRead = Convert.ToBoolean(row["EnablePublicRead"]);
             
            if (row.Table.Columns.Contains("CreatedUtc") && row["CreatedUtc"] != DBNull.Value && row["CreatedUtc"] != null
                && !String.IsNullOrEmpty(row["CreatedUtc"].ToString()))
                ret.CreatedUtc = Convert.ToDateTime(row["CreatedUtc"]); 

            return ret;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
