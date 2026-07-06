[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

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
Write-Host 'Uninstallation completed. The screenshot menu item has been removed.' -ForegroundColor Green
