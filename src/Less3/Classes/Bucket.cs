using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the owner.
        /// </summary>
        [Column("ownerguid", false, DataTypes.Nvarchar, 64, false)]
        public string OwnerGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the bucket.
        /// </summary>
        [Column("name", false, DataTypes.Nvarchar, 256, false)]
        public string Name { get; set; } = null;

        /// <summary>
        /// Bucket region string.
        /// </summary>
        [Column("regionstring", false, DataTypes.Nvarchar, 32, false)]
        public string RegionString { get; set; } = "us-west-1";

        /// <summary>
        /// Type of storage driver.
        /// </summary>
        [Column("storagetype", false, DataTypes.Enum, 16, false)]
        public StorageDriverType StorageType { get; set; } = StorageDriverType.Disk;

        /// <summary>
        /// Objects directory.
        /// </summary>
        [Column("diskdirectory", false, DataTypes.Nvarchar, 256, false)]
        public string DiskDirectory { get; set; } = "./disk/";

        /// <summary>
        /// Enable or disable versioning.
        /// </summary>
        [Column("enableversioning", false, DataTypes.Boolean, false)]
        public bool EnableVersioning { get; set; } = false;

        /// <summary>
        /// Enable or disable public write.
        /// </summary>
        [Column("enablepublicwrite", false, DataTypes.Boolean, false)]
        public bool EnablePublicWrite { get; set; } = false;

        /// <summary>
        /// Enable or disable public read.
        /// </summary>
        [Column("enablepublicread", false, DataTypes.Boolean, false)]
        public bool EnablePublicRead { get; set; } = false;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Bucket()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="owner">Owner GUID.</param>
        /// <param name="storageType">Storage type.</param>
        /// <param name="diskDirectory">Disk directory.</param>
        /// <param name="region">Region.</param>
        public Bucket(
            string name,
            string owner,
            StorageDriverType storageType,
            string diskDirectory,
            string region = "us-west-1")
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(diskDirectory)) throw new ArgumentNullException(nameof(diskDirectory));

            Name = name;
            RegionString = region;
            StorageType = storageType;
            DiskDirectory = diskDirectory;
            OwnerGUID = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="name">Name.</param>
        /// <param name="owner">Owner GUID.</param>
        /// <param name="storageType">Storage type.</param>
        /// <param name="diskDirectory">Disk directory.</param>
        /// <param name="region">Region.</param>
        public Bucket(
            string guid,
            string name,
            string owner,
            StorageDriverType storageType,
            string diskDirectory,
            string region = "us-west-1")
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(diskDirectory)) throw new ArgumentNullException(nameof(diskDirectory));

            GUID = guid;
            Name = name;
            RegionString = region;
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
