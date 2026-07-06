[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appName = 'GlobalScreenshotMenu'
$sourceExecutable = Join-Path $PSScriptRoot "$appName.exe"
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

if (-not (Test-Path -LiteralPath $sourceExecutable)) {
    throw "Missing $appName.exe. Keep all downloaded files in the same folder."
}

Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -Path $runKey -Force | Out-Null
New-ItemProperty -Path $runKey -Name $appName -Value ('"{0}"' -f $sourceExecutable) -PropertyType String -Force | Out-Null

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

Start-Process -FilePath $sourceExecutable

Write-Host ''
Write-Host 'Installation completed.' -ForegroundColor Green
Write-Host 'Use Ctrl + right-click anywhere to open the screenshot menu.'
Write-Host 'The helper will start automatically when you sign in to Windows.'
