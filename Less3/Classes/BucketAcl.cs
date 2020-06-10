using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;

namespace Less3.Classes
{
    /// <summary>
    /// Access control list entry for a bucket.
    /// </summary>
    [Table("bucketacls")]
    public class BucketAcl
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; }
        
        /// <summary>
        /// User group.
        /// </summary>
        [Column("usergroup", false, DataTypes.Nvarchar, 256, true)]
        public string UserGroup { get; set; }

        /// <summary>
        /// Bucket GUID.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, true)]
        public string BucketGUID { get; set; }

        /// <summary>
        /// User GUID.
        /// </summary>
        [Column("userguid", false, DataTypes.Nvarchar, 64, true)]
        public string UserGUID { get; set; }

        /// <summary>
        /// GUID of the issuing user.
        /// </summary>
        [Column("issuedbyuserguid", false, DataTypes.Nvarchar, 64, true)]
        public string IssuedByUserGUID { get; set; }

        /// <summary>
        /// Permit read operations.
        /// </summary>
        [Column("permitread", false, DataTypes.Boolean, false)]
        public bool PermitRead { get; set; }
        
        /// <summary>
        /// Permit write operations.
        /// </summary>
        [Column("permitwrite", false, DataTypes.Boolean, false)]
        public bool PermitWrite { get; set; }

        /// <summary>
        /// Permit access control read operations.
        /// </summary>
        [Column("permitreadacp", false, DataTypes.Boolean, false)]
        public bool PermitReadAcp { get; set; }
        
        /// <summary>
        /// Permit access control write operations.
        /// </summary>
        [Column("permitwriteacp", false, DataTypes.Boolean, false)]
        public bool PermitWriteAcp { get; set; }
        
        /// <summary>
        /// Permit full control.
        /// </summary>
        [Column("permitfullcontrol", false, DataTypes.Boolean, false)]
        public bool FullControl { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BucketAcl()
        {

        }

        internal static BucketAcl BucketGroupAcl(
            string groupName, 
            string issuedByUserGuid, 
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));

            BucketAcl ret = new BucketAcl();

            ret.UserGroup = groupName;
            ret.UserGUID = null;
            ret.IssuedByUserGUID = issuedByUserGuid;

            ret.PermitRead = permitRead;
            ret.PermitWrite = permitWrite;
            ret.PermitReadAcp = permitReadAcp;
            ret.PermitWriteAcp = permitWriteAcp;
            ret.FullControl = fullControl;

            return ret;
        }

        internal static BucketAcl BucketUserAcl(
            string userGuid, 
            string issuedByUserGuid,
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(issuedByUserGuid)) throw new ArgumentNullException(nameof(issuedByUserGuid));

            BucketAcl ret = new BucketAcl();

            ret.UserGroup = null;
            ret.UserGUID = userGuid;
            ret.IssuedByUserGUID = issuedByUserGuid;

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
        /// Human-readable string of the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string
                ret = "--- Bucket ACL " + Id + " ---" + Environment.NewLine +
                "  User group      : " + UserGroup + Environment.NewLine +
                "  User GUID       : " + UserGUID + Environment.NewLine +
                "  Issued by       : " + IssuedByUserGUID + Environment.NewLine +
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
