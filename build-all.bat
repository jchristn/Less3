@ECHO OFF
IF "%1" == "" GOTO :Usage

CALL "%~dp0build-dashboard.bat" %*
IF ERRORLEVEL 1 GOTO :Failed

CALL "%~dp0build-server.bat" %*
IF ERRORLEVEL 1 GOTO :Failed

GOTO :Done

:Usage
ECHO.
ECHO Provide an argument specifying the version or tag.
ECHO Example: build-all.bat v3.0.0
EXIT /B 1

:Failed
ECHO.
ECHO Build failed.
EXIT /B 1

:Done
ECHO.
ECHO Done
@ECHO ON
