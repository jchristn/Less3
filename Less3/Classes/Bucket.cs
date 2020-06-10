using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Watson.ORM.Core;
using Less3.Storage;
 
namespace Less3.Classes
{
    /// <summary>
    /// Bucket configuration.
    /// </summary>
    [Table("buckets")]
    public class Bucket
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; }

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; }

        /// <summary>
        /// GUID of the owner.
        /// </summary>
        [Column("ownerguid", false, DataTypes.Nvarchar, 64, false)]
        public string OwnerGUID { get; set; }

        /// <summary>
        /// Name of the bucket.
        /// </summary>
        [Column("name", false, DataTypes.Nvarchar, 256, false)]
        public string Name { get; set; }
        
        /// <summary>
        /// Type of storage driver.
        /// </summary>
        [Column("storagetype", false, DataTypes.Enum, 16, false)]
        public StorageDriverType StorageType { get; set; }

        /// <summary>
        /// Objects directory.
        /// </summary>
        [Column("diskdirectory", false, DataTypes.Nvarchar, 256, false)]
        public string DiskDirectory { get; set; }

        /// <summary>
        /// Enable or disable versioning.
        /// </summary>
        [Column("enableversioning", false, DataTypes.Boolean, false)]
        public bool EnableVersioning { get; set; }

        /// <summary>
        /// Enable or disable public write.
        /// </summary>
        [Column("enablepublicwrite", false, DataTypes.Boolean, false)]
        public bool EnablePublicWrite { get; set; }

        /// <summary>
        /// Enable or disable public read.
        /// </summary>
        [Column("enablepublicread", false, DataTypes.Boolean, false)]
        public bool EnablePublicRead { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, false)]
        public DateTime CreatedUtc { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Bucket()
        {

        }
         
        internal Bucket(
            string guid,
            string name,
            string owner, 
            StorageDriverType storageType,
            string diskDirectory)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner)); 
            if (String.IsNullOrEmpty(diskDirectory)) throw new ArgumentNullException(nameof(diskDirectory));

            GUID = guid;
            Name = name;
            StorageType = storageType;
            DiskDirectory = diskDirectory;
            OwnerGUID = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
