namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Access control list entry for an object.
    /// </summary>
    public class ObjectAcl
    {
        #region Public-Members

        /// <summary>
        /// Id.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateObjectAclId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// User group.
        /// </summary>
        public string UserGroup { get; set; } = null;

        /// <summary>
        /// User Id.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Id of the issuing user.
        /// </summary>
        public string IssuedByUserId { get; set; } = null;

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string BucketId { get; set; } = null;

        /// <summary>
        /// Id of the object.
        /// </summary>
        public string ObjectId { get; set; } = null;

        /// <summary>
        /// Permit read operations.
        /// </summary>
        public bool PermitRead { get; set; } = false;

        /// <summary>
        /// Permit write operations.
        /// </summary>
        public bool PermitWrite { get; set; } = false;

        /// <summary>
        /// Permit access control read operations.
        /// </summary>
        public bool PermitReadAcp { get; set; } = false;

        /// <summary>
        /// Permit access control write operations.
        /// </summary>
        public bool PermitWriteAcp { get; set; } = false;

        /// <summary>
        /// Permit full control.
        /// </summary>
        public bool FullControl { get; set; } = false;

        /// <summary>
        /// Timestamp from record creation, in UTC time.
        /// </summary>
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
        /// <param name="issuedByUserId">Issued by user Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit access control read.</param>
        /// <param name="permitWriteAcp">Permit access control write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>Instance.</returns>
        public static ObjectAcl GroupAcl(
            string groupName, 
            string issuedByUserId, 
            string bucketId,
            string objectId,  
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(issuedByUserId)) throw new ArgumentNullException(nameof(issuedByUserId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId)); 

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = groupName;
            ret.UserId = null;
            ret.IssuedByUserId = issuedByUserId;
            ret.BucketId = bucketId;
            ret.ObjectId = objectId;

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
        /// <param name="userId">User Id.</param>
        /// <param name="issuedByUserId">Issued by user Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="permitRead">Permit read.</param>
        /// <param name="permitWrite">Permit write.</param>
        /// <param name="permitReadAcp">Permit access control read.</param>
        /// <param name="permitWriteAcp">Permit access control write.</param>
        /// <param name="fullControl">Full control.</param>
        /// <returns>Instance.</returns>
        public static ObjectAcl UserAcl(
            string userId, 
            string issuedByUserId, 
            string bucketId,
            string objectId,  
            bool permitRead,
            bool permitWrite,
            bool permitReadAcp,
            bool permitWriteAcp,
            bool fullControl)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(issuedByUserId)) throw new ArgumentNullException(nameof(issuedByUserId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId)); 

            ObjectAcl ret = new ObjectAcl();

            ret.UserGroup = null;
            ret.UserId = userId;
            ret.IssuedByUserId = issuedByUserId;
            ret.BucketId = bucketId;
            ret.ObjectId = objectId;

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
                "  User Id       : " + UserId + Environment.NewLine +
                "  Issued by       : " + IssuedByUserId + Environment.NewLine +
                "  Bucket Id     : " + BucketId + Environment.NewLine +
                "  Object Id     : " + ObjectId + Environment.NewLine +
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
