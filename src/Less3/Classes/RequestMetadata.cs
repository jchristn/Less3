namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using S3ServerLibrary;

    /// <summary>
    /// Less3 request metadata.
    /// </summary>
    public class RequestMetadata
    {
        #region Public-Members

        /// <summary>
        /// User.
        /// </summary>
        public User User = null;

        /// <summary>
        /// Credential.
        /// </summary>
        public Credential Credential = null;

        /// <summary>
        /// Bucket.
        /// </summary>
        public Bucket Bucket = null;

        /// <summary>
        /// Bucket client.
        /// </summary>
        internal BucketClient BucketClient = null;

        /// <summary>
        /// Bucket access control lists.
        /// </summary>
        public List<BucketAcl> BucketAcls = new List<BucketAcl>();

        /// <summary>
        /// Bucket tags.
        /// </summary>
        public List<BucketTag> BucketTags = new List<BucketTag>();

        /// <summary>
        /// Object.
        /// </summary>
        public Obj Obj = null;

        /// <summary>
        /// Object access control lists.
        /// </summary>
        public List<ObjectAcl> ObjectAcls = new List<ObjectAcl>();

        /// <summary>
        /// Object tags.
        /// </summary>
        public List<ObjectTag> ObjectTags = new List<ObjectTag>();

        /// <summary>
        /// Authentication result.
        /// </summary>
        public AuthenticationResult Authentication = AuthenticationResult.NotAuthenticated;

        /// <summary>
        /// Authorization result.
        /// </summary>
        public AuthorizationResult Authorization = AuthorizationResult.NotAuthorized;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestMetadata()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
