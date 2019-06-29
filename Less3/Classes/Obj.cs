using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    public class Obj
    {
        #region Public-Members

        public int Id { get; set; }
        public string Owner { get; set; }
        public string Author { get; set; }
        public string Key { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public long Version { get; set; }
        public string Etag { get; set; }
        public string RetentionType { get; set; }
        public string BlobFilename { get; set; }
        public int DeleteMarker { get; set; }
        public string Md5 { get; set; } 
        public DateTime CreatedUtc { get; set; }
        public DateTime LastUpdateUtc { get; set; }
        public DateTime LastAccessUtc { get; set; }
        public DateTime? ExpirationUtc = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public Obj()
        {

        }


        public static Obj FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            Obj ret = new Obj();
             
            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                ret.Id = Convert.ToInt32(row["Id"]);

            if (row.Table.Columns.Contains("Owner") && row["Owner"] != DBNull.Value && row["Owner"] != null)
                ret.Owner = row["Owner"].ToString();

            if (row.Table.Columns.Contains("Author") && row["Author"] != DBNull.Value && row["Author"] != null)
                ret.Author = row["Author"].ToString();

            if (row.Table.Columns.Contains("Key") && row["Key"] != DBNull.Value && row["Key"] != null)
                ret.Key = row["Key"].ToString();

            if (row.Table.Columns.Contains("ContentType") && row["ContentType"] != DBNull.Value && row["ContentType"] != null)
                ret.ContentType = row["ContentType"].ToString();

            if (row.Table.Columns.Contains("ContentLength") && row["ContentLength"] != DBNull.Value && row["ContentLength"] != null)
                ret.ContentLength = Convert.ToInt64(row["ContentLength"]);

            if (row.Table.Columns.Contains("Version") && row["Version"] != DBNull.Value && row["Version"] != null)
                ret.Version= Convert.ToInt64(row["Version"]);

            if (row.Table.Columns.Contains("Etag") && row["Etag"] != DBNull.Value && row["Etag"] != null)
                ret.Etag = row["Etag"].ToString();

            if (row.Table.Columns.Contains("RetentionType") && row["RetentionType"] != DBNull.Value && row["RetentionType"] != null)
                ret.RetentionType = row["RetentionType"].ToString();

            if (row.Table.Columns.Contains("BlobFilename") && row["BlobFilename"] != DBNull.Value && row["BlobFilename"] != null)
                ret.BlobFilename = row["BlobFilename"].ToString();

            if (row.Table.Columns.Contains("DeleteMarker") && row["DeleteMarker"] != DBNull.Value && row["DeleteMarker"] != null)
                ret.DeleteMarker = Convert.ToInt32(row["DeleteMarker"]);

            if (row.Table.Columns.Contains("Md5") && row["Md5"] != DBNull.Value && row["Md5"] != null)
                ret.Md5 = row["Md5"].ToString();
             
            if (row.Table.Columns.Contains("CreatedUtc") && row["CreatedUtc"] != DBNull.Value && row["CreatedUtc"] != null
                && !String.IsNullOrEmpty(row["CreatedUtc"].ToString()))
                ret.CreatedUtc = Convert.ToDateTime(row["CreatedUtc"]);

            if (row.Table.Columns.Contains("LastUpdateUtc") && row["LastUpdateUtc"] != DBNull.Value && row["LastUpdateUtc"] != null
                && !String.IsNullOrEmpty(row["LastUpdateUtc"].ToString()))
                ret.LastUpdateUtc = Convert.ToDateTime(row["LastUpdateUtc"]);

            if (row.Table.Columns.Contains("LastAccessUtc") && row["LastAccessUtc"] != DBNull.Value && row["LastAccessUtc"] != null
                && !String.IsNullOrEmpty(row["LastAccessUtc"].ToString()))
                ret.LastAccessUtc = Convert.ToDateTime(row["LastAccessUtc"]);

            if (row.Table.Columns.Contains("ExpirationUtc") && row["ExpirationUtc"] != DBNull.Value && row["ExpirationUtc"] != null
                && !String.IsNullOrEmpty(row["ExpirationUtc"].ToString()))
                ret.ExpirationUtc = Convert.ToDateTime(row["ExpirationUtc"]);

            return ret;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
