@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo Less3 Comprehensive API Test Suite
echo ===============================================================================
echo.
echo Optional environment variables:
echo   LESS3_ENDPOINT                         Less3 S3 endpoint (default http://localhost:8000)
echo   AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY  credentials; when set, 'aws configure' is skipped
echo.

if defined LESS3_ENDPOINT (set ENDPOINT=%LESS3_ENDPOINT%) else (set ENDPOINT=http://localhost:8000)
if not defined AWS_DEFAULT_REGION set AWS_DEFAULT_REGION=us-west-1
set TEST_BUCKET=test-bucket
set COMPAT_DEL=test-compat-delete
set COMPAT_VER=test-compat-versions
set COMPAT_LIST=test-compat-list
set COMPAT_OBJ=test-compat-objects
set OUT=%TEMP%\less3-awscli-out.tmp
set ERR=%TEMP%\less3-awscli-err.tmp

if not defined AWS_ACCESS_KEY_ID (
    echo Configuring AWS CLI...
    aws configure
    echo.
)

echo ===============================================================================
echo SERVICE OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Listing buckets...
aws --endpoint-url %ENDPOINT% s3 ls s3://
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List buckets failed
    goto :error
)
echo [PASS] List buckets succeeded
echo.

echo ===============================================================================
echo BUCKET OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Cleaning up any pre-existing test buckets...
for %%b in (%TEST_BUCKET% %COMPAT_DEL% %COMPAT_VER% %COMPAT_LIST% %COMPAT_OBJ%) do call :purge_bucket %%b
echo [INFO] Pre-test cleanup attempted (ignoring errors if buckets didn't exist)
echo.

echo [TEST] Creating test bucket...
aws --endpoint-url %ENDPOINT% s3 mb s3://%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Create bucket failed
    goto :error
)
echo [PASS] Create bucket succeeded
echo.

echo [TEST] Checking if bucket exists...
aws s3api head-bucket --endpoint %ENDPOINT% --bucket %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Head bucket failed
    goto :error
)
echo [PASS] Head bucket succeeded
echo.

echo [TEST] Listing buckets to verify...
aws --endpoint-url %ENDPOINT% s3 ls s3:// | findstr %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Bucket not found in list
    goto :error
)
echo [PASS] Bucket found in list
echo.

echo [TEST] Re-creating a bucket you own returns BucketAlreadyOwnedByYou...
aws s3api create-bucket --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% >nul 2>"%ERR%"
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] Re-creating an existing bucket unexpectedly succeeded
    goto :error
)
findstr /C:"BucketAlreadyOwnedByYou" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected BucketAlreadyOwnedByYou
    type "%ERR%"
    goto :error
)
echo [PASS] BucketAlreadyOwnedByYou returned
echo.

echo ===============================================================================
echo OBJECT OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Creating test file...
echo Hello from Less3 API test! > test-file.txt
echo [PASS] Test file created
echo.

echo [TEST] Uploading object...
aws --endpoint-url %ENDPOINT% s3 cp test-file.txt s3://%TEST_BUCKET%/test-file.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload object failed
    goto :error
)
echo [PASS] Upload object succeeded
echo.

echo [TEST] Checking if object exists...
aws s3api head-object --endpoint %ENDPOINT% --bucket %TEST_BUCKET% --key test-file.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Head object failed
    goto :error
)
echo [PASS] Head object succeeded
echo.

echo [TEST] Downloading object...
aws --endpoint-url %ENDPOINT% s3 cp s3://%TEST_BUCKET%/test-file.txt test-file-downloaded.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Download object failed
    goto :error
)
echo [PASS] Download object succeeded
echo.

echo [TEST] Verifying downloaded content...
fc test-file.txt test-file-downloaded.txt >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Downloaded content does not match
    goto :error
)
echo [PASS] Downloaded content verified
echo.

echo [TEST] Listing objects...
aws --endpoint-url %ENDPOINT% s3 ls s3://%TEST_BUCKET%/
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List objects failed
    goto :error
)
echo [PASS] List objects succeeded
echo.

