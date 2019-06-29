using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SyslogLogging;

namespace Less3.Classes
{
    public class UserManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;

        private readonly object _UsersLock = new object();
        private List<User> _Users = new List<User>();

        #endregion

        #region Constructors-and-Factories

        public UserManager(Settings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Settings = settings;
            _Logging = logging;

            Load();
        }

        #endregion

        #region Public-Methods

        public void Load()
        {
            lock (_UsersLock)
            {
                _Users = Common.DeserializeJson<List<User>>(Common.ReadTextFile(_Settings.Files.Users));
            }
        }

        public void Save()
        {
            lock (_UsersLock)
            {
                Common.WriteFile(_Settings.Files.Users, Encoding.UTF8.GetBytes(Common.SerializeJson(_Users, true)));
            }
        }
         
        public void Add(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            User newUser = new User(name);
            bool added = false;

            lock (_UsersLock)
            {
                bool exists = _Users.Exists(u => u.Name.Equals(name));
                if (!exists)
                {
                    _Users.Add(newUser);
                    added = true;
                }
            }

            if (added) Save();
        }

        public void Remove(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            bool removed = false;

            lock (_UsersLock)
            {
                bool exists = _Users.Exists(u => u.Name.Equals(name));
                if (exists)
                {
                    User test = _Users.Where(c => c.Name.Equals(name)).First();
                    _Users.Remove(test);
                    removed = true;
                }
            }

            if (removed) Save();
        }

        public bool Get(string name, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            lock (_UsersLock)
            {
                bool exists = _Users.Exists(u => u.Name.Equals(name));
                if (exists)
                {
                    User test = _Users.Where(c => c.Name.Equals(name)).First();
                    user = test;
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
