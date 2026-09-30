@echo off
setlocal enabledelayedexpansion

echo ===============================================================================
echo Less3 Comprehensive API Test Suite - MinIO Client (mc) Version
echo ===============================================================================
echo.
echo [IMPORTANT] Ensure you run this script against a clean installation of Less3
echo             If you have leftover buckets or objects, delete less3.db and restart
echo.
echo Optional environment variables:
echo   LESS3_ENDPOINT     Less3 S3 endpoint (default http://localhost:8000)
echo   LESS3_ACCESS_KEY   access key (default: default)
echo   LESS3_SECRET_KEY   secret key, at least 8 characters for mc (default: defaultsecret)
echo.

if defined LESS3_ENDPOINT (set ENDPOINT=%LESS3_ENDPOINT%) else (set ENDPOINT=http://localhost:8000)
if defined LESS3_ACCESS_KEY (set ACCESS_KEY=%LESS3_ACCESS_KEY%) else (set ACCESS_KEY=default)
if defined LESS3_SECRET_KEY (set SECRET_KEY=%LESS3_SECRET_KEY%) else (set SECRET_KEY=defaultsecret)
set ALIAS=less3
set TEST_BUCKET=test-bucket
set COMPAT_VER=test-compat-versions
set COMPAT_OBJ=test-compat-objects
set COMPAT_COPY=test-compat-copy
set OUT=%TEMP%\less3-mc-out.tmp
set ERR=%TEMP%\less3-mc-err.tmp

echo Configuring MinIO Client...
echo [INFO] MinIO Client requires secret keys to be at least 8 characters
echo [INFO] Using ACCESS_KEY=%ACCESS_KEY%
mc alias set %ALIAS% %ENDPOINT% %ACCESS_KEY% %SECRET_KEY% >nul
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

echo [TEST] Cleaning up any pre-existing test buckets...
for %%b in (%TEST_BUCKET% %COMPAT_VER% %COMPAT_OBJ% %COMPAT_COPY%) do call :purge_bucket %%b
echo [INFO] Pre-test cleanup attempted
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
fsutil file createnew test-multipart-large.dat 15728640 >nul
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
echo S3 COMPATIBILITY TESTS
echo ===============================================================================
echo.

mc mb %ALIAS%/%COMPAT_VER% >nul
mc mb %ALIAS%/%COMPAT_OBJ% >nul
mc mb %ALIAS%/%COMPAT_COPY% >nul

rem ---------------------------------------------------------------- batch delete
echo [TEST] Recursive remove (DeleteObjects) removes every object under a prefix...
echo x> x.txt
for %%k in (one two three) do mc cp --quiet x.txt %ALIAS%/%COMPAT_OBJ%/batch/%%k.txt >nul
mc rm --recursive --force %ALIAS%/%COMPAT_OBJ%/batch/ > "%OUT%" 2>"%ERR%"
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Recursive remove failed
    type "%ERR%"
    goto :error
)
mc ls --recursive %ALIAS%/%COMPAT_OBJ%/batch/ > "%OUT%" 2>nul
call :expect_empty "%OUT%" "objects remain after recursive remove"
if defined FAILED goto :error
echo [PASS] Recursive remove succeeded
echo.

rem ---------------------------------------------------------------- versioning
echo [TEST] Versioned delete keeps earlier versions; undo restores the object...
mc version enable %ALIAS%/%COMPAT_VER% >nul
for %%v in (one two three) do (
    echo %%v> v.txt
    mc cp --quiet v.txt %ALIAS%/%COMPAT_VER%/doc >nul
)
mc rm %ALIAS%/%COMPAT_VER%/doc >nul
mc cat %ALIAS%/%COMPAT_VER%/doc > "%OUT%" 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] A deleted object is still readable
    goto :error
)
mc cat --vid 1 %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_content "%OUT%" "one" "version 1 after delete"
if defined FAILED goto :error
mc ls --versions --json %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_count "%OUT%" "\"versionId\"" 4 "versions plus delete marker"
if defined FAILED goto :error
mc undo %ALIAS%/%COMPAT_VER%/doc --force >nul
mc cat %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_content "%OUT%" "three" "object after undo"
if defined FAILED goto :error
echo [PASS] Delete marker, version reads and undo
echo.

echo [TEST] Suspending versioning keeps existing versions (no data loss)...
mc version suspend %ALIAS%/%COMPAT_VER% >nul
mc version info --json %ALIAS%/%COMPAT_VER% > "%OUT%"
findstr /C:"Suspended" "%OUT%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Versioning is not reported as Suspended
    type "%OUT%"
    goto :error
)
echo suspended> v.txt
mc cp --quiet v.txt %ALIAS%/%COMPAT_VER%/doc >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Write after suspending versioning failed
    goto :error
)
mc ls --versions --json %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_count "%OUT%" "\"versionId\"" 4 "three earlier versions plus the null version"
if defined FAILED goto :error
mc cat --vid 1 %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_content "%OUT%" "one" "version 1 after suspending"
if defined FAILED goto :error
mc cat %ALIAS%/%COMPAT_VER%/doc > "%OUT%"
call :expect_content "%OUT%" "suspended" "latest after suspended write"
if defined FAILED goto :error
echo [PASS] Suspended versioning keeps prior versions
echo.