echo [TEST] Getting object with range read...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-file.txt --range bytes=0-4 test-range.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Range read failed
    goto :error
)
echo [PASS] Range read succeeded
echo.

echo [TEST] Deleting object...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-file.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete object failed
    goto :error
)
echo [PASS] Delete object succeeded
echo.

echo ===============================================================================
echo MULTIPART UPLOAD TESTS
echo ===============================================================================
echo.

echo [TEST] Creating large test file (15 MB)...
fsutil file createnew multipart-auto.dat 15728640 >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Create large file failed
    goto :error
)
echo [PASS] Large file created
echo.

echo [TEST] Uploading large file with multipart upload...
aws s3 cp multipart-auto.dat s3://%TEST_BUCKET%/multipart-auto.dat --endpoint-url %ENDPOINT%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Multipart upload failed
    goto :error
)
echo [PASS] Multipart upload succeeded
echo.

echo [TEST] Verifying uploaded large file exists...
aws s3api head-object --endpoint %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-auto.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Large file head-object failed
    goto :error
)
echo [PASS] Large file verified
echo.

echo [TEST] Deleting large file...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/multipart-auto.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete large file failed
    goto :error
)
echo [PASS] Delete large file succeeded
echo.

echo [TEST] Manual multipart upload - Initiate...
aws s3api create-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --query UploadId --output text > "%OUT%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Initiate multipart upload failed
    goto :error
)
set /p UPLOAD_ID=<"%OUT%"
echo [PASS] Initiated multipart upload: !UPLOAD_ID!
echo.

echo [TEST] Creating part files...
fsutil file createnew part1.dat 5242880 >nul
fsutil file createnew part2.dat 5242880 >nul
echo [PASS] Part files created
echo.

echo [TEST] Uploading part 1...
aws s3api upload-part --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --part-number 1 --upload-id !UPLOAD_ID! --body part1.dat --query ETag --output text > "%OUT%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload part 1 failed
    goto :error
)
set /p ETAG1=<"%OUT%"
echo [PASS] Uploaded part 1: !ETAG1!
echo.

echo [TEST] Uploading part 2...
aws s3api upload-part --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --part-number 2 --upload-id !UPLOAD_ID! --body part2.dat --query ETag --output text > "%OUT%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload part 2 failed
    goto :error
)
set /p ETAG2=<"%OUT%"
echo [PASS] Uploaded part 2: !ETAG2!
echo.

echo [TEST] Listing parts one at a time (max-parts pagination)...
aws s3api list-parts --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --upload-id !UPLOAD_ID! --no-paginate --max-parts 1 --query "join(',', [to_string(length(Parts)), to_string(IsTruncated)])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="1,true" (
    echo [FAIL] list-parts with max-parts 1 returned [!VAL!], expected [1,true]
    goto :error
)
aws s3api list-parts --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --upload-id !UPLOAD_ID! --page-size 1 --query "join(',', Parts[].to_string(PartNumber))" --output json > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
set VAL=!VAL:"=!
if not "!VAL!"=="1,2" (
    echo [FAIL] Paging through parts returned [!VAL!], expected [1,2]
    goto :error
)
echo [PASS] List parts paginates
echo.

echo [TEST] Creating multipart completion JSON...
(echo {"Parts":[{"ETag":!ETAG1!,"PartNumber":1},{"ETag":!ETAG2!,"PartNumber":2}]}) > complete-multipart.json
echo [PASS] Completion JSON created
echo.

echo [TEST] Completing multipart upload...
aws s3api complete-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --upload-id !UPLOAD_ID! --multipart-upload file://complete-multipart.json
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Complete multipart upload failed
    goto :error
)
echo [PASS] Multipart upload completed
echo.

echo [TEST] Verifying completed multipart object...
aws s3api head-object --endpoint %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Multipart object verification failed
    goto :error
)
echo [PASS] Multipart object verified
echo.

echo [TEST] Deleting multipart object...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/multipart-manual.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete multipart object failed
    goto :error
)
echo [PASS] Multipart object deleted
echo.

