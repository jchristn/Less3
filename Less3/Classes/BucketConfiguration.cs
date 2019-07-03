using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
 
namespace Less3.Classes
{
    /// <summary>
    /// Bucket configuration.
    /// </summary>
    public class BucketConfiguration
    {
        #region Public-Members

        /// <summary>
        /// Name of the bucket.
        /// </summary>
        public string Name { get; set; } 

        /// <summary>
        /// Full path and filename to the bucket database.
        /// </summary>
        public string DatabaseFilename { get; set; }

        /// <summary>
        /// Enable or disable database debugging to the console.
        /// </summary>
        public bool DatabaseDebug { get; set; }

        /// <summary>
        /// Full path where objects should be stored.
        /// </summary>
        public string ObjectsDirectory { get; set; }

        /// <summary>
        /// Bucket owner name.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Enable or disable object versioning.
        /// </summary>
        public bool EnableVersioning { get; set; } 

        /// <summary>
        /// Enable or disable public write.
        /// </summary>
        public bool EnablePublicWrite { get; set; }

        /// <summary>
        /// Enable or disable public read.
        /// </summary>
        public bool EnablePublicRead { get; set; }

        /// <summary>
        /// List of access keys permitted to the bucket.
        /// </summary>
        public List<string> PermittedAccessKeys { get; set; } 

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        #endregion

        #region Private-Members
         
        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BucketConfiguration()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <param name="owner">Bucket owner.</param>
        /// <param name="databaseFilename">Database filename and path.</param>
        /// <param name="objectsDirectory">Directory where objects should be stored.</param>
        /// <param name="permittedAccessKeys">Permitted access keys.</param>
        public BucketConfiguration(
            string name, 
            string owner,
            string databaseFilename, 
            string objectsDirectory, 
            List<string> permittedAccessKeys)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(owner)) throw new ArgumentNullException(nameof(owner));
            if (String.IsNullOrEmpty(databaseFilename)) throw new ArgumentNullException(nameof(databaseFilename));
            if (String.IsNullOrEmpty(objectsDirectory)) throw new ArgumentNullException(nameof(objectsDirectory));  
            Name = name;
            DatabaseFilename = databaseFilename;
            DatabaseDebug = false;
            ObjectsDirectory = objectsDirectory;
            Owner = owner;
            CreatedUtc = DateTime.Now.ToUniversalTime(); 

            PermittedAccessKeys = permittedAccessKeys;
            if (PermittedAccessKeys == null) PermittedAccessKeys = new List<string>();
            if (!PermittedAccessKeys.Contains(owner)) PermittedAccessKeys.Add(owner); 
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
