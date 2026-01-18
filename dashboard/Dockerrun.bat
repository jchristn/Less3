@echo off

IF "%1" == "" GOTO :Usage

docker run ^
  -p 3000:3000 ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  jchristn77/less3-ui:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag. 
ECHO Example: dockerrun.bat v2.1.12
:Done
@echo on
