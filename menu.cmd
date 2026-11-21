@echo off
setlocal

pushd "%~dp0"
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Menu.ps1"
set "exitCode=%errorlevel%"
popd

exit /b %exitCode%