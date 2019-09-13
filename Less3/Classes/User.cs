using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// User object.
    /// </summary>
    internal class User
    {
        #region Internal-Members

        internal int Id { get; set; }
        internal string GUID { get; set; }
        internal string Name { get; set; }
        internal string Email { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        internal User()
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

        internal static User FromDataRow(DataRow row)
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
