@echo off
pushd %~dp0

rem build
rmdir nmeasvc\bin /s /q
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsMSBuildCmd.bat"
call msbuild.exe /p:Configuration=Release /p:Platform="Any CPU" nmeasvc\nmeasvc.sln

rem copy all files
rmdir build /s /q
mkdir build
copy nmeasvc\bin\Release\nmeasvc.exe build
copy com0com\x64\* build
copy install.bat build

rem get timestamp and zip build
for /F "tokens=* delims=_" %%i in ('PowerShell -Command "& {Get-Date -format "yyyyddMM"}"') do set FDATE=%%i
cd build
7z a -tzip ..\locsvc2nmea_%FDATE%.zip .
cd ..

popd
