[CmdletBinding()]
param(
    [ValidateSet("Release")]
    [string]$Configuration = "Release",
    [switch]$NoRestore
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
    throw "Add a 256x256 PNG Thunderstore icon at thunderstore\icon.png."
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
if ([string]::IsNullOrWhiteSpace($author) -or $author -like "TODO*") {
    throw "Set Authors in RaiseToUpgrade.csproj to your Thunderstore team/author name."
}

$manifestPath = Join-Path $metadataDirectory "manifest.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.version_number -ne $version) {
    throw "Version mismatch: csproj=$version, manifest=$($manifest.version_number)"
}
if ($manifest.website_url -like "*TODO*") {
    throw "Set website_url in manifest.json to the actual project URL."
}

$pluginSource = Get-Content -LiteralPath (Join-Path $projectRoot "src\RaiseToUpgrade\Plugin.cs") -Raw -Encoding UTF8
$pluginVersionMatch = [regex]::Match($pluginSource, 'PluginVersion\s*=\s*"(?<version>[^"]+)"')
if (-not $pluginVersionMatch.Success -or $pluginVersionMatch.Groups["version"].Value -ne $version) {
    throw "Version mismatch: csproj=$version, Plugin.cs=$($pluginVersionMatch.Groups['version'].Value)"
}
if ($pluginSource -like "*TODO_AUTHOR*") {
    throw "Set PluginGuid in Plugin.cs to your author identifier."
}

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path $projectRoot ".nuget\packages"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$buildArguments = @("build", $projectFile, "--configuration", $Configuration, "-p:CopyFilesToPluginOutputDirectoryOnBuild=false")
if ($NoRestore) {
    $buildArguments += "--no-restore"
}
& $dotnetCommand @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$dllPath = Join-Path $projectRoot "src\RaiseToUpgrade\bin\$Configuration\netstandard2.1\RaiseToUpgrade.dll"
if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Build output DLL was not found: $dllPath"
}

$artifactsDirectory = Join-Path $projectRoot "artifacts"
$stagingDirectory = Join-Path $artifactsDirectory "thunderstore-staging"
$packagePath = Join-Path $artifactsDirectory "RaiseToUpgrade-$version.zip"

# Check absolute targets before removing a previous staging directory or package.
$artifactRoot = [IO.Path]::GetFullPath($artifactsDirectory).TrimEnd('\') + '\'
foreach ($cleanupPath in @($stagingDirectory, $packagePath)) {
    $resolvedPath = [IO.Path]::GetFullPath($cleanupPath)
    if (-not $resolvedPath.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup target is outside the artifacts directory: $resolvedPath"
    }
}

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

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($packagePath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $stagingDirectory -File -Recurse) {
        # ZIP entry paths use forward slashes on every platform.
        $entryName = $file.FullName.Substring($stagingDirectory.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $archive.Dispose()
}
Write-Host "Thunderstore package created: $packagePath"
