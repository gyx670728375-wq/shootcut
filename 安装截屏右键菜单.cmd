@echo off
title Install Global Screenshot Menu
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
set "INSTALL_EXIT=%errorlevel%"
echo.
if errorlevel 1 (
    echo Installation failed. Please check the error shown above.
) else (
    echo Installation completed. Use Ctrl + right-click anywhere.
)
echo.
pause
exit /b %INSTALL_EXIT%