echo [TEST] Testing abort multipart upload...
aws s3api create-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-abort.dat --query UploadId --output text > "%OUT%"
set /p ABORT_UPLOAD_ID=<"%OUT%"
aws s3api upload-part --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-abort.dat --part-number 1 --upload-id !ABORT_UPLOAD_ID! --body part1.dat > nul
aws s3api abort-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-abort.dat --upload-id !ABORT_UPLOAD_ID!
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Abort multipart upload failed
    goto :error
)
echo [PASS] Abort multipart upload succeeded
echo.

echo ===============================================================================
echo ACL OPERATIONS TESTS
echo ===============================================================================
echo.

echo Hello from Less3 API test! > test-file.txt

echo [TEST] Uploading object with public-read ACL...
aws --endpoint-url %ENDPOINT% s3 cp test-file.txt s3://%TEST_BUCKET%/test-acl.txt --acl public-read
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload with ACL failed
    goto :error
)
echo [PASS] Upload with ACL succeeded
echo.

echo [TEST] Getting object ACL...
aws s3api get-object-acl --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-acl.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get object ACL failed
    goto :error
)
echo [PASS] Get object ACL succeeded
echo.

echo [TEST] Setting object ACL to private...
aws s3api put-object-acl --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-acl.txt --acl private
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Put object ACL failed
    goto :error
)
echo [PASS] Put object ACL succeeded
echo.

echo [TEST] Getting bucket ACL...
aws s3api get-bucket-acl --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get bucket ACL failed
    goto :error
)
echo [PASS] Get bucket ACL succeeded
echo.

echo [TEST] Uploading with the bucket-owner-full-control canned ACL...
aws --endpoint-url %ENDPOINT% s3 cp test-file.txt s3://%TEST_BUCKET%/test-acl-owner.txt --acl bucket-owner-full-control
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] bucket-owner-full-control was rejected
    goto :error
)
echo [PASS] bucket-owner-full-control accepted
echo.

echo [TEST] Deleting ACL test objects...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-acl.txt
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-acl-owner.txt
echo [PASS] ACL test objects deleted
echo.

echo ===============================================================================
echo TAGGING OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Uploading object for tagging...
aws --endpoint-url %ENDPOINT% s3 cp test-file.txt s3://%TEST_BUCKET%/test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload object failed
    goto :error
)
echo [PASS] Object uploaded
echo.

echo [TEST] Putting object tags...
aws s3api put-object-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-tag.txt --tagging "TagSet=[{Key=Environment,Value=Test},{Key=Application,Value=Less3}]"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Put object tagging failed
    goto :error
)
echo [PASS] Put object tagging succeeded
echo.

echo [TEST] Getting object tags...
aws s3api get-object-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get object tagging failed
    goto :error
)
echo [PASS] Get object tagging succeeded
echo.

echo [TEST] Deleting object tags...
aws s3api delete-object-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete object tagging failed
    goto :error
)
echo [PASS] Delete object tagging succeeded
echo.

echo [TEST] Putting bucket tags...
aws s3api put-bucket-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --tagging "TagSet=[{Key=Project,Value=Less3},{Key=Owner,Value=TestUser}]"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Put bucket tagging failed
    goto :error
)
echo [PASS] Put bucket tagging succeeded
echo.

echo [TEST] Getting bucket tags...
aws s3api get-bucket-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get bucket tagging failed
    goto :error
)
echo [PASS] Get bucket tagging succeeded
echo.

echo [TEST] Deleting bucket tags...
aws s3api delete-bucket-tagging --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete bucket tagging failed
    goto :error
)
echo [PASS] Delete bucket tagging succeeded
echo.

echo [TEST] Deleting tagging test object...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-tag.txt
echo [PASS] Tagging test object deleted
echo.

echo ===============================================================================
echo VERSIONING OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Enabling versioning on bucket...
aws s3api put-bucket-versioning --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --versioning-configuration Status=Enabled
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Enable versioning failed
    goto :error
)
echo [PASS] Versioning enabled
echo.

