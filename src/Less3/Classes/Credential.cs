namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Watson.ORM.Core;

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
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID of the credential.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User GUID.
        /// </summary>
        [Column("userguid", false, DataTypes.Nvarchar, 64, false)]
        public string UserGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Description.
        /// </summary>
        [Column("description", false, DataTypes.Nvarchar, 256, true)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Access key.
        /// </summary>
        [Column("accesskey", false, DataTypes.Nvarchar, 256, false)]
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Secret key.
        /// </summary>
        [Column("secretkey", false, DataTypes.Nvarchar, 256, false)]
        public string SecretKey { get; set; } = null;

        /// <summary>
        /// Indicates if the secret key is base64 encoded.
        /// </summary>
        [Column("isbase64", false, DataTypes.Boolean, false)]
        public bool IsBase64 { get; set; } = false;

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
        public Credential()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="description">Description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="isBase64">Is base64 encoded.</param>
        public Credential(string userGuid, string description, string accessKey, string secretKey, bool isBase64)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            GUID = Guid.NewGuid().ToString();
            UserGUID = userGuid;
            Description = description;
            AccessKey = accessKey;
            SecretKey = secretKey;
            IsBase64 = isBase64;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="description">Description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="isBase64">Is base64 encoded.</param>
        public Credential(string guid, string userGuid, string description, string accessKey, string secretKey, bool isBase64)
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
            IsBase64 = isBase64;
        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
