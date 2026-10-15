# RepoHell

R.E.P.O.용 BepInEx 5 / HarmonyX 모드 프로젝트입니다. 현재는 플러그인 로드와 Thunderstore 패키징까지 가능한 최소 골격만 포함합니다.

## 준비물

- R.E.P.O. (Steam)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Gale](https://github.com/Kesomannen/gale) 또는 r2modman/Thunderstore Mod Manager의 별도 개발 프로필
- 개발 프로필에 설치한 `BepInEx-BepInExPack`

## 최초 설정

1. 모드 매니저에서 R.E.P.O.용 `Mod Dev` 프로필을 만듭니다.
2. 해당 프로필에 `BepInExPack`을 설치하고 한 번 `Start modded`로 실행합니다.
3. r2modman은 `Directory.Repo.props.example`, Gale은 `Directory.Repo.Gale.props.example`을 `Directory.Repo.props`로 복사합니다.
4. 복사한 파일의 `GameDirectory`, `ProfileName`, `BepInExDirectory`를 로컬 환경에 맞게 수정합니다.
5. 아래 TODO를 실제 값으로 바꿉니다.
   - `src/RepoHell/RepoHell.csproj`: `Authors`, `Description`
   - `src/RepoHell/Plugin.cs`: `PluginGuid`
   - `thunderstore/manifest.json`: `website_url`, `description`

`Directory.Repo.props`에는 사용자 경로가 들어가므로 Git에서 제외됩니다.

## 개발

```powershell
dotnet restore
dotnet build
```

R.E.P.O. Modding SDK가 설정된 BepInEx 프로필을 찾으면 빌드된 DLL을 프로필의 `BepInEx\plugins` 아래로 자동 배포합니다. `dotnet run --project src/RepoHell/RepoHell.csproj`로 모드가 적용된 게임을 실행할 수도 있습니다.

정상 로드 여부는 BepInEx 콘솔 또는 `BepInEx\LogOutput.log`의 다음 메시지로 확인합니다.

```text
RepoHell v0.1.0 loaded.
```

## Thunderstore 패키지

1. 256x256 PNG 아이콘을 `thunderstore/icon.png`에 추가합니다.
2. `RepoHell.csproj`, `Plugin.cs`, `manifest.json`의 버전을 동일하게 맞춥니다.
3. 패키징 스크립트를 실행합니다.

```powershell
.\scripts\Pack-Thunderstore.ps1
```

업로드용 ZIP은 `artifacts/RepoHell-<version>.zip`에 생성됩니다. ZIP 루트에는 `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `BepInEx/`가 들어갑니다.

## 구조

```text
src/RepoHell/             C# 플러그인 소스
thunderstore/             배포 메타데이터와 패키지 문서
scripts/                  로컬 빌드/패키징 자동화
Directory.Repo.props.*    개인별 게임 및 모드 프로필 경로
```
