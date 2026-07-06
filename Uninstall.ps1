[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appName = 'GlobalScreenshotMenu'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path -LiteralPath $runKey) {
    Remove-ItemProperty -Path $runKey -Name $appName -Force -ErrorAction SilentlyContinue
}

$menuLocations = @(
    'HKCU:\Software\Classes\Directory\Background\shell\QuickScreenshot',
    'HKCU:\Software\Classes\DesktopBackground\Shell\QuickScreenshot'
)

foreach ($location in $menuLocations) {
    if (Test-Path -LiteralPath $location) {
        Remove-Item -LiteralPath $location -Recurse -Force
    }
}

Write-Host ''
Write-Host 'Uninstallation completed. The global screenshot helper has been removed.' -ForegroundColor Green
