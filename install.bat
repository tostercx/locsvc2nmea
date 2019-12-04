@echo off
if not "%1"=="am_admin" (powershell start -verb runas '%0' am_admin & exit /b)

pushd %~dp0
setupc uninstall
setupc install PortName=COM48 PortName=COM49
nmeasvc

pause
