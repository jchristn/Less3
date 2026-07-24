@echo off

set IMG_TAG=%1
if "%IMG_TAG%"=="" set IMG_TAG=v3.0.0

if not exist db mkdir db
if not exist logs mkdir logs
if not exist temp mkdir temp
if not exist disk mkdir disk

REM Items that require persistence
REM   system.json
REM   db\
REM   logs/
REM   temp/
REM   disk/

if exist system.json (
  echo Using mounted system.json from the Docker directory.
  docker run ^
    -p 8000:8000 ^
    -t ^
    -i ^
    -e "TERM=xterm-256color" ^
    -v .\system.json:/app/system.json ^
    -v .\db\:/app/db/ ^
    -v .\logs\:/app/logs/ ^
    -v .\temp\:/app/temp/ ^
    -v .\disk\:/app/disk/ ^
    jchristn77/less3:%IMG_TAG%
) else (
  echo system.json not found. Less3 will generate a default container configuration.
  docker run ^
    -p 8000:8000 ^
    -t ^
    -i ^
    -e "TERM=xterm-256color" ^
    -v .\db\:/app/db/ ^
    -v .\logs\:/app/logs/ ^
    -v .\temp\:/app/temp/ ^
    -v .\disk\:/app/disk/ ^
    jchristn77/less3:%IMG_TAG%
)
