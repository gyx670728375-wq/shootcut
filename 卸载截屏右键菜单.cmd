@echo off
chcp 65001 >nul
title 卸载截屏右键菜单
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
if errorlevel 1 (
    echo.
    echo 卸载失败，请保留此窗口并检查上方错误信息。
    pause
)
