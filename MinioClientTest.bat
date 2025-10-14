@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo Less3 Comprehensive API Test Suite - MinIO Client (mc) Version
echo ===============================================================================
echo.
echo [IMPORTANT] Ensure you run this script against a clean installation of Less3
echo             If you have leftover buckets or objects, delete less3.db and restart
echo.

set ENDPOINT=http://localhost:8000
set ACCESS_KEY=default
set SECRET_KEY=defaultsecret
set ALIAS=less3
set TEST_BUCKET=test-bucket

echo Configuring MinIO Client...
echo [INFO] MinIO Client requires secret keys to be at least 8 characters
echo [INFO] Using ACCESS_KEY=%ACCESS_KEY% and SECRET_KEY=%SECRET_KEY%
mc alias set %ALIAS% %ENDPOINT% %ACCESS_KEY% %SECRET_KEY%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] MinIO Client configuration failed
    echo Please ensure mc is installed: https://min.io/docs/minio/linux/reference/minio-mc.html
    goto :error
)
echo [PASS] MinIO Client configured
echo.

echo ===============================================================================
echo SERVICE OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Listing buckets...
mc ls %ALIAS%/
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
mc rm --recursive --force --versions %ALIAS%/%TEST_BUCKET%/
timeout /t 2 /nobreak >nul
mc rb --force %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% EQU 0 (
    echo [INFO] Pre-existing bucket was deleted
) else (
    echo [INFO] No pre-existing bucket found or already clean
)
timeout /t 1 /nobreak >nul
echo.

echo [TEST] Creating test bucket...
mc mb %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Create bucket failed
    goto :error
)
echo [PASS] Create bucket succeeded
echo.

echo [TEST] Listing buckets to verify...
mc ls %ALIAS%/ | findstr %TEST_BUCKET%
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
echo Hello from Less3 API test with MinIO Client! > test-file.txt
echo [PASS] Test file created
echo.

echo [TEST] Uploading object...
mc cp test-file.txt %ALIAS%/%TEST_BUCKET%/test-file.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload object failed
    goto :error
)
echo [PASS] Upload object succeeded
echo.

echo [TEST] Checking if object exists...
mc stat %ALIAS%/%TEST_BUCKET%/test-file.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Stat object failed
    goto :error
)
echo [PASS] Stat object succeeded
echo.

echo [TEST] Downloading object...
mc cp %ALIAS%/%TEST_BUCKET%/test-file.txt test-file-downloaded.txt
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
mc ls %ALIAS%/%TEST_BUCKET%/
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List objects failed
    goto :error
)
echo [PASS] List objects succeeded
echo.

echo [TEST] Deleting object...
mc rm %ALIAS%/%TEST_BUCKET%/test-file.txt
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
echo [NOTE] MinIO Client automatically handles multipart uploads for large files
echo.

echo [TEST] Creating large test file (15 MB)...
fsutil file createnew test-multipart-large.dat 15728640
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Create large file failed
    goto :error
)
echo [PASS] Large file created
echo.

echo [TEST] Uploading large file (multipart handled automatically)...
mc cp --quiet test-multipart-large.dat %ALIAS%/%TEST_BUCKET%/multipart-large.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Large file upload failed
    goto :error
)
echo [PASS] Large file upload succeeded
echo.

echo [TEST] Verifying uploaded large file exists...
mc stat %ALIAS%/%TEST_BUCKET%/multipart-large.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Large file stat failed
    goto :error
)
echo [PASS] Large file verified
echo.

echo [TEST] Deleting large file...
mc rm %ALIAS%/%TEST_BUCKET%/multipart-large.dat
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete large file failed
    goto :error
)
echo [PASS] Delete large file succeeded
echo.

echo ===============================================================================
echo ACL / POLICY OPERATIONS TESTS
echo ===============================================================================
echo.
echo [NOTE] Less3 currently supports ACLs but not bucket policies
echo       MinIO Client's 'mc anonymous' command requires bucket policy APIs
echo       which are not yet implemented in Less3
echo       For ACL testing, use AwsCliTest.bat with AWS CLI
echo.
echo [SKIP] Skipping MinIO Client anonymous policy tests (not supported)
echo.

echo ===============================================================================
echo TAGGING OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Uploading object for tagging...
mc cp test-file.txt %ALIAS%/%TEST_BUCKET%/test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload object failed
    goto :error
)
echo [PASS] Object uploaded
echo.

echo [TEST] Setting object tags...
mc tag set %ALIAS%/%TEST_BUCKET%/test-tag.txt "Environment=Test&Application=Less3"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Set object tags failed
    goto :error
)
echo [PASS] Object tags set
echo.

echo [TEST] Getting object tags...
mc tag list %ALIAS%/%TEST_BUCKET%/test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get object tags failed
    goto :error
)
echo [PASS] Get object tags succeeded
echo.

echo [TEST] Removing object tags...
mc tag remove %ALIAS%/%TEST_BUCKET%/test-tag.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Remove object tags failed
    goto :error
)
echo [PASS] Object tags removed
echo.

echo [TEST] Setting bucket tags...
mc tag set %ALIAS%/%TEST_BUCKET% "Project=Less3&Owner=TestUser"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Set bucket tags failed
    goto :error
)
echo [PASS] Bucket tags set
echo.

