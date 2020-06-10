using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;

namespace Less3.Classes
{
    /// <summary>
    /// Tag entry for a bucket.
    /// </summary>
    [Table("buckettags")]
    public class BucketTag
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
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; }

        /// <summary>
        /// Key.
        /// </summary>
        [Column("tagkey", false, DataTypes.Nvarchar, 256, false)]
        public string Key { get; set; }
        
        /// <summary>
        /// Value.
        /// </summary>
        [Column("tagvalue", false, DataTypes.Nvarchar, 1024, true)]
        public string Value { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BucketTag()
        {

        }
         
        #endregion

        #region Public-Methods
         
        #endregion

        #region Private-Methods

        #endregion
    }
}
