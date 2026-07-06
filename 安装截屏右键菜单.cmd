@echo off
chcp 65001 >nul
title 安装截屏右键菜单
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
set "INSTALL_EXIT=%errorlevel%"
echo.
if errorlevel 1 (
    echo 安装失败，请保留此窗口并检查上方错误信息。
) else (
    echo 安装完成。Windows 11 请在右键菜单中打开“显示更多选项”。
)
echo.
pause
exit /b %INSTALL_EXIT%
