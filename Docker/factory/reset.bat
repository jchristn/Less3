@echo off
setlocal enabledelayedexpansion

REM ==========================================================================
REM reset.bat - Reset Less3 docker environment to factory defaults
REM
REM This script destroys all runtime data (database, object storage, logs)
REM and restores the factory-default database. Configuration files are
REM preserved.
REM
REM Usage: factory\reset.bat
REM ==========================================================================

set "SCRIPT_DIR=%~dp0"
set "DOCKER_DIR=%SCRIPT_DIR%..\"
set "FACTORY_DIR=%SCRIPT_DIR%"

REM -------------------------------------------------------------------------
REM Confirmation prompt
REM -------------------------------------------------------------------------
echo.
echo ==========================================================
echo   Less3 - Reset to Factory Defaults
echo ==========================================================
echo.
echo WARNING: This is a DESTRUCTIVE action. The following will
echo be permanently deleted:
echo.
echo   - Less3 SQLite database (buckets, objects, users,
echo     credentials, ACLs, tags)
echo   - All object storage files
echo   - All temporary files
echo   - All log files
echo.
echo Configuration files (system.json) will NOT be modified.
echo.
set /p "CONFIRM=Type 'RESET' to confirm: "
echo.

if not "%CONFIRM%"=="RESET" (
    echo Aborted. No changes were made.
    exit /b 1
)

REM -------------------------------------------------------------------------
REM Ensure containers are stopped
REM -------------------------------------------------------------------------
echo [1/5] Stopping containers...
pushd "%DOCKER_DIR%"
docker compose down 2>nul
popd

REM -------------------------------------------------------------------------
REM Restore factory database
REM -------------------------------------------------------------------------
echo [2/5] Restoring factory database...
del /q "%DOCKER_DIR%less3.db" 2>nul
del /q "%DOCKER_DIR%less3.db-shm" 2>nul
del /q "%DOCKER_DIR%less3.db-wal" 2>nul
copy /y "%FACTORY_DIR%less3.db" "%DOCKER_DIR%less3.db" >nul
echo         Restored less3.db

REM -------------------------------------------------------------------------
REM Clear object storage
REM -------------------------------------------------------------------------
echo [3/5] Clearing object storage...
if exist "%DOCKER_DIR%disk" rd /s /q "%DOCKER_DIR%disk" 2>nul && mkdir "%DOCKER_DIR%disk" 2>nul
del /q "%DOCKER_DIR%temp\*" 2>nul
echo         Cleared object storage and temp files

REM -------------------------------------------------------------------------
REM Clear logs
REM -------------------------------------------------------------------------
echo [4/5] Clearing logs...
del /q "%DOCKER_DIR%logs\*" 2>nul
echo         Cleared log files

REM -------------------------------------------------------------------------
REM Done
REM -------------------------------------------------------------------------
echo [5/5] Factory reset complete.
echo.
echo To start the environment:
echo   cd %DOCKER_DIR%
echo   docker compose up -d
echo.

endlocal
