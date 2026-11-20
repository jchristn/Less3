@echo off
REM Build every Less3 installer Windows can produce into installers\<version>\:
REM   the setup .exe (win-x64, win-arm64) and the portable .zip archives, plus SHA256SUMS.
REM Run build-installers.sh on macOS and Linux for theirs. All three operating systems build together in CI:
REM push a v<version> tag (see .github\workflows\release.yml). Needs Inno Setup 6: choco install innosetup
REM
REM Usage: build-installers.bat [version]
setlocal enabledelayedexpansion
cd /d "%~dp0"

set "VERSION_ARGS="
if /i "%~1"=="-h" goto :usage
if /i "%~1"=="--help" goto :usage
if not "%~1"=="" set "VERSION_ARGS=--version %~1"

echo Building the Less3 publisher...
dotnet build src\Less3.Publisher\Less3.Publisher.csproj -c Release --nologo -v q
if errorlevel 1 exit /b 1
set "PUB=src\Less3.Publisher\bin\Release\net10.0\less3-publish.dll"

for /f "delims=" %%v in ('dotnet "%PUB%" --print-version %VERSION_ARGS%') do set "VERSION=%%v"
set "OUT=installers\%VERSION%"
echo.
echo Version : %VERSION%
echo Output  : %OUT%

dotnet "%PUB%" --channel installers --sums --output "%OUT%" %VERSION_ARGS%
if errorlevel 1 exit /b 1

echo.
echo Installers are in %OUT%. Install one, open a new terminal, and run: less3 (or less3-dashboard)
endlocal & exit /b 0

:usage
echo Usage: build-installers.bat [version]
echo   version   override the version (default: ^<Version^> in src\Directory.Build.props)
endlocal & exit /b 0
