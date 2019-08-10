using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// Credential.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// ID (database row).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// User GUID.
        /// </summary>
        public string UserGUID { get; set; }

        /// <summary>
        /// Name of the credential or description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Access key.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Secret key.
        /// </summary>
        public string SecretKey { get; set; }
         
        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Credential()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="description">Name of the credential or description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param> 
        public Credential(string userGuid, string description, string accessKey, string secretKey)
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="description">Name of the credential or description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param> 
        public Credential(string guid, string userGuid, string description, string accessKey, string secretKey)
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

        /// <summary>
        /// Create an instance from a DataRow.
        /// </summary>
        /// <param name="row">DataRow.</param>
        /// <returns>Credential.</returns>
        public static Credential FromDataRow(DataRow row)
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
