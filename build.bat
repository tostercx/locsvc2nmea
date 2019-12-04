@echo off
pushd %~dp0

rmdir nmeasvc\bin /s /q
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsMSBuildCmd.bat"
call msbuild.exe /p:Configuration=Release /p:Platform="Any CPU" nmeasvc\nmeasvc.sln

rmdir build /s /q
mkdir build
copy nmeasvc\bin\Release\nmeasvc.exe build
copy com0com\x64\* build
copy install.bat build

cd build
7z a -tzip ..\locsvc2nmea.zip .
cd ..

popd
