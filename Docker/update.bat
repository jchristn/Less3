@ECHO OFF
PUSHD "%~dp0"
docker compose down && docker compose pull && docker compose up -d && docker ps -a
SET EXITCODE=%ERRORLEVEL%
POPD
EXIT /B %EXITCODE%
