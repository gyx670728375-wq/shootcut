@echo off
chcp 65001 >nul
title 安装截屏右键菜单
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if errorlevel 1 (
    echo.
    echo 安装失败，请保留此窗口并检查上方错误信息。
    pause
)
