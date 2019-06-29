using System;
using System.Collections.Generic;
using System.Text;

namespace Less3.Classes
{
    public class User
    {
        #region Public-Members

        public string Name { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public User()
        {

        }

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
