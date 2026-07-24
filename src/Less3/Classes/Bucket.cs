namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;
    using Less3.Storage;

    /// <summary>
    /// Bucket configuration.
    /// </summary>
    public class Bucket
    {
        #region Public-Members

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateBucketId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Id of the owner.
        /// </summary>
        public string OwnerId { get; set; } = Less3.Helpers.IdGenerator.GenerateUserId();

        /// <summary>
        /// Name of the bucket.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Bucket region string.
        /// </summary>
        public string RegionString { get; set; } = "us-west-1";

        /// <summary>
        /// Type of storage driver.
        /// </summary>
        public StorageDriverType StorageType { get; set; } = StorageDriverType.Disk;

        /// <summary>
        /// Objects directory.
        /// </summary>
        public string DiskDirectory { get; set; } = "./disk/";

        /// <summary>
        /// Enable or disable versioning.
        /// </summary>
        public bool EnableVersioning { get; set; } = false;

        /// <summary>
        /// Enable or disable public write.
        /// </summary>
        public bool EnablePublicWrite { get; set; } = false;

        /// <summary>
        /// Enable or disable public read.
        /// </summary>
        public bool EnablePublicRead { get; set; } = false;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
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
        /// <param name="owner">Owner Id.</param>
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
            OwnerId = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="name">Name.</param>
        /// <param name="owner">Owner Id.</param>
        /// <param name="storageType">Storage type.</param>
        /// <param name="diskDirectory">Disk directory.</param>
        /// <param name="region">Region.</param>
        public Bucket(
            string id,
            string name,
            string owner,
            StorageDriverType storageType,
            string diskDirectory,
            string region = "us-west-1")
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(diskDirectory)) throw new ArgumentNullException(nameof(diskDirectory));

            Id = id;
            Name = name;
            RegionString = region;
            StorageType = storageType;
            DiskDirectory = diskDirectory;
            OwnerId = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime();
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
