using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{
    // see https://docs.aws.amazon.com/AmazonS3/latest/API/ErrorResponses.html#ErrorCodeList

    public enum ErrorCode
    {
        [XmlEnum(Name = "AccessDenied")]
        AccessDenied,
        [XmlEnum(Name = "AccountProblem")]
        AccountProblem,
        [XmlEnum(Name = "AllAccessDisabled")]
        AllAccessDisabled,
        [XmlEnum(Name = "AmbiguousGrantByEmailAddress")]
        AmbiguousGrantByEmailAddress,
        [XmlEnum(Name = "AuthorizationHeaderMalformed")]
        AuthorizationHeaderMalformed,
        [XmlEnum(Name = "BadDigest")]
        BadDigest,
        [XmlEnum(Name = "BucketAlreadyExists")]
        BucketAlreadyExists,
        [XmlEnum(Name = "BucketAlreadyOwnedByYou")]
        BucketAlreadyOwnedByYou,
        [XmlEnum(Name = "BucketNotEmpty")]
        BucketNotEmpty,
        [XmlEnum(Name = "CredentialsNotSupported")]
        CredentialsNotSupported,
        [XmlEnum(Name = "CrossLocationLoggingProhibited")]
        CrossLocationLoggingProhibited,
        [XmlEnum(Name = "EntityTooSmall")]
        EntityTooSmall,
        [XmlEnum(Name = "EntityTooLarge")]
        EntityTooLarge,
        [XmlEnum(Name = "ExpiredToken")]
        ExpiredToken,
        [XmlEnum(Name = "IllegalVersioningConfigurationException")]
        IllegalVersioningConfigurationException,
        [XmlEnum(Name = "IncompleteBody")]
        IncompleteBody,
        [XmlEnum(Name = "IncorrectNumberOfFilesInPostRequest")]
        IncorrectNumberOfFilesInPostRequest,
        [XmlEnum(Name = "InlineDataTooLarge")]
        InlineDataTooLarge,
        [XmlEnum(Name = "InternalError")]
        InternalError,
        [XmlEnum(Name = "InvalidAccessKeyId")]
        InvalidAccessKeyId,
        [XmlEnum(Name = "InvalidAddressingHeader")]
        InvalidAddressingHeader,
        [XmlEnum(Name = "InvalidArgument")]
        InvalidArgument,
        [XmlEnum(Name = "InvalidBucketName")]
        InvalidBucketName,
        [XmlEnum(Name = "InvalidBucketState")]
        InvalidBucketState,
        [XmlEnum(Name = "InvalidDigest")]
        InvalidDigest,
        [XmlEnum(Name = "InvalidEncryptionAlgorithmError")]
        InvalidEncryptionAlgorithmError,
        [XmlEnum(Name = "InvalidLocationConstraint")]
        InvalidLocationConstraint,
        [XmlEnum(Name = "InvalidObjectState")]
        InvalidObjectState,
        [XmlEnum(Name = "InvalidPart")]
        InvalidPart,
        [XmlEnum(Name = "InvalidPartOrder")]
        InvalidPartOrder,
        [XmlEnum(Name = "InvalidPayer")]
        InvalidPayer,
        [XmlEnum(Name = "InvalidPolicyDocument")]
        InvalidPolicyDocument,
        [XmlEnum(Name = "InvalidRange")]
        InvalidRange,
        [XmlEnum(Name = "InvalidRequest")]
        InvalidRequest,
        [XmlEnum(Name = "InvalidSecurity")]
        InvalidSecurity,
        [XmlEnum(Name = "InvalidSOAPRequest")]
        InvalidSOAPRequest,
        [XmlEnum(Name = "InvalidStorageClass")]
        InvalidStorageClass,
        [XmlEnum(Name = "InvalidTargetBucketForLogging")]
        InvalidTargetBucketForLogging,
        [XmlEnum(Name = "InvalidToken")]
        InvalidToken,
        [XmlEnum(Name = "InvalidURI")]
        InvalidURI,
        [XmlEnum(Name = "KeyTooLongError")]
        KeyTooLongError,
        [XmlEnum(Name = "MalformedACLError")]
        MalformedACLError,
        [XmlEnum(Name = "MalformedPOSTRequest")]
        MalformedPOSTRequest,
        [XmlEnum(Name = "MalformedXML")]
        MalformedXML,
        [XmlEnum(Name = "MaxMessageLengthExceeded")]
        MaxMessageLengthExceeded,
        [XmlEnum(Name = "MaxPostPreDataLengthExceededError")]
        MaxPostPreDataLengthExceededError,
        [XmlEnum(Name = "MetadataTooLarge")]
        MetadataTooLarge,
        [XmlEnum(Name = "MethodNotAllowed")]
        MethodNotAllowed,
        [XmlEnum(Name = "MissingAttachment")]
        MissingAttachment,
        [XmlEnum(Name = "MissingContentLength")]
        MissingContentLength,
        [XmlEnum(Name = "MissingRequestBodyError")]
        MissingRequestBodyError,
        [XmlEnum(Name = "MissingSecurityElement")]
        MissingSecurityElement,
        [XmlEnum(Name = "MissingSecurityHeader")]
        MissingSecurityHeader,
        [XmlEnum(Name = "NoLoggingStatusForKey")]
        NoLoggingStatusForKey,
        [XmlEnum(Name = "NoSuchBucket")]
        NoSuchBucket,
        [XmlEnum(Name = "NoSuchBucketPolicy")]
        NoSuchBucketPolicy,
        [XmlEnum(Name = "NoSuchKey")]
        NoSuchKey,
        [XmlEnum(Name = "NoSuchLifecycleConfiguration")]
        NoSuchLifecycleConfiguration,
        [XmlEnum(Name = "NoSuchUpload")]
        NoSuchUpload,
        [XmlEnum(Name = "NoSuchVersion")]
        NoSuchVersion,
        [XmlEnum(Name = "NotImplemented")]
        NotImplemented,
        [XmlEnum(Name = "NotSignedUp")]
        NotSignedUp,
        [XmlEnum(Name = "OperationAborted")]
        OperationAborted,
        [XmlEnum(Name = "PermanentRedirect")]
        PermanentRedirect,
        [XmlEnum(Name = "PreconditionFailed")]
        PreconditionFailed,
        [XmlEnum(Name = "Redirect")]
        Redirect, 
        [XmlEnum(Name = "RestoreAlreadyInProgress")]
        RestoreAlreadyInProgress,
        [XmlEnum(Name = "RequestIsNotMultiPartContent")]
        RequestIsNotMultiPartContent,
        [XmlEnum(Name = "RequestTimeout")]
        RequestTimeout,
        [XmlEnum(Name = "RequestTimeTooSkewed")]
        RequestTimeTooSkewed,
        [XmlEnum(Name = "RequestTorrentOfBucketError")]
        RequestTorrentOfBucketError,
        [XmlEnum(Name = "SignatureDoesNotMatch")]
        SignatureDoesNotMatch,
        [XmlEnum(Name = "ServiceUnavailable")]
        ServiceUnavailable,
        [XmlEnum(Name = "SlowDown")]
        SlowDown,
        [XmlEnum(Name = "TemporaryRedirect")]
        TemporaryRedirect,
        [XmlEnum(Name = "TokenRefreshRequired")]
        TokenRefreshRequired,
        [XmlEnum(Name = "TooManyBuckets")]
        TooManyBuckets,
        [XmlEnum(Name = "UnexpectedContent")]
        UnexpectedContent,
        [XmlEnum(Name = "UnresolvableGrantByEmailAddress")]
        UnresolvableGrantByEmailAddress,
        [XmlEnum(Name = "UserKeyMustBeSpecified")]
        UserKeyMustBeSpecified,
        [XmlEnum(Name = "ServerSideEncryptionConfigurationNotFoundError")]
        ServerSideEncryptionConfigurationNotFoundError
    }
}