echo [TEST] Getting bucket versioning status...
aws s3api get-bucket-versioning --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --query Status --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="Enabled" (
    echo [FAIL] Versioning status is [!VAL!], expected [Enabled]
    goto :error
)
echo [PASS] Get bucket versioning succeeded
echo.

echo [TEST] Uploading version 1...
echo Version 1 content > test-version.txt
aws --endpoint-url %ENDPOINT% s3 cp test-version.txt s3://%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 1 failed
    goto :error
)
echo [PASS] Version 1 uploaded
echo.

echo [TEST] Uploading version 2...
echo Version 2 content > test-version.txt
aws --endpoint-url %ENDPOINT% s3 cp test-version.txt s3://%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 2 failed
    goto :error
)
echo [PASS] Version 2 uploaded
echo.

echo [TEST] Uploading version 3...
echo Version 3 content > test-version.txt
aws --endpoint-url %ENDPOINT% s3 cp test-version.txt s3://%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 3 failed
    goto :error
)
echo [PASS] Version 3 uploaded
echo.

echo [TEST] Listing object versions...
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --prefix test-version.txt --query "length(Versions)" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="3" (
    echo [FAIL] Expected 3 versions, found [!VAL!]
    goto :error
)
echo [PASS] List object versions succeeded
echo.

echo [TEST] Deleting versioned object (creates delete marker)...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete versioned object failed
    goto :error
)
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --prefix test-version.txt --query "join(',', [to_string(length(Versions)), to_string(length(DeleteMarkers))])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="3,1" (
    echo [FAIL] Expected 3 versions and 1 delete marker, found [!VAL!]
    goto :error
)
echo [PASS] Versioned object deleted (delete marker created, versions kept)
echo.

echo ===============================================================================
echo S3 COMPATIBILITY TESTS
echo ===============================================================================
echo.

aws --endpoint-url %ENDPOINT% s3 mb s3://%COMPAT_DEL% >nul
aws --endpoint-url %ENDPOINT% s3 mb s3://%COMPAT_VER% >nul
aws --endpoint-url %ENDPOINT% s3 mb s3://%COMPAT_LIST% >nul
aws --endpoint-url %ENDPOINT% s3 mb s3://%COMPAT_OBJ% >nul

rem ---------------------------------------------------------------- DeleteObjects
echo [TEST] DeleteObjects reports a missing key as deleted, not NoSuchKey...
echo exists> exists.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --key exists.txt --body exists.txt >nul
aws s3api delete-objects --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --delete "Objects=[{Key=exists.txt},{Key=missing.txt}],Quiet=false" --query "join(',', [to_string(length(Deleted)), to_string(Errors)])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="2,null" (
    echo [FAIL] Expected 2 deleted and no errors, got [!VAL!]
    goto :error
)
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --key exists.txt >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] exists.txt still exists after DeleteObjects
    goto :error
)
echo [PASS] Missing key reported as deleted; existing key removed
echo.

echo [TEST] DeleteObjects in Quiet mode returns no Deleted entries...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --key quiet.txt --body exists.txt >nul
aws s3api delete-objects --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --delete "Objects=[{Key=quiet.txt}],Quiet=true" --query "Deleted" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="None" (
    echo [FAIL] Quiet mode returned Deleted entries: [!VAL!]
    goto :error
)
echo [PASS] Quiet mode honored
echo.

echo [TEST] DeleteObjects with an invalid VersionId fails only that key...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --key after.txt --body exists.txt >nul
aws s3api delete-objects --endpoint-url %ENDPOINT% --bucket %COMPAT_DEL% --delete "Objects=[{Key=bad.txt,VersionId=not-a-version},{Key=after.txt}]" --query "join(',', [Errors[0].Code, Deleted[0].Key])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="NoSuchVersion,after.txt" (
    echo [FAIL] Expected [NoSuchVersion,after.txt], got [!VAL!]
    goto :error
)
echo [PASS] Per-key error; remaining keys processed
echo.

