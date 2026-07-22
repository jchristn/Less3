@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f dashboard\Dockerfile --platform linux/amd64,linux/arm64/v8 --tag jchristn77/less3-ui:%1 --push dashboard
GOTO :Done

:Usage
ECHO.
ECHO Provide a tag argument.
ECHO Example: build-dashboard.bat v3.0.0

:Done
ECHO.
ECHO Done
@ECHO ON
