@echo off

for %%i in (*.proto) do (ECHO Compiling %%i.. & bin\protoc --csharp_out=..\Demo\Pokemon\Proto %%i)

pause