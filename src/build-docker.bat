@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f Less3\Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag jchristn/less3:%1 --push .
GOTO :Done

:Usage
ECHO.
ECHO Provide an argument specifying the version or tag.
ECHO Example: dockerbuild.bat v2.1.12

:Done
ECHO.
ECHO Done
@ECHO ON
