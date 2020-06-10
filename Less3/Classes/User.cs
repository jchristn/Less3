using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;

namespace Less3.Classes
{
    /// <summary>
    /// User object.
    /// </summary>
    [Table("users")]
    public class User
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; }

        /// <summary>
        /// GUID of the user.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; }

        /// <summary>
        /// Name of the user.
        /// </summary>
        [Column("name", false, DataTypes.Nvarchar, 256, false)]
        public string Name { get; set; }
         
        /// <summary>
        /// Email address of the user.
        /// </summary>
        [Column("email", false, DataTypes.Nvarchar, 256, false)]
        public string Email { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public User()
        {

        }

        internal User(string name, string email)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            GUID = Guid.NewGuid().ToString();
            Name = name;
            Email = email;
        }

        internal User(string guid, string name, string email)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            GUID = guid;
            Name = name;
            Email = email;
        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
