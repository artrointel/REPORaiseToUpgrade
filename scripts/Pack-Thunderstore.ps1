[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "src\RaiseToUpgrade\RaiseToUpgrade.csproj"
$metadataDirectory = Join-Path $projectRoot "thunderstore"
$iconPath = Join-Path $metadataDirectory "icon.png"
$localDotnet = Join-Path $projectRoot ".dotnet\dotnet.exe"
$dotnetCommand = if (Test-Path -LiteralPath $localDotnet -PathType Leaf) {
    $localDotnet
}
else {
    (Get-Command dotnet -ErrorAction SilentlyContinue).Source
}

if ([string]::IsNullOrWhiteSpace($dotnetCommand)) {
    throw ".NET 9 SDK is required: https://dotnet.microsoft.com/download/dotnet/9.0"
}

if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "Add a 256x256 PNG icon at thunderstore\icon.png."
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "The Thunderstore icon must be exactly 256x256 pixels. Current size: $($icon.Width)x$($icon.Height)"
    }
}
finally {
    $icon.Dispose()
}

[xml]$project = Get-Content -LiteralPath $projectFile -Encoding UTF8
$versionNode = $project.SelectSingleNode("/Project/PropertyGroup/Version")
$version = if ($null -eq $versionNode) { "" } else { $versionNode.InnerText.Trim() }
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Version was not found in RaiseToUpgrade.csproj."
}

$authorNode = $project.SelectSingleNode("/Project/PropertyGroup/Authors")
$author = if ($null -eq $authorNode) { "" } else { $authorNode.InnerText.Trim() }
if ([string]::IsNullOrWhiteSpace($author)) {
    throw "Set Authors in RaiseToUpgrade.csproj to the actual Thunderstore team or author name."
}

$manifestPath = Join-Path $metadataDirectory "manifest.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.version_number -ne $version) {
    throw "Version mismatch: csproj=$version, manifest=$($manifest.version_number)"
}
if ([string]::IsNullOrWhiteSpace($manifest.website_url)) {
    throw "website_url is required in manifest.json."
}

$pluginSource = Get-Content -LiteralPath (Join-Path $projectRoot "src\RaiseToUpgrade\Plugin.cs") -Raw -Encoding UTF8
$pluginVersionMatch = [regex]::Match($pluginSource, 'PluginVersion\s*=\s*"(?<version>[^"]+)"')
$pluginVersion = $pluginVersionMatch.Groups["version"].Value
if (-not $pluginVersionMatch.Success -or $pluginVersion -ne $version) {
    throw "Version mismatch: csproj=$version, Plugin.cs=$pluginVersion"
}
& $dotnetCommand build $projectFile --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "The build failed."
}

$dllPath = Join-Path $projectRoot "src\RaiseToUpgrade\bin\$Configuration\netstandard2.1\RaiseToUpgrade.dll"
if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "The build output DLL was not found: $dllPath"
}

$artifactsDirectory = Join-Path $projectRoot "artifacts"
$stagingDirectory = Join-Path $artifactsDirectory "thunderstore-staging"
$packagePath = Join-Path $artifactsDirectory "RaiseToUpgrade-$version.zip"

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

$pluginDirectory = Join-Path $stagingDirectory "BepInEx\plugins\RaiseToUpgrade"
New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
Copy-Item -LiteralPath $dllPath -Destination $pluginDirectory
Copy-Item -LiteralPath $manifestPath -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $metadataDirectory "README.md") -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $metadataDirectory "CHANGELOG.md") -Destination $stagingDirectory
Copy-Item -LiteralPath $iconPath -Destination $stagingDirectory

Compress-Archive -Path (Join-Path $stagingDirectory "*") -DestinationPath $packagePath
Write-Host "Thunderstore package created: $packagePath"
