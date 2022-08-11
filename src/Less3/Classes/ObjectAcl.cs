using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;
using Newtonsoft.Json;

namespace Less3.Classes
{
    /// <summary>
    /// Access control list entry for an object.
    /// </summary>
    [Table("objectacls")]
    public class ObjectAcl
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User group.
        /// </summary>
        [Column("usergroup", false, DataTypes.Nvarchar, 256, true)]
        public string UserGroup { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        [Column("userguid", false, DataTypes.Nvarchar, 64, true)]
        public string UserGUID { get; set; } = null;

        /// <summary>
        /// GUID of the issuing user.
        /// </summary>
        [Column("issuedbyuserguid", false, DataTypes.Nvarchar, 64, true)]
        public string IssuedByUserGUID { get; set; } = null;

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; } = null;

        /// <summary>
        /// GUID of the object.
        /// </summary>
        [Column("objectguid", false, DataTypes.Nvarchar, 64, false)]
        public string ObjectGUID { get; set; } = null;

        /// <summary>
        /// Permit read operations.
        /// </summary>
        [Column("permitread", false, DataTypes.Boolean, false)]
        public bool PermitRead { get; set; } = false;

        /// <summary>
        /// Permit write operations.
        /// </summary>
        [Column("permitwrite", false, DataTypes.Boolean, false)]
        public bool PermitWrite { get; set; } = false;

        /// <summary>
        /// Permit access control read operations.
        /// </summary>
        [Column("permitreadacp", false, DataTypes.Boolean, false)]
        public bool PermitReadAcp { get; set; } = false;

        /// <summary>
        /// Permit access control write operations.
        /// </summary>
        [Column("permitwriteacp", false, DataTypes.Boolean, false)]
        public bool PermitWriteAcp { get; set; } = false;

        /// <summary>
        /// Permit full control.
        /// </summary>
        [Column("permitfullcontrol", false, DataTypes.Boolean, false)]
        public bool FullControl { get; set; } = false;

        /// <summary>
        /// Timestamp from record creation, in UTC time.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, 6, 6, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ObjectAcl()
        {

        }
         
        /// <summary>
        /// Create a group ACL.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <param name="issuedByUserGuid">Issued by user GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit access control read.</param>
        /// <param name="permitWriteAcp">Permit access control write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>Instance.</returns>
        public static ObjectAcl GroupAcl(
            string groupName, 
            string issuedByUserGuid, 
            string bucketGuid,
            string objectGuid,  
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid)); 

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = groupName;
            ret.UserGUID = null;
            ret.IssuedByUserGUID = issuedByUserGuid;
            ret.BucketGUID = bucketGuid;
            ret.ObjectGUID = objectGuid;

            ret.PermitRead = permitRead;
            ret.PermitWrite = permitWrite;
            ret.PermitReadAcp = permitReadAcp;
            ret.PermitWriteAcp = permitWriteAcp;
            ret.FullControl = fullControl;

            return ret;
        }

        /// <summary>
        /// Create a user ACL.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="issuedByUserGuid">Issued by user GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit access control read.</param>
        /// <param name="permitWriteAcp">Permit access control write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>Instance.</returns>
        public static ObjectAcl UserAcl(
            string userGuid, 
            string issuedByUserGuid, 
            string bucketGuid,
            string objectGuid,  
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid)); 

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = null;
            ret.UserGUID = userGuid;
            ret.IssuedByUserGUID = issuedByUserGuid;
            ret.BucketGUID = bucketGuid;
            ret.ObjectGUID = objectGuid;

            ret.PermitRead = permitRead;
            ret.PermitWrite = permitWrite;
            ret.PermitReadAcp = permitReadAcp;
            ret.PermitWriteAcp = permitWriteAcp;
            ret.FullControl = fullControl;

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
                "  Bucket GUID     : " + BucketGUID + Environment.NewLine +
                "  Object GUID     : " + ObjectGUID + Environment.NewLine +
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
