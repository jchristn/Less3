using System;
using System.Collections.Generic;
using System.Text;

namespace Less3.Classes
{
    internal static class Constants
    {
        // http://loveascii.com/hearts.html
        // http://patorjk.com/software/taag/#p=display&f=Small&t=less3 
        internal static string Logo = 
                @"  ,d88b.d88b,     _           ____  " + Environment.NewLine +
                @"  88888888888    | |___ _____|__ /  " + Environment.NewLine +
                @"  `Y8888888Y'    | / -_|_-<_-<|_ \  " + Environment.NewLine +
                @"    `Y888Y'      |_\___/__/__/___/  " + Environment.NewLine +
                @"      `Y'       " + Environment.NewLine;

        internal static class Headers
        {
            internal static string RequestType = "X-Request-Type";
            internal static string AuthenticationResult = "X-Authentication-Result";
            internal static string AuthorizedBy = "X-Authorized-By";

            internal static string DeleteMarker = "X-Amz-Delete-Marker";
            internal static string AccessControlList = "X-Amz-Acl";
            internal static string AclGrantRead = "X-Amz-Grant-Read";
            internal static string AclGrantWrite = "X-Amz-Grant-Write";
            internal static string AclGrantReadAcp = "X-Amz-Grant-Read-Acp";
            internal static string AclGrantWriteAcp = "X-Amz-Grant-Write-Acp";
            internal static string AclGrantFullControl = "X-Amz-Grant-Full-Control";
        }
    }
}
