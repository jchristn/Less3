using System;
using System.Collections.Generic;
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
        /// User name.
        /// </summary>
        public string Name { get; set; }

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
        public User(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        #endregion

        #region Public-Methods
         
        #endregion

        #region Private-Methods

        #endregion
    }
}