rem ---------------------------------------------------------------- Versioning
echo [TEST] Versioned delete hides the latest version and keeps older versions...
aws s3api put-bucket-versioning --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --versioning-configuration Status=Enabled
echo one> v.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --body v.txt --query VersionId --output text > "%OUT%"
set /p V1=<"%OUT%"
echo two> v.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --body v.txt --query VersionId --output text > "%OUT%"
set /p V2=<"%OUT%"
echo three> v.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --body v.txt >nul
aws s3api delete-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --query "join(',', [to_string(DeleteMarker), VersionId])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
for /f "tokens=1,2 delims=," %%a in ("!VAL!") do (set IS_MARKER=%%a& set MARKER=%%b)
if not "!IS_MARKER!"=="true" (
    echo [FAIL] DeleteObject did not create a delete marker: [!VAL!]
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc out.txt >nul 2>"%ERR%"
findstr /C:"NoSuchKey" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] GET of a deleted key did not return NoSuchKey
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id !V1! out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="one" (
    echo [FAIL] Version 1 content is [!VAL!]
    goto :error
)
echo [PASS] Delete marker hides the latest version; version 1 still readable
echo.

echo [TEST] GET of a delete marker's version returns 405 MethodNotAllowed...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id !MARKER! out.txt >nul 2>"%ERR%"
findstr /C:"MethodNotAllowed" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected MethodNotAllowed
    type "%ERR%"
    goto :error
)
echo [PASS] 405 returned for a delete marker version
echo.

echo [TEST] Deleting the delete marker restores the object...
aws s3api delete-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id !MARKER! >nul
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="three" (
    echo [FAIL] Restored content is [!VAL!], expected [three]
    goto :error
)
echo [PASS] Object restored
echo.

echo [TEST] Deleting a specific version removes it permanently...
aws s3api delete-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id !V2! >nul
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --query "join(',', [to_string(length(Versions)), to_string(DeleteMarkers)])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="2,null" (
    echo [FAIL] Expected 2 versions and no markers, got [!VAL!]
    goto :error
)
echo [PASS] Version permanently removed
echo.

echo [TEST] Invalid version ID returns InvalidArgument...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id not-a-version out.txt >nul 2>"%ERR%"
findstr /C:"InvalidArgument" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected InvalidArgument
    goto :error
)
echo [PASS] InvalidArgument returned
echo.

echo [TEST] Suspending versioning keeps existing versions (no data loss)...
aws s3api put-bucket-versioning --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --versioning-configuration Status=Suspended
aws s3api get-bucket-versioning --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --query Status --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="Suspended" (
    echo [FAIL] Versioning status is [!VAL!], expected [Suspended]
    goto :error
)
echo suspended> v.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --body v.txt --query VersionId --output text > "%OUT%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Write after suspending versioning failed
    goto :error
)
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="null" (
    echo [FAIL] Suspended write returned version [!VAL!], expected [null]
    goto :error
)
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --query "length(Versions)" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="3" (
    echo [FAIL] Expected the 2 earlier versions plus the null version, found [!VAL!]
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key doc --version-id !V1! out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="one" (
    echo [FAIL] Version 1 lost after suspending: [!VAL!]
    goto :error
)
echo [PASS] Suspended versioning keeps prior versions
echo.

echo [TEST] PutBucketVersioning with an invalid status is rejected...
aws s3api put-bucket-versioning --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --versioning-configuration Status=Disabled >nul 2>"%ERR%"
findstr /C:"MalformedXML" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected MalformedXML
    goto :error
)
echo [PASS] Invalid status rejected
echo.

echo [TEST] A bucket holding versions cannot be deleted...
aws s3api delete-bucket --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% >nul 2>"%ERR%"
findstr /C:"BucketNotEmpty" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected BucketNotEmpty
    goto :error
)
echo [PASS] BucketNotEmpty returned
echo.

