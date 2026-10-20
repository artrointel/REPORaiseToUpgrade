# RaiseToUpgrade

R.E.P.O. 상점에서 업그레이드 아이템을 잡고 위를 2초 동안 바라봐 무료 개인 업그레이드 하나를 선택하는 호스트 전용 모드입니다.

## 동작 방식

- 상점마다 각 플레이어는 한 번만 선택할 수 있습니다.
- 원하는 업그레이드 아이템을 잡고 시선을 수평선보다 60도 이상 위로 2초 유지하면 해당 능력치가 `+1` 됩니다.
- 손을 놓거나 시선을 내리면 2초 진행 시간이 초기화됩니다.
- 잡았던 아이템은 사라지거나 구매 처리되지 않습니다.
- 성공하면 순정 업그레이드 아이템 사용 사운드와 렌더링 이펙트가 모든 클라이언트에서 재생됩니다.
- 판정과 보상 지급은 방장이 담당합니다.
- 참가자는 모드를 설치하지 않은 순정 클라이언트여도 됩니다.

지원 항목은 체력, 스태미나, 이동 속도, 추가 점프, 구르기 관련 능력, 잡기 거리·힘·던지기, 지도 플레이어 수, 데스 헤드 배터리입니다.

세부 규칙과 검증 항목은 [planning/README.md](planning/README.md)를 참고하세요.

## 개발 환경

- R.E.P.O. (Steam)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- BepInEx 5가 설치된 R.E.P.O. 모드 프로필

Visual Studio와 `.sln` 파일은 필요하지 않습니다. VS Code 터미널에서 다음 명령만 실행하면 됩니다.

```powershell
dotnet restore
dotnet build src/RaiseToUpgrade/RaiseToUpgrade.csproj
```

### VS Code 빠른 테스트

현재 로컬 `REPO` Thunderstore 프로필을 기준으로 개발 실행이 설정되어 있습니다.

- `Ctrl+Shift+B`: Debug 빌드 후 테스트 프로필에 DLL/PDB 배포
- `Ctrl+Shift+P` → `Tasks: Run Task` → `RaiseToUpgrade: Start game`: 빌드·배포 후 모드 적용 게임 실행
- `F5` → `RaiseToUpgrade: Launch and attach`: Mono 디버그 서버로 게임을 실행하고 중단점 연결
- `RaiseToUpgrade: Watch BepInEx log`: BepInEx 로그 실시간 확인

F5 중단점 디버깅에는 VS Code의 `C# Dev Kit`과 `Unity` 확장이 필요합니다. 확장 추천 알림에서 설치할 수 있습니다. 코드 변경 후에는 실행 중인 게임을 종료하고 다시 빌드·실행해야 합니다.

로컬 게임 경로를 지정하려면 `Directory.Repo.props.example` 또는 `Directory.Repo.Gale.props.example`을 `Directory.Repo.props`로 복사한 뒤 값을 수정하세요. 이 파일은 개인 경로를 포함하므로 Git에서 제외됩니다.

정상 로드 시 BepInEx 로그에 `RaiseToUpgrade v0.1.0 loaded.`가 표시됩니다.

## Thunderstore 패키징

먼저 `Authors`, `PluginGuid`, `website_url`을 실제 배포자 정보로 바꾸고, 256x256 PNG 아이콘을 `thunderstore/icon.png`에 추가합니다.

```powershell
.\scripts\Pack-Thunderstore.ps1
```

결과 ZIP은 `artifacts/RaiseToUpgrade-<version>.zip`에 생성됩니다.

## 구조

```text
src/RaiseToUpgrade/     플러그인 소스
planning/         현재 기능 명세와 테스트 계획
thunderstore/     배포 메타데이터
scripts/          패키징 자동화
```
