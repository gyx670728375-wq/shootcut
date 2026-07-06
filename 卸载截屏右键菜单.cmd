@echo off
title Uninstall Global Screenshot Menu
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
set "UNINSTALL_EXIT=%errorlevel%"
echo.
if errorlevel 1 (
    echo Uninstallation failed. Please check the error shown above.
) else (
    echo Uninstallation completed.
)
echo.
pause
exit /b %UNINSTALL_EXIT%