rem ---------------------------------------------------------------- Listing
echo [TEST] Listing returns keys in byte order across pages...
for %%k in (b.txt B.txt a.txt _x.txt dir/y.txt dir/z.txt) do (
    echo %%k> k.txt
    aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --key %%k --body k.txt >nul
)
aws s3api list-objects-v2 --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --page-size 1 --query "join(' ', Contents[].Key)" --output json > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
set VAL=!VAL:"=!
if not "!VAL!"=="B.txt _x.txt a.txt b.txt dir/y.txt dir/z.txt" (
    echo [FAIL] ListObjectsV2 order is [!VAL!]
    goto :error
)
aws s3api list-objects --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --page-size 2 --query "join(' ', Contents[].Key)" --output json > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
set VAL=!VAL:"=!
if not "!VAL!"=="B.txt _x.txt a.txt b.txt dir/y.txt dir/z.txt" (
    echo [FAIL] ListObjects v1 order is [!VAL!]
    goto :error
)
echo [PASS] Keys listed once each, in byte order, for v1 and v2
echo.

echo [TEST] Delimiter, start-after and literal prefixes...
aws s3api list-objects-v2 --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --delimiter / --page-size 1 --query "join(' ', [Contents[].Key, CommonPrefixes[].Prefix][])" --output json > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
set VAL=!VAL:"=!
if not "!VAL!"=="B.txt _x.txt a.txt b.txt dir/" (
    echo [FAIL] Delimiter listing is [!VAL!]
    goto :error
)
aws s3api list-objects-v2 --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --start-after a.txt --query "join(' ', Contents[].Key)" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="b.txt dir/y.txt dir/z.txt" (
    echo [FAIL] start-after listing is [!VAL!]
    goto :error
)
aws s3api list-objects-v2 --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --prefix _ --query "join(' ', Contents[].Key)" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="_x.txt" (
    echo [FAIL] Prefix _ matched [!VAL!]
    goto :error
)
echo [PASS] Delimiter, start-after and prefix behave as in S3
echo.

echo [TEST] Keys differing only in case are distinct objects...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --key B.txt out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="B.txt" (
    echo [FAIL] B.txt content is [!VAL!]
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_LIST% --key b.txt out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="b.txt" (
    echo [FAIL] b.txt content is [!VAL!]
    goto :error
)
echo [PASS] Case-sensitive keys
echo.

rem ---------------------------------------------------------------- Ranges and conditions
<nul set /p =0123456789> ten.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --body ten.txt >nul

echo [TEST] Suffix range bytes=-3 returns the last 3 bytes...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --range bytes=-3 out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="789" (
    echo [FAIL] Suffix range returned [!VAL!]
    goto :error
)
echo [PASS] Suffix range
echo.

echo [TEST] A range past the end is clamped; a range starting past the end is 416...
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --range bytes=5-100 out.txt >nul
set VAL=
set /p VAL=<out.txt
if not "!VAL!"=="56789" (
    echo [FAIL] Clamped range returned [!VAL!]
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --range bytes=10- out.txt >nul 2>"%ERR%"
findstr /C:"InvalidRange" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected InvalidRange
    goto :error
)
echo [PASS] Range clamping and InvalidRange
echo.

echo [TEST] HEAD honors Range, and Content-Range describes the bytes returned...
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --range bytes=2-4 --query "join(',', [ContentRange, to_string(ContentLength)])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="bytes 2-4/10,3" (
    echo [FAIL] HEAD with Range returned [!VAL!]
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --range bytes=5-100 --query ContentRange --output text out.txt > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="bytes 5-9/10" (
    echo [FAIL] Content-Range for bytes=5-100 was [!VAL!]
    goto :error
)
echo [PASS] HEAD ranges and Content-Range
echo.

echo [TEST] On an empty object a range is 416 but a suffix range is 200 with no bytes...
type nul > empty.txt
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key empty.txt --body empty.txt >nul
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key empty.txt --range bytes=0-0 out.txt >nul 2>"%ERR%"
findstr /C:"InvalidRange" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected InvalidRange for bytes=0-0 on an empty object
    type "%ERR%"
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key empty.txt --range bytes=-1 out.txt >nul 2>"%ERR%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] A suffix range on an empty object failed
    type "%ERR%"
    goto :error
)
for %%f in (out.txt) do if %%~zf NEQ 0 (
    echo [FAIL] A suffix range on an empty object returned %%~zf bytes
    goto :error
)
echo [PASS] Empty-object ranges
echo.

