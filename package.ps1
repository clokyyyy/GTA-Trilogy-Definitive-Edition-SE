<#
.SYNOPSIS
    Publishes the editor and builds the two release archives in .\release:
      GtaDeSaveEditor-v<version>-win-x64.zip  ready-to-run editor (exe, README, LICENSE)
      GtaDeSaveEditor-v<version>-src.zip      clean source tree (no bin, obj, dist, config or IDE files)
    Every path is relative to this script, which sits next to GtaDeSaveEditor.sln.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { $version = '1.0.0' }

$release = Join-Path $root 'release'
$staging = Join-Path $release 'staging'
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

if (-not $SkipTests) {
    dotnet test --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}

dotnet publish (Join-Path $root 'src\GtaDe.App') -p:PublishProfile=win-x64 --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

# Editor archive: only the exe and the docs. config\settings.json is created on first run and is personal.
$app = Join-Path $staging 'GtaDeSaveEditor'
New-Item -ItemType Directory -Force -Path $app | Out-Null
Copy-Item (Join-Path $root 'dist\GtaDeSaveEditor.exe') $app
Copy-Item (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') $app

$appZip = Join-Path $release "GtaDeSaveEditor-v$version-win-x64.zip"
if (Test-Path $appZip) { Remove-Item $appZip -Force }
Compress-Archive -Path $app -DestinationPath $appZip

# Source archive: an allow-list of top-level items, minus build output and user state inside them.
$src = Join-Path $staging 'GtaDeSaveEditor-src'
New-Item -ItemType Directory -Force -Path $src | Out-Null
$topLevel = 'src', 'tests', 'GtaDeSaveEditor.sln', 'Directory.Build.props', 'global.json',
            'README.md', 'LICENSE', '.gitignore', 'package.ps1'
$excludedDirs = 'bin', 'obj', '.vs', '.idea', '.vscode', 'config', 'Screenshots', 'TestResults', 'TestAppData'
$excludedFiles = '*.user', '*.suo', '*.code-workspace', '*.bak', '*.tmp', 'settings.json'

foreach ($item in $topLevel) {
    $from = Join-Path $root $item
    if (-not (Test-Path $from)) { continue }

    if (Test-Path $from -PathType Leaf) {
        Copy-Item $from $src
        continue
    }

    Get-ChildItem $from -Recurse -File -Force | Where-Object {
        $name = $_.Name
        $relative = $_.FullName.Substring($root.Length + 1)
        $parts = $relative.Split([IO.Path]::DirectorySeparatorChar)
        -not ($parts | Where-Object { $excludedDirs -contains $_ }) -and
        -not ($excludedFiles | Where-Object { $name -like $_ })
    } | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length + 1)
        $target = Join-Path $src $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Copy-Item $_.FullName $target
    }
}

$srcZip = Join-Path $release "GtaDeSaveEditor-v$version-src.zip"
if (Test-Path $srcZip) { Remove-Item $srcZip -Force }
Compress-Archive -Path $src -DestinationPath $srcZip

Remove-Item $staging -Recurse -Force
Get-ChildItem $release -Filter *.zip | Format-Table Name, @{ n = 'Size (MB)'; e = { [math]::Round($_.Length / 1MB, 2) } } -AutoSize