echo [TEST] A bucket holding versions cannot be removed without --force...
mc rb %ALIAS%/%COMPAT_VER% > "%OUT%" 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] Bucket with versions was removed
    goto :error
)
echo [PASS] Bucket removal refused
echo.

rem ---------------------------------------------------------------- listing
echo [TEST] mirror then diff reports no differences (listing is in S3 key order)...
mkdir test-order 2>nul
mkdir test-order\dir 2>nul
for %%k in (Zed.txt a_b.txt a-b.txt ab.txt a.txt _under.txt 10.txt 9.txt dir\y.txt dir\z.txt) do echo %%k> test-order\%%k
mc mirror --quiet test-order %ALIAS%/%COMPAT_OBJ%/order >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Mirror failed
    goto :error
)
mc diff test-order %ALIAS%/%COMPAT_OBJ%/order > "%OUT%" 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] diff failed
    type "%OUT%"
    goto :error
)
call :expect_empty "%OUT%" "mc diff reported differences"
if defined FAILED goto :error
echo [PASS] No differences after mirror
echo.

echo [TEST] Keys differing only in case are distinct objects...
echo upper> upper.txt
echo lower> lower.txt
mc cp --quiet upper.txt %ALIAS%/%COMPAT_OBJ%/Case.txt >nul
mc cp --quiet lower.txt %ALIAS%/%COMPAT_OBJ%/case.txt >nul
mc cat %ALIAS%/%COMPAT_OBJ%/Case.txt > "%OUT%"
call :expect_content "%OUT%" "upper" "Case.txt"
if defined FAILED goto :error
mc cat %ALIAS%/%COMPAT_OBJ%/case.txt > "%OUT%"
call :expect_content "%OUT%" "lower" "case.txt"
if defined FAILED goto :error
echo [PASS] Case-sensitive keys
echo.

echo [TEST] Keys with spaces and plus signs list and read back exactly...
echo encoded> enc.txt
mc cp --quiet enc.txt "%ALIAS%/%COMPAT_OBJ%/enc dir/a b+c.txt" >nul
mc ls --recursive "%ALIAS%/%COMPAT_OBJ%/enc dir/" > "%OUT%"
call :expect_count "%OUT%" "a b+c.txt" 1 "ListObjects key with a space and a plus"
if defined FAILED goto :error
mc cat "%ALIAS%/%COMPAT_OBJ%/enc dir/a b+c.txt" > "%OUT%"
call :expect_content "%OUT%" "encoded" "content of the encoded key"
if defined FAILED goto :error
mc cp --quiet enc.txt "%ALIAS%/%COMPAT_VER%/enc dir/a b+c.txt" >nul
mc ls --versions "%ALIAS%/%COMPAT_VER%/enc dir/" > "%OUT%"
call :expect_count "%OUT%" "a b+c.txt" 1 "ListObjectVersions key with a space and a plus"
if defined FAILED goto :error
echo [PASS] Encoded keys round-trip
echo.

rem ---------------------------------------------------------------- ranges, metadata, tags
<nul set /p =0123456789> ten.txt
mc cp --quiet ten.txt %ALIAS%/%COMPAT_OBJ%/ten.txt >nul

echo [TEST] Range reads (offset and tail)...
mc cat --offset 5 %ALIAS%/%COMPAT_OBJ%/ten.txt > "%OUT%"
call :expect_content "%OUT%" "56789" "offset 5"
if defined FAILED goto :error
mc cat --tail 3 %ALIAS%/%COMPAT_OBJ%/ten.txt > "%OUT%"
call :expect_content "%OUT%" "789" "tail 3"
if defined FAILED goto :error
echo [PASS] Range reads
echo.

echo [TEST] Metadata and tags given at upload are stored...
mc cp --quiet --attr "Cache-Control=max-age=60;Color=Blue" --tags "team=storage" ten.txt %ALIAS%/%COMPAT_OBJ%/meta.txt >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Upload with metadata and tags failed
    goto :error
)
mc stat --json %ALIAS%/%COMPAT_OBJ%/meta.txt > "%OUT%"
findstr /I /C:"max-age=60" "%OUT%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Cache-Control not stored
    type "%OUT%"
    goto :error
)
findstr /C:"Blue" "%OUT%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] User metadata not stored
    type "%OUT%"
    goto :error
)
mc tag list --json %ALIAS%/%COMPAT_OBJ%/meta.txt > "%OUT%"
findstr /C:"storage" "%OUT%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Tags given at upload not stored
    type "%OUT%"
    goto :error
)
echo [PASS] Metadata and tags stored
echo.