echo [TEST] Keys with spaces and plus signs round-trip through both listings (encoding-type=url)...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key "enc dir/a b+c.txt" --body ten.txt >nul
aws s3api list-objects-v2 --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --prefix "enc dir/" --query "Contents[0].Key" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="enc dir/a b+c.txt" (
    echo [FAIL] ListObjectsV2 returned key [!VAL!]
    goto :error
)
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key "enc dir/a b+c.txt" --body ten.txt >nul
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --prefix "enc dir/" --query "Versions[0].Key" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="enc dir/a b+c.txt" (
    echo [FAIL] ListObjectVersions returned key [!VAL!]
    goto :error
)
echo [PASS] Encoded listings decode to the original keys
echo.

echo [TEST] Delete markers in a version listing carry no ETag or StorageClass...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key dm-shape.txt --body ten.txt >nul
aws s3api delete-object --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --key dm-shape.txt >nul
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %COMPAT_VER% --prefix dm-shape.txt --query "join(',', [to_string(DeleteMarkers[0].ETag), to_string(DeleteMarkers[0].StorageClass), to_string(DeleteMarkers[0].IsLatest)])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="null,null,true" (
    echo [FAIL] Delete marker fields were [!VAL!]
    goto :error
)
echo [PASS] Delete marker shape
echo.

echo [TEST] Conditional GET: If-None-Match returns 304, If-Match mismatch returns 412...
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --query ETag --output text > "%OUT%"
set /p ETAG=<"%OUT%"
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --if-none-match !ETAG! out.txt >nul 2>"%ERR%"
findstr /C:"304" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected 304 Not Modified
    type "%ERR%"
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --if-match 00000000000000000000000000000000 out.txt >nul 2>"%ERR%"
findstr /C:"PreconditionFailed" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected PreconditionFailed
    type "%ERR%"
    goto :error
)
aws s3api get-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --if-match !ETAG! out.txt >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] If-Match with the current ETag failed
    goto :error
)
echo [PASS] Conditional GET
echo.

echo [TEST] Conditional PUT: If-None-Match * refuses to overwrite...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key ten.txt --body exists.txt --if-none-match * >nul 2>"%ERR%"
findstr /C:"PreconditionFailed" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected PreconditionFailed
    type "%ERR%"
    goto :error
)
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key new-once.txt --body exists.txt --if-none-match * >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] If-None-Match * on a new key failed
    goto :error
)
echo [PASS] Conditional PUT
echo.

echo [TEST] A Content-MD5 that does not match the body is rejected...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key bad-md5.txt --body exists.txt --content-md5 AAAAAAAAAAAAAAAAAAAAAA== >nul 2>"%ERR%"
findstr /C:"BadDigest" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected BadDigest
    type "%ERR%"
    goto :error
)
echo [PASS] BadDigest returned
echo.

echo [TEST] System metadata, user metadata and tags on PUT are stored...
aws s3api put-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key meta.txt --body exists.txt --cache-control max-age=60 --content-disposition attachment --metadata Color=Blue --tagging team=storage >nul
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key meta.txt --query "join(',', [CacheControl, ContentDisposition, Metadata.color])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="max-age=60,attachment,Blue" (
    echo [FAIL] Stored metadata is [!VAL!]
    goto :error
)
aws s3api get-object-tagging --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key meta.txt --query "TagSet[0].Value" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="storage" (
    echo [FAIL] Tag from x-amz-tagging is [!VAL!]
    goto :error
)
echo [PASS] Metadata and tags stored
echo.

rem ---------------------------------------------------------------- Copy
echo [TEST] CopyObject copies content and metadata...
aws s3api copy-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key meta-copy.txt --copy-source %COMPAT_OBJ%/meta.txt >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] copy-object failed
    goto :error
)
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key meta-copy.txt --query "join(',', [to_string(ContentLength), CacheControl, Metadata.color])" --output text > "%OUT%"
set VAL=
set /p VAL=<"%OUT%"
if not "!VAL!"=="8,max-age=60,Blue" (
    echo [FAIL] Copied object is [!VAL!]
    goto :error
)
echo [PASS] CopyObject
echo.

