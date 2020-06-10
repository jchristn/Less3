using System;
using System.Collections.Generic;
using System.Text;
using S3ServerInterface;

namespace Less3.Classes
{
    /// <summary>
    /// Less3 request metadata.
    /// </summary>
    public class RequestMetadata
    { 
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public RequestMetadata()
        {

        }
    }
}