rem ---------------------------------------------------------------- server-side copy
echo [TEST] Server-side copy between buckets preserves content and metadata...
mc cp --quiet %ALIAS%/%COMPAT_OBJ%/meta.txt %ALIAS%/%COMPAT_COPY%/meta-copy.txt >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Server-side copy failed
    goto :error
)
mc cat %ALIAS%/%COMPAT_COPY%/meta-copy.txt > "%OUT%"
call :expect_content "%OUT%" "0123456789" "copied content"
if defined FAILED goto :error
mc stat --json %ALIAS%/%COMPAT_COPY%/meta-copy.txt > "%OUT%"
findstr /C:"Blue" "%OUT%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Metadata not copied
    goto :error
)
echo [PASS] Server-side copy
echo.

echo [TEST] Server-side move of a 15 MB object preserves its content...
fsutil file createnew move-large.dat 15728640 >nul
mc cp --quiet move-large.dat %ALIAS%/%COMPAT_OBJ%/large.dat >nul
mc mv --quiet %ALIAS%/%COMPAT_OBJ%/large.dat %ALIAS%/%COMPAT_COPY%/large-moved.dat >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Server-side move failed
    goto :error
)
mc cp --quiet %ALIAS%/%COMPAT_COPY%/large-moved.dat move-large-downloaded.dat >nul
fc /b move-large.dat move-large-downloaded.dat >nul
if %ERRORLEVEL% NEQ 0 (
    echo [FAIL] Moved object content differs from the original
    goto :error
)
mc stat %ALIAS%/%COMPAT_OBJ%/large.dat >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [FAIL] Source still exists after mv
    goto :error
)
echo [PASS] Server-side move preserved content
echo.

echo ===============================================================================
echo CLEANUP
echo ===============================================================================
echo.

echo [TEST] Removing test buckets and every object version...
for %%b in (%TEST_BUCKET% %COMPAT_VER% %COMPAT_OBJ% %COMPAT_COPY%) do call :purge_bucket %%b
mc ls %ALIAS%/%TEST_BUCKET% >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [WARN] Cleanup may have failed - some objects may remain
    echo [INFO] You may need to manually delete: mc rb --force %ALIAS%/%TEST_BUCKET%
)
echo [PASS] Cleanup completed
echo.

echo [TEST] Removing MinIO Client alias...
mc alias rm %ALIAS% >nul
echo [PASS] Alias removed
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
echo [INFO] Comparison with AWS CLI Test Suite:
echo        - MinIO Client automatically handles multipart uploads
echo        - ACL support is simplified (bucket-level anonymous policies)
echo        - Includes mirror/diff checks that depend on S3 key ordering
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
call :cleanup_files
mc alias rm %ALIAS% >nul 2>nul
exit /b 1

rem Remove every object version, delete marker and incomplete upload, then the bucket.
:purge_bucket
mc rm --recursive --force --versions %ALIAS%/%~1 >nul 2>nul
mc rm --incomplete --recursive --force %ALIAS%/%~1 >nul 2>nul
mc rb %ALIAS%/%~1 >nul 2>nul
exit /b 0

rem expect_content <file> <expected> <description>: the file holds exactly one line equal to <expected>.
:expect_content
set FAILED=
set VAL=
set /p VAL=<%1
if not "!VAL!"=="%~2" (
    echo [FAIL] %~3: expected [%~2], got [!VAL!]
    set FAILED=1
)
exit /b 0

rem expect_count <file> <text> <count> <description>: the file has <count> lines containing <text>.
:expect_count
set FAILED=
set COUNT=0
rem Windows find.exe by full path: a Unix 'find' earlier on PATH (e.g. from Git) would scan the drive.
for /f %%c in ('findstr /C:%2 %1 ^| "%SystemRoot%\System32\find.exe" /c /v ""') do set COUNT=%%c
if not "!COUNT!"=="%~3" (
    echo [FAIL] %~4: expected %~3, found !COUNT!
    set FAILED=1
)
exit /b 0

rem expect_empty <file> <description>: the file is empty.
:expect_empty
set FAILED=
for %%f in (%1) do if %%~zf NEQ 0 (
    echo [FAIL] %~2:
    type %1
    set FAILED=1
)
exit /b 0

:cleanup_files
del /q test-file.txt test-file-downloaded.txt test-multipart-large.dat test-version.txt 2>nul
del /q x.txt v.txt upper.txt lower.txt ten.txt enc.txt move-large.dat move-large-downloaded.dat 2>nul
del /q "%OUT%" "%ERR%" 2>nul
rd /s /q test-mirror 2>nul
rd /s /q test-order 2>nul
exit /b 0

:end
endlocal
@echo on
