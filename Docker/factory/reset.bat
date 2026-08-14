@echo off
setlocal enabledelayedexpansion

REM ==========================================================================
REM reset.bat - Reset the Less3 Docker environment to factory defaults.
REM
REM The Docker deployment runs on PostgreSQL, with metadata in the 'pgdata'
REM volume and object data in the 'less3-data' volume. A factory reset drops
REM those volumes (wiping the database and object storage) and clears the
REM host-mounted logs. On the next 'docker compose up' the nodes recreate their
REM schema and re-seed the default tenant, credentials, and sample bucket
REM automatically - no database file to restore.
REM
REM Usage: factory\reset.bat
REM ==========================================================================

set "SCRIPT_DIR=%~dp0"
set "DOCKER_DIR=%SCRIPT_DIR%..\"

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
echo   - The PostgreSQL data volume (all metadata, lock state,
echo     cluster membership, buckets, users, credentials, ACLs)
echo   - The shared object-storage volume (all object data and
echo     multipart parts)
echo   - All host log files
echo.
echo Configuration files (system.node.json, clutch\clutch.json)
echo are NOT modified. The nodes re-seed the default data on the
echo next startup.
echo.
set /p "CONFIRM=Type 'RESET' to confirm: "
echo.

if not "%CONFIRM%"=="RESET" (
    echo Aborted. No changes were made.
    exit /b 1
)

REM -------------------------------------------------------------------------
REM Stop containers and drop the data volumes (the whole stack, including the
REM Clutch lock server). This removes the 'pgdata' and 'less3-data' named
REM volumes; on the next boot the nodes reconnect to Clutch (the default lock
REM provider) and re-seed the default data.
REM -------------------------------------------------------------------------
echo [1/2] Stopping containers and removing data volumes...
pushd "%DOCKER_DIR%"
docker compose down -v --remove-orphans 2>nul
popd

REM -------------------------------------------------------------------------
REM Clear host logs
REM -------------------------------------------------------------------------
echo [2/2] Clearing logs...
if not exist "%DOCKER_DIR%logs" mkdir "%DOCKER_DIR%logs" 2>nul
del /q "%DOCKER_DIR%logs\*" 2>nul
type nul > "%DOCKER_DIR%logs\.gitkeep"

echo.
echo Factory reset complete.
echo.
echo To start fresh (the nodes will recreate the schema and seed defaults):
echo   cd %DOCKER_DIR%
echo   docker compose up -d
echo.

endlocal
