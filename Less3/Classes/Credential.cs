using System;
using System.Collections.Generic;
using System.Text;

namespace Less3.Classes
{
    public class Credential
    {
        #region Public-Members

        public string User { get; set; }
        public string Name { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public List<RequestType> Permit { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public Credential()
        {

        }

        public Credential(string user, string name, string accessKey, string secretKey, List<RequestType> permit)
        {
            if (String.IsNullOrEmpty(user)) throw new ArgumentNullException(nameof(user));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (permit == null || permit.Count < 1) throw new ArgumentException("At least one permission must be specified.");

            User = user;
            Name = name;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Permit = permit;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
