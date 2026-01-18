@echo off

IF "%1" == "" GOTO :Usage

if not exist system.json (
  echo Configuration file system.json not found.
  exit /b 1
)

REM Items that require persistence
REM   system.json
REM   less3.db
REM   logs/
REM   temp/
REM   disk/

REM Argument order matters!

docker run ^
  -p 8000:8000 ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  -v .\system.json:/app/system.json ^
  -v .\less3.db:/app/less3.db ^
  -v .\logs\:/app/logs/ ^
  -v .\temp\:/app/temp/ ^
  -v .\disk\:/app/disk/ ^
  jchristn77/less3:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag. 
ECHO Example: dockerrun.bat v2.1.11
:Done
@echo on
