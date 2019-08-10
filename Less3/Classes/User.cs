using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// User object.
    /// </summary>
    public class User
    {
        #region Public-Members

        /// <summary>
        /// ID (database row).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User GUID.
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// User name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User email.
        /// </summary>
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <param name="email">User email.</param>
        public User(string name, string email)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            GUID = Guid.NewGuid().ToString();
            Name = name;
            Email = email;
        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <param name="name">User name.</param>
        /// <param name="email">User email.</param>
        public User(string guid, string name, string email)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            GUID = guid;
            Name = name;
            Email = email;
        }

        /// <summary>
        /// Create an instance from a DataRow.
        /// </summary>
        /// <param name="row">DataRow.</param>
        /// <returns>User.</returns>
        public static User FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            User ret = new User();

            if (row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value && row["Id"] != null)
                ret.Id = Convert.ToInt32(row["Id"]);

            if (row.Table.Columns.Contains("GUID") && row["GUID"] != DBNull.Value && row["GUID"] != null)
                ret.GUID = row["GUID"].ToString();

            if (row.Table.Columns.Contains("Name") && row["Name"] != DBNull.Value && row["Name"] != null)
                ret.Name = row["Name"].ToString();

            if (row.Table.Columns.Contains("Email") && row["Email"] != DBNull.Value && row["Email"] != null)
                ret.Email = row["Email"].ToString(); 

            return ret;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
