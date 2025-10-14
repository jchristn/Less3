@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo Less3 Comprehensive API Test Suite
echo ===============================================================================
echo.

set ENDPOINT=http://localhost:8000
set TEST_BUCKET=test-bucket

echo Configuring AWS CLI...
aws configure
echo.

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

echo [TEST] Cleaning up any pre-existing test bucket...
aws s3 rb s3://%TEST_BUCKET% --endpoint-url %ENDPOINT% --force 2>nul
echo [INFO] Pre-test cleanup attempted (ignoring errors if bucket didn't exist)
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
fsutil file createnew multipart-auto.dat 15728640
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
aws s3api create-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat > multipart-init.json
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Initiate multipart upload failed
    goto :error
)

for /f "tokens=2 delims=:, " %%a in ('findstr /C:"UploadId" multipart-init.json') do set UPLOAD_ID=%%~a
echo [PASS] Initiated multipart upload: !UPLOAD_ID!
echo.

echo [TEST] Creating part files...
fsutil file createnew part1.dat 5242880
fsutil file createnew part2.dat 5242880
echo [PASS] Part files created
echo.

echo [TEST] Uploading part 1...
aws s3api upload-part --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --part-number 1 --upload-id !UPLOAD_ID! --body part1.dat > part1-response.json
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload part 1 failed
    goto :error
)

for /f "tokens=2 delims=:, " %%a in ('findstr /C:"ETag" part1-response.json') do set ETAG1=%%~a
echo [PASS] Uploaded part 1: !ETAG1!
echo.

echo [TEST] Uploading part 2...
aws s3api upload-part --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --part-number 2 --upload-id !UPLOAD_ID! --body part2.dat > part2-response.json
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload part 2 failed
    goto :error
)

for /f "tokens=2 delims=:, " %%a in ('findstr /C:"ETag" part2-response.json') do set ETAG2=%%~a
echo [PASS] Uploaded part 2: !ETAG2!
echo.

echo [TEST] Listing parts...
aws s3api list-parts --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-manual.dat --upload-id !UPLOAD_ID!
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List parts failed
    goto :error
)
echo [PASS] List parts succeeded
echo.

echo [TEST] Creating multipart completion JSON...
(echo {"Parts":[{"ETag":"!ETAG1!","PartNumber":1},{"ETag":"!ETAG2!","PartNumber":2}]}) > complete-multipart.json
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
aws s3api create-multipart-upload --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --key multipart-abort.dat > abort-init.json
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"UploadId" abort-init.json') do set ABORT_UPLOAD_ID=%%~a
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

echo [TEST] Deleting ACL test object...
aws --endpoint-url %ENDPOINT% s3 rm s3://%TEST_BUCKET%/test-acl.txt
echo [PASS] ACL test object deleted
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
aws s3api get-bucket-versioning --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get bucket versioning failed
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
aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --prefix test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List object versions failed
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
echo [PASS] Versioned object deleted (delete marker created)
echo.

echo ===============================================================================
echo CLEANUP
echo ===============================================================================
echo.

echo [TEST] Suspending versioning on test bucket...
aws s3api put-bucket-versioning --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --versioning-configuration Status=Suspended 2>nul
echo.

echo [TEST] Cleaning up test bucket (removing all versions)...
aws s3api delete-objects --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --delete "$(aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --output json --query '{Objects: Versions[].{Key:Key,VersionId:VersionId}}')" 2>nul
aws s3api delete-objects --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --delete "$(aws s3api list-object-versions --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% --output json --query '{Objects: DeleteMarkers[].{Key:Key,VersionId:VersionId}}')" 2>nul
aws s3 rb s3://%TEST_BUCKET% --endpoint-url %ENDPOINT% --force 2>nul
aws s3api delete-bucket --endpoint-url %ENDPOINT% --bucket %TEST_BUCKET% 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Cleanup may have failed - some objects may remain
    echo [INFO] You may need to restart Less3 and delete less3.db to clean up
)
echo [PASS] Cleanup completed
echo.

echo [TEST] Cleaning up temporary files...
del /q test-file.txt 2>nul
del /q test-file-downloaded.txt 2>nul
del /q test-range.txt 2>nul
del /q multipart-auto.dat 2>nul
del /q part1.dat 2>nul
del /q part2.dat 2>nul
del /q test-version.txt 2>nul
del /q multipart-init.json 2>nul
del /q part1-response.json 2>nul
del /q part2-response.json 2>nul
del /q complete-multipart.json 2>nul
del /q abort-init.json 2>nul
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
del /q test-file.txt 2>nul
del /q test-file-downloaded.txt 2>nul
del /q test-range.txt 2>nul
del /q multipart-auto.dat 2>nul
del /q part1.dat 2>nul
del /q part2.dat 2>nul
del /q test-version.txt 2>nul
del /q multipart-init.json 2>nul
del /q part1-response.json 2>nul
del /q part2-response.json 2>nul
del /q complete-multipart.json 2>nul
del /q abort-init.json 2>nul
exit /b 1

:end
endlocal
@echo on
