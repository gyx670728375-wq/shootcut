[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$menuLocations = @(
    'HKCU:\Software\Classes\Directory\Background\shell\QuickScreenshot',
    'HKCU:\Software\Classes\DesktopBackground\Shell\QuickScreenshot'
)

$snippingTool = Join-Path $env:SystemRoot 'System32\SnippingTool.exe'
$icon = if (Test-Path -LiteralPath $snippingTool) { $snippingTool } else { 'shell32.dll,259' }
$command = 'explorer.exe "ms-screenclip:"'
$menuText = ([string][char]0x622A) + ([char]0x5C4F)

foreach ($location in $menuLocations) {
    $commandLocation = Join-Path $location 'command'

    New-Item -Path $commandLocation -Force | Out-Null
    Set-Item -Path $location -Value $menuText
    New-ItemProperty -Path $location -Name 'MUIVerb' -Value $menuText -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $location -Name 'Icon' -Value $icon -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $location -Name 'Position' -Value 'Top' -PropertyType String -Force | Out-Null
    Set-Item -Path $commandLocation -Value $command
}

Write-Host ''
Write-Host 'Installation completed.' -ForegroundColor Green
Write-Host 'Right-click the desktop or a folder background and select the screenshot command.'
Write-Host 'On Windows 11, it may appear under Show more options.'
Write-Host ''
Read-Host 'Press Enter to close'
