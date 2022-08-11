using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;
using Newtonsoft.Json;

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
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the user.
        /// </summary>
        [Column("name", false, DataTypes.Nvarchar, 256, false)]
        public string Name { get; set; } = null;

        /// <summary>
        /// Email address of the user.
        /// </summary>
        [Column("email", false, DataTypes.Nvarchar, 256, false)]
        public string Email { get; set; } = null;

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
        public User()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="email">Email.</param>
        public User(string name, string email)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            GUID = Guid.NewGuid().ToString();
            Name = name;
            Email = email;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="name">Name.</param>
        /// <param name="email">Email.</param>
        public User(string guid, string name, string email)
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
