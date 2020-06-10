using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;

namespace Less3.Classes
{
    /// <summary>
    /// Credential.
    /// </summary>
    [Table("credential")]
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; }

        /// <summary>
        /// GUID of the credential.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; }

        /// <summary>
        /// User GUID.
        /// </summary>
        [Column("userguid", false, DataTypes.Nvarchar, 64, false)]
        public string UserGUID { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        [Column("description", false, DataTypes.Nvarchar, 256, true)]
        public string Description { get; set; }

        /// <summary>
        /// Access key.
        /// </summary>
        [Column("accesskey", false, DataTypes.Nvarchar, 256, false)]
        public string AccessKey { get; set; }

        /// <summary>
        /// Secret key.
        /// </summary>
        [Column("secretkey", false, DataTypes.Nvarchar, 256, false)]
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
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
