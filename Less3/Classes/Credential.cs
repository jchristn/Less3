using System;
using System.Collections.Generic;
using System.Text;

namespace Less3.Classes
{
    /// <summary>
    /// Credential.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// User name.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Name of the credential or description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Access key.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Secret key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// List of permitted request types.
        /// </summary>
        public List<RequestType> Permit { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Credential()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="name">Name of the credential or description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="permit">List of permitted request types.</param>
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
