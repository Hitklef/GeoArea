<#
.SYNOPSIS
    Створює ярлик GeoArea на робочому столі поточного користувача.

.PARAMETER ExePath
    Повний шлях до GeoArea.exe.

.PARAMETER ShortcutName
    Назва ярлика (без .lnk). За замовчуванням "GeoArea".

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\create-shortcut.ps1 -ExePath "C:\Projects\GeoArea\publish\GeoArea.exe"
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $ExePath,

    [string] $ShortcutName = "GeoArea"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExePath)) {
    Write-Error "Файл не знайдено: $ExePath"
    exit 1
}

$ExePath = (Resolve-Path -LiteralPath $ExePath).Path
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktop "$ShortcutName.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $ExePath
$shortcut.WorkingDirectory = Split-Path -Parent $ExePath
$shortcut.IconLocation = "$ExePath,0"
$shortcut.Description = "GeoArea — обчислення площі ділянки за координатами"
$shortcut.Save()

Write-Host "Ярлик створено: $shortcutPath"
