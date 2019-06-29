using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
 
namespace Less3.Classes
{
    public class BucketConfiguration
    {
        #region Public-Members

        public string Name { get; set; } 
        public string DatabaseFilename { get; set; }
        public bool DatabaseDebug { get; set; }
        public string ObjectsDirectory { get; set; }
        public string Owner { get; set; }
        public bool EnableVersioning { get; set; } 
        public bool EnablePublicWrite { get; set; }
        public bool EnablePublicRead { get; set; }
        public List<string> PermittedAccessKeys { get; set; } 
        public DateTime CreatedUtc { get; set; }

        #endregion

        #region Private-Members
         
        #endregion

        #region Constructors-and-Factories

        public BucketConfiguration()
        {

        }

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
