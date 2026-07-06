@echo off
chcp 65001 >nul
title 卸载截屏右键菜单
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
set "UNINSTALL_EXIT=%errorlevel%"
echo.
if errorlevel 1 (
    echo 卸载失败，请保留此窗口并检查上方错误信息。
) else (
    echo 卸载完成。
)
echo.
pause
exit /b %UNINSTALL_EXIT%
