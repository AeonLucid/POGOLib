@echo off

cd pogo\

setlocal enabledelayedexpansion
for /R . %%f in (*) do (
  set B=%%f
  ECHO Compiling "!B:%CD%\=!"..
  ..\..\packages\Google.Protobuf.Tools.3.0.0-beta3\tools\windows_x64\protoc --csharp_out=..\..\POGOLib\Pokemon\Proto "!B:%CD%\=!"
)

pause