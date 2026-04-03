namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class UploadPartQueries
    {
        internal static string InsertQuery(UploadPart part)
        {
            return "INSERT INTO uploadparts (guid, bucketguid, ownerguid, uploadguid, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(part.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(part.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(part.OwnerGUID) + "', "
                + "'" + Sanitizer.SanitizeString(part.UploadGUID) + "', "
                + part.PartNumber + ", "
                + part.PartLength + ", "
                + "'" + Sanitizer.SanitizeString(part.MD5Hash) + "', "
                + "'" + Sanitizer.SanitizeString(part.Sha1Hash) + "', "
                + "'" + Sanitizer.SanitizeString(part.Sha256Hash) + "', "
                + "'" + part.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + part.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByUploadGuid(string uploadGuid)
        {
            return "SELECT * FROM uploadparts WHERE uploadguid = '" + Sanitizer.SanitizeString(uploadGuid) + "';";
        }

        internal static string DeleteByUploadGuid(string uploadGuid)
        {
            return "DELETE FROM uploadparts WHERE uploadguid = '" + Sanitizer.SanitizeString(uploadGuid) + "';";
        }
    }
}
