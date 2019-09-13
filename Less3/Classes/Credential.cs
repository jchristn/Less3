using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// Credential.
    /// </summary>
    internal class Credential
    {
        #region Internal-Members

        internal int Id { get; set; }
        internal string GUID { get; set; }
        internal string UserGUID { get; set; }
        internal string Description { get; set; }
        internal string AccessKey { get; set; }
        internal string SecretKey { get; set; }
         
        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        internal Credential()
        {

        }

        internal Credential(string userGuid, string description, string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            GUID = Guid.NewGuid().ToString();
            UserGUID = userGuid;
            Description = description;
            AccessKey = accessKey;
            SecretKey = secretKey;
        }

        internal Credential(string guid, string userGuid, string description, string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            GUID = guid;
            UserGUID = userGuid;
            Description = description;
            AccessKey = accessKey;
            SecretKey = secretKey;
        }

        internal static Credential FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            Credential ret = new Credential();

            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                ret.Id = Convert.ToInt32(row["Id"]);

            if (row.Table.Columns.Contains("GUID") && row["GUID"] != DBNull.Value && row["GUID"] != null)
                ret.GUID = row["GUID"].ToString();

            if (row.Table.Columns.Contains("UserGUID") && row["UserGUID"] != DBNull.Value && row["UserGUID"] != null)
                ret.UserGUID = row["UserGUID"].ToString();

            if (row.Table.Columns.Contains("Description") && row["Description"] != DBNull.Value && row["Description"] != null)
                ret.Description = row["Description"].ToString();

            if (row.Table.Columns.Contains("AccessKey") && row["AccessKey"] != DBNull.Value && row["AccessKey"] != null)
                ret.AccessKey = row["AccessKey"].ToString();

            if (row.Table.Columns.Contains("SecretKey") && row["SecretKey"] != DBNull.Value && row["SecretKey"] != null)
                ret.SecretKey = row["SecretKey"].ToString();

            return ret;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
