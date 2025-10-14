namespace Less3.Helpers
{
    using System;

    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;

    using Less3.Api.S3;
    using Less3.Classes;

    using SyslogLogging;

    /// <summary>
    /// Request validation helper methods.
    /// Provides common validation patterns used across API handlers.
    /// </summary>
    internal static class RequestValidator
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validates that request metadata can be retrieved from the S3Context.
        /// </summary>
        /// <param name="ctx">S3 request context.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <returns>RequestMetadata object containing authentication and authorization information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ctx or logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with InternalError when metadata cannot be retrieved.</exception>
        internal static RequestMetadata ValidateAndGetMetadata(S3Context ctx, LoggingModule logging, string header)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            return md;
        }

        /// <summary>
        /// Validates that the request is authorized.
        /// </summary>
        /// <param name="md">Request metadata containing authorization result.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when md or logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with AccessDenied when authorization check fails.</exception>
        internal static void ValidateAuthorization(RequestMetadata md, LoggingModule logging, string header)
        {
            if (md == null) throw new ArgumentNullException(nameof(md));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }
        }

        /// <summary>
        /// Validates that the bucket exists and has an associated client.
        /// </summary>
        /// <param name="md">Request metadata containing bucket and bucket client references.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when md or logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with NoSuchBucket when bucket or bucket client is null.</exception>
        internal static void ValidateBucketExists(RequestMetadata md, LoggingModule logging, string header)
        {
            if (md == null) throw new ArgumentNullException(nameof(md));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (md.Bucket == null || md.BucketClient == null)
            {
                logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }
        }

        /// <summary>
        /// Validates that the user is authenticated.
        /// </summary>
        /// <param name="md">Request metadata containing user and credential references.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when md or logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with AccessDenied when user or credential is null.</exception>
        internal static void ValidateAuthentication(RequestMetadata md, LoggingModule logging, string header)
        {
            if (md == null) throw new ArgumentNullException(nameof(md));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (md.User == null || md.Credential == null)
            {
                logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }
        }

        /// <summary>
        /// Parses and validates the version ID from the S3 request context.
        /// Default value is 1 if no version ID is specified.
        /// </summary>
        /// <param name="ctx">S3 request context containing version ID string.</param>
        /// <returns>Parsed version ID as a long integer. Returns 1 if version ID is not specified.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ctx is null.</exception>
        /// <exception cref="S3Exception">Thrown with NoSuchVersion when version ID cannot be parsed as a valid long integer.</exception>
        internal static long ParseVersionId(S3Context ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            return versionId;
        }

        /// <summary>
        /// Validates that an object exists and throws appropriate exceptions based on version ID.
        /// </summary>
        /// <param name="obj">Object metadata. Null indicates object does not exist.</param>
        /// <param name="versionId">Version ID being requested. Used to determine appropriate error message.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with NoSuchKey when object is null and versionId is 1. Thrown with NoSuchVersion when object is null and versionId is greater than 1.</exception>
        internal static void ValidateObjectExists(Obj obj, long versionId, LoggingModule logging, string header)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (obj == null)
            {
                if (versionId == 1)
                {
                    logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }
        }

        /// <summary>
        /// Checks if the object is marked as deleted and sets appropriate response headers.
        /// </summary>
        /// <param name="obj">Object metadata to check for delete marker.</param>
        /// <param name="ctx">S3 request context for setting response headers.</param>
        /// <exception cref="ArgumentNullException">Thrown when obj or ctx is null.</exception>
        /// <exception cref="S3Exception">Thrown with NoSuchKey when object has a delete marker. Response headers will include delete marker indicator.</exception>
        internal static void CheckDeleteMarker(Obj obj, S3Context ctx)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            if (obj.DeleteMarker)
            {
                ctx.Response.Headers.Add(Constants.Headers.DeleteMarker, "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }
        }

        /// <summary>
        /// Parses and validates the upload ID from the S3 request context.
        /// </summary>
        /// <param name="ctx">S3 request context containing upload ID string.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when ctx or logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with InvalidRequest when upload ID is null or empty.</exception>
        internal static void ValidateUploadId(S3Context ctx, LoggingModule logging, string header)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (String.IsNullOrEmpty(ctx.Request.UploadId))
            {
                logging.Warn(header + "upload ID not supplied");
                throw new S3Exception(new Error(ErrorCode.InvalidRequest));
            }
        }

        /// <summary>
        /// Validates that a multipart upload record exists and has not expired.
        /// </summary>
        /// <param name="upload">Upload record from database. Null indicates upload does not exist.</param>
        /// <param name="uploadId">Upload ID being validated. Used for error logging.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with NoSuchUpload when upload is null or has expired.</exception>
        internal static void ValidateUpload(Less3.Classes.Upload upload, string uploadId, LoggingModule logging, string header)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (upload == null)
            {
                logging.Warn(header + "upload " + uploadId + " not found");
                throw new S3Exception(new Error(ErrorCode.NoSuchUpload));
            }

            if (upload.ExpirationUtc < DateTime.UtcNow)
            {
                logging.Warn(header + "upload " + uploadId + " expired");
                throw new S3Exception(new Error(ErrorCode.NoSuchUpload));
            }
        }

        /// <summary>
        /// Validates that a part number is within valid range for multipart uploads.
        /// Valid range is 1 to 10000 inclusive.
        /// </summary>
        /// <param name="partNumber">Part number to validate.</param>
        /// <param name="logging">Logging module for error reporting.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <exception cref="ArgumentNullException">Thrown when logging is null.</exception>
        /// <exception cref="S3Exception">Thrown with InvalidArgument when part number is less than 1 or greater than 10000.</exception>
        internal static void ValidatePartNumber(int partNumber, LoggingModule logging, string header)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            if (partNumber < 1 || partNumber > 10000)
            {
                logging.Warn(header + "invalid part number " + partNumber);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