echo [TEST] Copying a missing source fails and creates nothing...
aws s3api copy-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key never.txt --copy-source %COMPAT_OBJ%/missing.txt >nul 2>"%ERR%"
findstr /C:"NoSuchKey" "%ERR%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Expected NoSuchKey
    goto :error
)
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key never.txt >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] A failed copy created the destination
    goto :error
)
echo [PASS] Failed copy leaves no destination
echo.

echo [TEST] Moving a 15 MB object server-side (multipart copy) preserves its content...
fsutil file createnew copy-large.dat 15728640 >nul
aws --endpoint-url %ENDPOINT% s3 cp copy-large.dat s3://%COMPAT_OBJ%/large-src.dat >nul
aws --endpoint-url %ENDPOINT% s3 mv s3://%COMPAT_OBJ%/large-src.dat s3://%COMPAT_LIST%/large-dst.dat >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] s3 mv failed
    goto :error
)
aws --endpoint-url %ENDPOINT% s3 cp s3://%COMPAT_LIST%/large-dst.dat copy-large-downloaded.dat >nul
fc /b copy-large.dat copy-large-downloaded.dat >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Moved object content differs from the original
    goto :error
)
aws s3api head-object --endpoint-url %ENDPOINT% --bucket %COMPAT_OBJ% --key large-src.dat >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] Source still exists after s3 mv
    goto :error
)
echo [PASS] Server-side move preserved content
echo.

echo ===============================================================================
echo CLEANUP
echo ===============================================================================
echo.

echo [TEST] Removing test buckets and every object version...
for %%b in (%TEST_BUCKET% %COMPAT_DEL% %COMPAT_VER% %COMPAT_LIST% %COMPAT_OBJ%) do call :purge_bucket %%b
aws s3api head-bucket --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [WARN] Cleanup may have failed - %TEST_BUCKET% still exists
)
echo [PASS] Cleanup completed
echo.

call :cleanup_files
echo [PASS] Temporary files cleaned
echo.

echo ===============================================================================
echo TEST SUMMARY
echo ===============================================================================
echo.
echo [SUCCESS] All tests passed!
echo.
goto :end

:error
echo.
echo ===============================================================================
echo TEST SUMMARY
echo ===============================================================================
echo.
echo [FAILURE] One or more tests failed!
echo.
echo Cleaning up temporary files...
call :cleanup_files
exit /b 1

rem Delete every version and delete marker in a bucket, then the bucket.
:purge_bucket
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %~1 --query "[Versions, DeleteMarkers][][].[VersionId, Key]" --output text > "%TEMP%\less3-awscli-versions.tmp" 2>nul
for /f "usebackq tokens=1,*" %%a in ("%TEMP%\less3-awscli-versions.tmp") do (
    if not "%%a"=="None" aws s3api delete-object --endpoint-url %ENDPOINT% --bucket %~1 --key "%%b" --version-id %%a >nul 2>nul
)
aws s3api list-multipart-uploads --endpoint-url %ENDPOINT% --bucket %~1 --query "Uploads[].[UploadId, Key]" --output text > "%TEMP%\less3-awscli-versions.tmp" 2>nul
for /f "usebackq tokens=1,*" %%a in ("%TEMP%\less3-awscli-versions.tmp") do (
    if not "%%a"=="None" aws s3api abort-multipart-upload --endpoint-url %ENDPOINT% --bucket %~1 --key "%%b" --upload-id %%a >nul 2>nul
)
aws s3api delete-bucket --endpoint-url %ENDPOINT% --bucket %~1 >nul 2>nul
del /q "%TEMP%\less3-awscli-versions.tmp" 2>nul
exit /b 0

:cleanup_files
del /q test-file.txt test-file-downloaded.txt test-range.txt multipart-auto.dat part1.dat part2.dat 2>nul
del /q test-version.txt complete-multipart.json exists.txt v.txt k.txt ten.txt out.txt 2>nul
del /q copy-large.dat copy-large-downloaded.dat empty.txt "%OUT%" "%ERR%" 2>nul
exit /b 0

:end
endlocal
@echo on
