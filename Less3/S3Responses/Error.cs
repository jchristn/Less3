using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{ 
    [XmlRoot(ElementName = "Error", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class Error
    {
        [XmlElement(ElementName = "Key", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Key { get; set; }
        [XmlElement(ElementName = "VersionId", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string VersionId { get; set; }
        [XmlElement(ElementName = "RequestId", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string RequestId { get; set; }
        [XmlElement(ElementName = "Resource", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Resource { get; set; }
        [XmlElement(ElementName = "Code", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public ErrorCode Code { get; set; }
        [XmlElement(ElementName = "Message", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Message { get; set; }
        [XmlElement(ElementName = "HttpStatusCode")]
        public int HttpStatusCode { get; set; }

        public Error()
        {

        }

        public Error(ErrorCode error)
        {
            switch (error)
            {
                case ErrorCode.AccessDenied:
                    Message = "Access denied.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.AccountProblem:
                    Message = "There is a problem with your AWS account that prevents the operation from completing successfully.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.AllAccessDisabled:
                    Message = "All access to this Amazon S3 resource has been disabled.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.AmbiguousGrantByEmailAddress:
                    Message = "The email address you provided is associated with more than one account.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.AuthorizationHeaderMalformed:
                    Message = "The authorization header you provided is invalid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.BadDigest:
                    Message = "The Content-MD5 you specified did not match what we received.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.BucketAlreadyExists:
                    Message = "The requested bucket name is not available. The bucket namespace is shared by all users of the system. Please select a different name and try again.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.BucketAlreadyOwnedByYou:
                    Message = "The bucket you tried to create already exists, and you own it.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.BucketNotEmpty:
                    Message = "The bucket you tried to delete is not empty.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.CredentialsNotSupported:
                    Message = "This request does not support credentials.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.CrossLocationLoggingProhibited:
                    Message = "Cross-location logging not allowed. Buckets in one geographic location cannot log information to a bucket in another location.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.EntityTooSmall:
                    Message = "Your proposed upload is smaller than the minimum allowed object size.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.EntityTooLarge:
                    Message = "Your proposed upload exceeds the maximum allowed object size.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.ExpiredToken:
                    Message = "The provided token has expired.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.IllegalVersioningConfigurationException:
                    Message = "Indicates that the versioning configuration specified in the request is invalid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.IncompleteBody:
                    Message = "You did not provide the number of bytes specified by the Content-Length HTTP header.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.IncorrectNumberOfFilesInPostRequest:
                    Message = "POST requires exactly one file upload per request.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InlineDataTooLarge:
                    Message = "Inline data exceeds the maximum allowed size.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InternalError:
                    Message = "We encountered an internal error. Please try again.";
                    HttpStatusCode = 500;
                    break;
                case ErrorCode.InvalidAccessKeyId:
                    Message = "The AWS access key ID you provided does not exist in our records.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.InvalidAddressingHeader:
                    Message = "You must specify the Anonymous role.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidArgument:
                    Message = "Invalid Argument.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidBucketName:
                    Message = "The specified bucket is not valid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidBucketState:
                    Message = "The request is not valid with the current state of the bucket.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.InvalidDigest:
                    Message = "The Content-MD5 you specified is not valid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidEncryptionAlgorithmError:
                    Message = "The encryption request you specified is not valid. The valid value is AES256.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidLocationConstraint:
                    Message = "The specified location constraint is not valid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidObjectState:
                    Message = "The operation is not valid for the current state of the object.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.InvalidPart:
                    Message = "One or more of the specified parts could not be found. The part might not have been uploaded, or the specified entity tag might not have matched the part's entity tag.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidPartOrder:
                    Message = "The list of parts was not in ascending order. Parts list must be specified in order by part number.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidPayer:
                    Message = "All access to this object has been disabled.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.InvalidPolicyDocument:
                    Message = "The content of the form does not meet the conditions specified in the policy document.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidRange:
                    Message = "The requested range cannot be satisfied.";
                    HttpStatusCode = 416;
                    break;
                case ErrorCode.InvalidRequest:
                    Message = "Your request is invalid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidSecurity:
                    Message = "The provided security credentials are not valid.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.InvalidSOAPRequest:
                    Message = "The SOAP request body is invalid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidStorageClass:
                    Message = "The storage class you specified is not valid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidTargetBucketForLogging:
                    Message = "The target bucket for logging does not exist, is not owned by you, or does not have the appropriate grants for the log-delivery group.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidToken:
                    Message = "The provided token is malformed or otherwise invalid.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.InvalidURI:
                    Message = "Couldn't parse the specified URI.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.KeyTooLongError:
                    Message = "Your key is too long.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MalformedACLError:
                    Message = "The XML you provided was not well-formed or did not validate against our published schema.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MalformedPOSTRequest:
                    Message = "The body of your POST request is not well-formed multipart/form-data.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MalformedXML:
                    Message = "The XML you provided was not well-formed or did not validate against our published schema.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MaxMessageLengthExceeded:
                    Message = "Your request was too big.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MaxPostPreDataLengthExceededError:
                    Message = "Your POST request fields preceding the upload file were too large.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MetadataTooLarge:
                    Message = "Your metadata headers exceed the maximum allowed metadata size.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MethodNotAllowed:
                    Message = "The specified method is not allowed against this resource.";
                    HttpStatusCode = 405;
                    break;
                case ErrorCode.MissingAttachment:
                    Message = "A SOAP attachment was expected, but none were found.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MissingContentLength:
                    Message = "You must provide the Content-Length HTTP header.";
                    HttpStatusCode = 411;
                    break;
                case ErrorCode.MissingRequestBodyError:
                    Message = "Request body is empty.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MissingSecurityElement:
                    Message = "The SOAP 1.1 request is missing a security element.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.MissingSecurityHeader:
                    Message = "Your request is missing a required header.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.NoLoggingStatusForKey:
                    Message = "There is no such thing as a logging status subresource for a key.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.NoSuchBucket:
                    Message = "The specified bucket does not exist.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NoSuchBucketPolicy:
                    Message = "The specified bucket does not have a bucket policy.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NoSuchKey:
                    Message = "The specified key does not exist.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NoSuchLifecycleConfiguration:
                    Message = "The lifecycle configuration does not exist.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NoSuchUpload:
                    Message = "The specified multipart upload does not exist. The upload ID might be invalid, or the multipart upload might have been aborted or completed.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NoSuchVersion:
                    Message = "The version ID specified in the request does not match an existing version.";
                    HttpStatusCode = 404;
                    break;
                case ErrorCode.NotImplemented:
                    Message = "A header you provided implies functionality that is not implemented.";
                    HttpStatusCode = 501;
                    break;
                case ErrorCode.NotSignedUp:
                    Message = "Your account is not signed up for the Amazon S3 service.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.OperationAborted:
                    Message = "A conflicting conditional operation is currently in progress against this resource. Try again.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.PermanentRedirect:
                    Message = "The bucket you are attempting to access must be addressed using the specified endpoint. Send all future requests to this endpoint.";
                    HttpStatusCode = 301;
                    break;
                case ErrorCode.PreconditionFailed:
                    Message = "At least one of the preconditions you specified did not hold.";
                    HttpStatusCode = 412;
                    break;
                case ErrorCode.Redirect:
                    Message = "Temporary redirect.";
                    HttpStatusCode = 307;
                    break;
                case ErrorCode.RestoreAlreadyInProgress:
                    Message = "Object restore is already in progress.";
                    HttpStatusCode = 409;
                    break;
                case ErrorCode.RequestIsNotMultiPartContent:
                    Message = "Bucket POST must be of the enclosure-type multipart/form-data.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.RequestTimeout:
                    Message = "Your socket connection to the server was not read from or written to within the timeout period.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.RequestTimeTooSkewed:
                    Message = "The difference between the request time and the server's time is too large.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.RequestTorrentOfBucketError:
                    Message = "Requesting the torrent file of a bucket is not permitted.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.SignatureDoesNotMatch:
                    Message = "The request signature we calculated does not match the signature you provided.";
                    HttpStatusCode = 403;
                    break;
                case ErrorCode.ServiceUnavailable:
                    Message = "Reduce your request rate.";
                    HttpStatusCode = 503;
                    break;
                case ErrorCode.SlowDown:
                    Message = "Reduce your request rate.";
                    HttpStatusCode = 503;
                    break;
                case ErrorCode.TemporaryRedirect:
                    Message = "You are being redirected to the bucket while DNS updates.";
                    HttpStatusCode = 307;
                    break;
                case ErrorCode.TokenRefreshRequired:
                    Message = "The provided token must be refreshed.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.TooManyBuckets:
                    Message = "You have attempted to create more buckets than allowed.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.UnexpectedContent:
                    Message = "This request does not support content.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.UnresolvableGrantByEmailAddress:
                    Message = "The email address you provided does not match any account on record.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.UserKeyMustBeSpecified:
                    Message = "The bucket POST must contain the specified field name. If it is specified, check the order of the fields.";
                    HttpStatusCode = 400;
                    break;
                case ErrorCode.ServerSideEncryptionConfigurationNotFoundError:
                    Message = "The server side encryption configuration was not found.";
                    HttpStatusCode = 400;
                    break;
            }
        }
    } 
}
