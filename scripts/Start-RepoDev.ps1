[CmdletBinding()]
param(
    [switch]$EnableDebugger
)

$ErrorActionPreference = "Stop"

$gameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\REPO"
$gameExecutable = Join-Path $gameDirectory "REPO.exe"
$profileDirectory = Join-Path $env:APPDATA "Thunderstore Mod Manager\DataFolder\REPO\profiles\REPO"
$preloader = Join-Path $profileDirectory "BepInEx\core\BepInEx.Preloader.dll"
$doorstopProxy = Join-Path $gameDirectory "winhttp.dll"

foreach ($requiredFile in @($gameExecutable, $preloader, $doorstopProxy)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required development file was not found: $requiredFile"
    }
}

if (Get-Process -Name "REPO" -ErrorAction SilentlyContinue) {
    throw "R.E.P.O. is already running. Close it before rebuilding or starting another test."
}

$launchArguments = @(
    "--doorstop-enabled", "true",
    "--doorstop-target-assembly", "`"$preloader`""
)

if ($EnableDebugger) {
    $launchArguments += @(
        "--doorstop-mono-debug-enabled", "true",
        "--doorstop-mono-debug-suspend", "true",
        "--doorstop-mono-debug-address", "127.0.0.1:10000"
    )
}

Start-Process `
    -FilePath $gameExecutable `
    -WorkingDirectory $gameDirectory `
    -ArgumentList $launchArguments

if ($EnableDebugger) {
    Write-Host "R.E.P.O. started with the Mono debugger at 127.0.0.1:10000."
}
else {
    Write-Host "R.E.P.O. started with the Thunderstore REPO profile."
}