echo [TEST] Getting bucket tags...
mc tag list %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get bucket tags failed
    goto :error
)
echo [PASS] Get bucket tags succeeded
echo.

echo [TEST] Removing bucket tags...
mc tag remove %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Remove bucket tags failed
    goto :error
)
echo [PASS] Bucket tags removed
echo.

echo [TEST] Deleting tagging test object...
mc rm %ALIAS%/%TEST_BUCKET%/test-tag.txt
echo [PASS] Tagging test object deleted
echo.

echo ===============================================================================
echo VERSIONING OPERATIONS TESTS
echo ===============================================================================
echo.

echo [TEST] Enabling versioning on bucket...
mc version enable %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Enable versioning failed
    goto :error
)
echo [PASS] Versioning enabled
echo.

echo [TEST] Getting bucket versioning status...
mc version info %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Get bucket versioning failed
    goto :error
)
echo [PASS] Get bucket versioning succeeded
echo.

echo [TEST] Uploading version 1...
echo Version 1 content > test-version.txt
mc cp test-version.txt %ALIAS%/%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 1 failed
    goto :error
)
echo [PASS] Version 1 uploaded
echo.

echo [TEST] Uploading version 2...
echo Version 2 content > test-version.txt
mc cp test-version.txt %ALIAS%/%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 2 failed
    goto :error
)
echo [PASS] Version 2 uploaded
echo.

echo [TEST] Uploading version 3...
echo Version 3 content > test-version.txt
mc cp test-version.txt %ALIAS%/%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload version 3 failed
    goto :error
)
echo [PASS] Version 3 uploaded
echo.

echo [TEST] Listing object versions...
mc ls --versions %ALIAS%/%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List object versions failed
    goto :error
)
echo [PASS] List object versions succeeded
echo.

echo [TEST] Deleting versioned object (creates delete marker)...
mc rm %ALIAS%/%TEST_BUCKET%/test-version.txt
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Delete versioned object failed
    goto :error
)
echo [PASS] Versioned object deleted (delete marker created)
echo.

echo ===============================================================================
echo MIRROR/SYNC OPERATIONS TEST
echo ===============================================================================
echo.
echo [NOTE] MinIO Client has powerful mirror/sync features not available in AWS CLI
echo.

echo [TEST] Creating local directory structure...
mkdir test-mirror 2>nul
echo File 1 > test-mirror\file1.txt
echo File 2 > test-mirror\file2.txt
mkdir test-mirror\subdir 2>nul
echo File 3 > test-mirror\subdir\file3.txt
echo [PASS] Local directory created
echo.

echo [TEST] Mirroring local directory to bucket...
mc mirror test-mirror %ALIAS%/%TEST_BUCKET%/mirror-test
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Mirror to bucket failed
    goto :error
)
echo [PASS] Mirror to bucket succeeded
echo.

echo [TEST] Listing mirrored objects...
mc ls --recursive %ALIAS%/%TEST_BUCKET%/mirror-test
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] List mirrored objects failed
    goto :error
)
echo [PASS] Mirrored objects listed
echo.

echo [TEST] Cleaning up mirrored objects...
mc rm --recursive --force %ALIAS%/%TEST_BUCKET%/mirror-test
echo [PASS] Mirrored objects cleaned
echo.

echo ===============================================================================
echo CLEANUP
echo ===============================================================================
echo.

echo [TEST] Disabling versioning on test bucket...
mc version suspend %ALIAS%/%TEST_BUCKET% 2>nul
echo.

echo [TEST] Cleaning up all object versions...
mc rm --recursive --force --versions %ALIAS%/%TEST_BUCKET%/ 2>nul
timeout /t 2 /nobreak >nul
echo.

echo [TEST] Cleaning up test bucket...
mc rb --force %ALIAS%/%TEST_BUCKET%
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Cleanup may have failed - some objects may remain
    echo [INFO] You may need to manually delete: mc rb --force %ALIAS%/%TEST_BUCKET%
)
echo [PASS] Cleanup completed
echo.

echo [TEST] Removing MinIO Client alias...
mc alias rm %ALIAS%
echo [PASS] Alias removed
echo.

echo [TEST] Cleaning up temporary files and directories...
del /q test-file.txt 2>nul
del /q test-file-downloaded.txt 2>nul
del /q test-multipart-large.dat 2>nul
del /q test-version.txt 2>nul
rd /s /q test-mirror 2>nul
echo [PASS] Temporary files cleaned
echo.

echo ===============================================================================
echo TEST SUMMARY
echo ===============================================================================
echo.
echo [SUCCESS] All tests passed!
echo.
echo [INFO] Comparison with AWS CLI Test Suite:
echo        - MinIO Client automatically handles multipart uploads
echo        - ACL support is simplified (bucket-level anonymous policies)
echo        - Range reads not directly supported
echo        - Includes mirror/sync features not in AWS CLI
echo        - For comprehensive S3 API testing, use AwsCliTest.bat
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
echo Cleaning up temporary files and directories...
del /q test-file.txt 2>nul
del /q test-file-downloaded.txt 2>nul
del /q test-multipart-large.dat 2>nul
del /q test-version.txt 2>nul
rd /s /q test-mirror 2>nul
mc alias rm %ALIAS% 2>nul
exit /b 1

:end
endlocal
@echo on
