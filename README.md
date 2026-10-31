# RaiseToUpgrade

RaiseToUpgrade is a host-only R.E.P.O. mod that lets every player claim one free personal upgrade in each shop.

## How it works

- Grab a shop upgrade item and look at least 60 degrees above the horizon.
- Hold both conditions continuously for two seconds to increase that stat by `+1`.
- The original item remains available and the free claim does not affect its current or future price.
- Each player may claim once per shop.
- A successful claim plays the vanilla upgrade sound and visual effect, then releases the held item.
- Only the host installs the mod. Unmodded players receive the same gameplay result and vanilla feedback.

Supported upgrades include health, stamina, sprint speed, extra jump, tumble abilities, grab range, grab strength, grab throw, map player count, and Death Head battery.

## Requirements

- R.E.P.O. 0.5.0 on Steam
- A Thunderstore-compatible mod manager profile with BepInEx
- .NET 9 SDK for local development
- Visual Studio Code with the recommended C# extensions for breakpoint debugging

Visual Studio and a solution (`.sln`) file are not required.

## Build

```powershell
dotnet restore src/RaiseToUpgrade/RaiseToUpgrade.csproj
dotnet build src/RaiseToUpgrade/RaiseToUpgrade.csproj
```

When a local game installation is found, builds reference its current managed assemblies directly. Private game members are publicized only in intermediate build references; installed game files are never modified or packaged. If no game is installed, the project falls back to the published 0.4.4 reference package, which is not evidence of 0.5.0 runtime compatibility. Set `-p:UseInstalledGameAssemblies=false` to explicitly use that fallback.

The R.E.P.O. modding SDK can deploy the build to a detected development profile. If automatic detection does not match your setup, copy one of the example property files to the ignored `Directory.Repo.props` file and update its paths:

- `Directory.Repo.props.example` for r2modman
- `Directory.Repo.Gale.props.example` for Gale

## Test from VS Code

- `Ctrl+Shift+B`: build the Debug configuration and deploy it to the configured profile.
- Run `RaiseToUpgrade: Start game`: build, deploy, and launch the game.
- Press `F5`: build, launch with the Mono debugger enabled, and attach VS Code.
- Run `RaiseToUpgrade: Watch BepInEx log`: follow the configured profile log.

The included launch script assumes the default Steam installation and a Thunderstore Mod Manager profile named `REPO`. Update `scripts/Start-RepoDev.ps1` and `.vscode/tasks.json` if your paths or profile name differ. Close the game before rebuilding or starting another debug session.

## Package for Thunderstore

Keep the version synchronized in these files:

- `src/RaiseToUpgrade/RaiseToUpgrade.csproj`
- `src/RaiseToUpgrade/Plugin.cs`
- `thunderstore/manifest.json`

Packaging accepts only Release builds and does not deploy to the game profile. Add `-NoRestore` after restoring dependencies to package offline.

Then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Pack-Thunderstore.ps1
```

The upload-ready archive is created at `artifacts/RaiseToUpgrade-<version>.zip`.

## Compatibility checks

Build Debug and Release without deploying, then run the automated checks:

```powershell
dotnet build src/RaiseToUpgrade/RaiseToUpgrade.csproj -c Debug -p:CopyFilesToPluginOutputDirectoryOnBuild=false
dotnet build src/RaiseToUpgrade/RaiseToUpgrade.csproj -c Release -p:CopyFilesToPluginOutputDirectoryOnBuild=false
dotnet run --project tests/RaiseToUpgrade.Checks/RaiseToUpgrade.Checks.csproj
```

Append `-- "<game managed directory>"` to the last command for a non-default Steam installation. These checks inspect the installed game API and native network contracts; they do not replace a host/unmodded-client gameplay test. See [the 0.5.0 compatibility review](planning/compatibility-0.5.0.md).

## Project layout

```text
src/RaiseToUpgrade/    Plugin source and project file
planning/              Current specification and manual test plan
scripts/               Local launch and packaging automation
tests/                 Native packet and installed-game compatibility checks
thunderstore/          Thunderstore metadata and icon
.vscode/               VS Code build and debug tasks
```

See [planning/README.md](planning/README.md) for the implementation contract and [planning/test-plan.md](planning/test-plan.md) for release verification.
