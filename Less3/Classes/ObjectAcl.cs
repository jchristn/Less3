using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// Access control list entry for an object.
    /// </summary>
    public class ObjectAcl
    {
        #region Public-Members

        /// <summary>
        /// ID (database row).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The group to which this entry applies.
        /// </summary>
        public string UserGroup { get; set; }

        /// <summary>
        /// The user GUID to which this entry applies.
        /// </summary>
        public string UserGUID { get; set; }
        
        /// <summary>
        /// The user GUID of the user that issued this permission.
        /// </summary>
        public string IssuedByUserGUID { get; set; }

        /// <summary>
        /// The object key to which this entry applies.
        /// </summary>
        public string ObjectKey { get; set; }

        /// <summary>
        /// The object key's version to which this entry applies.
        /// </summary>
        public long ObjectVersion { get; set; }

        /// <summary>
        /// Enable read.
        /// </summary>
        public bool PermitRead { get; set; }

        /// <summary>
        /// Enable write.
        /// </summary>
        public bool PermitWrite { get; set; }

        /// <summary>
        /// Enable access control read.
        /// </summary>
        public bool PermitReadAcp { get; set; }

        /// <summary>
        /// Enable access control write.
        /// </summary>
        public bool PermitWriteAcp { get; set; }

        /// <summary>
        /// Enable full control.
        /// </summary>
        public bool FullControl { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public ObjectAcl()
        {

        }

        /// <summary>
        /// Create a group ACL for an object.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <param name="issuedByUserGuid">Issuing user GUID.</param>
        /// <param name="objectKey">Object key.</param>
        /// <param name="versionId">Version ID of the object.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit ACL read.</param>
        /// <param name="permitWriteAcp">Permit ACL write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>ObjectAcl.</returns>
        public static ObjectAcl ObjectGroupAcl(
            string groupName, 
            string issuedByUserGuid, 
            string objectKey, 
            long versionId,
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (versionId < 1) throw new ArgumentException("Version ID must be one or greater.");

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = groupName;
            ret.UserGUID = null;
            ret.IssuedByUserGUID = issuedByUserGuid;
            ret.ObjectKey = objectKey;
            ret.ObjectVersion = versionId;

            ret.PermitRead = permitRead;
            ret.PermitWrite = permitWrite;
            ret.PermitReadAcp = permitReadAcp;
            ret.PermitWriteAcp = permitWriteAcp;
            ret.FullControl = fullControl;

            return ret;
        }

        /// <summary>
        /// Create a user ACL for an object.
        /// </summary>
        /// <param name="userGuid">User GUID to which this ACL applies.</param>
        /// <param name="issuedByUserGuid">Issuing user GUID.</param>
        /// <param name="objectKey">Object key.</param>
        /// <param name="versionId">Version ID of the object.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit ACL read.</param>
        /// <param name="permitWriteAcp">Permit ACL write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>ObjectAcl.</returns>
        public static ObjectAcl ObjectUserAcl(
            string userGuid, 
            string issuedByUserGuid, 
            string objectKey, 
            long versionId,
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (versionId < 1) throw new ArgumentException("Version ID must be one or greater.");

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = null;
            ret.UserGUID = userGuid;
            ret.IssuedByUserGUID = issuedByUserGuid;
            ret.ObjectKey = objectKey;
            ret.ObjectVersion = versionId;

            ret.PermitRead = permitRead;
            ret.PermitWrite = permitWrite;
            ret.PermitReadAcp = permitReadAcp;
            ret.PermitWriteAcp = permitWriteAcp;
            ret.FullControl = fullControl;

            return ret;
        }
         
        /// <summary>
        /// Create an instance from a DataRow.
        /// </summary>
        /// <param name="row">DataRow.</param>
        /// <returns>Acl.</returns>
        public static ObjectAcl FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
             
            ObjectAcl ret = new ObjectAcl();

            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                ret.Id = Convert.ToInt32(row["Id"]);

            if (row.Table.Columns.Contains("UserGroup") && row["UserGroup"] != DBNull.Value && row["UserGroup"] != null)
                ret.UserGroup = row["UserGroup"].ToString();

            if (row.Table.Columns.Contains("UserGUID") && row["UserGUID"] != DBNull.Value && row["UserGUID"] != null)
                ret.UserGUID = row["UserGUID"].ToString();

            if (row.Table.Columns.Contains("IssuedByUserGUID") && row["IssuedByUserGUID"] != DBNull.Value && row["IssuedByUserGUID"] != null)
                ret.IssuedByUserGUID = row["IssuedByUserGUID"].ToString();

            if (row.Table.Columns.Contains("ObjectKey") && row["ObjectKey"] != DBNull.Value && row["ObjectKey"] != null)
                ret.ObjectKey = row["ObjectKey"].ToString();

            if (row.Table.Columns.Contains("ObjectVersion") && row["ObjectVersion"] != DBNull.Value && row["ObjectVersion"] != null)
                ret.ObjectVersion = Convert.ToInt64(row["ObjectVersion"]);

            if (row.Table.Columns.Contains("PermitRead") && row["PermitRead"] != DBNull.Value && row["PermitRead"] != null)
                if (Convert.ToBoolean(row["PermitRead"])) ret.PermitRead = true;

            if (row.Table.Columns.Contains("PermitWrite") && row["PermitWrite"] != DBNull.Value && row["PermitWrite"] != null)
                if (Convert.ToBoolean(row["PermitWrite"])) ret.PermitWrite = true;

            if (row.Table.Columns.Contains("PermitReadAcp") && row["PermitReadAcp"] != DBNull.Value && row["PermitReadAcp"] != null)
                if (Convert.ToBoolean(row["PermitReadAcp"])) ret.PermitReadAcp = true;

            if (row.Table.Columns.Contains("PermitWriteAcp") && row["PermitWriteAcp"] != DBNull.Value && row["PermitWriteAcp"] != null)
                if (Convert.ToBoolean(row["PermitWriteAcp"])) ret.PermitWriteAcp = true;

            if (row.Table.Columns.Contains("FullControl") && row["FullControl"] != DBNull.Value && row["FullControl"] != null)
                if (Convert.ToBoolean(row["FullControl"])) ret.FullControl = true;

            return ret;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Create a human-readable string of the object.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            string
                ret = "--- Object ACL " + Id + " ---" + Environment.NewLine +
                "  User group      : " + UserGroup + Environment.NewLine +
                "  User GUID       : " + UserGUID + Environment.NewLine +
                "  Issued by       : " + IssuedByUserGUID + Environment.NewLine +
                "  Object key      : " + ObjectKey + Environment.NewLine +
                "  Object version  : " + ObjectVersion + Environment.NewLine +
                "  Permissions     : " + Environment.NewLine +
                "    READ          : " + PermitRead.ToString() + Environment.NewLine +
                "    WRITE         : " + PermitWrite.ToString() + Environment.NewLine +
                "    READ_ACP      : " + PermitReadAcp.ToString() + Environment.NewLine +
                "    WRITE_ACP     : " + PermitWriteAcp.ToString() + Environment.NewLine +
                "    FULL_CONTROL  : " + FullControl.ToString() + Environment.NewLine; 

            return ret;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
