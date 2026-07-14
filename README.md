# Build Setting (com.actionfit.buildsetting)

Android/iOS 빌드 설정을 `BuildSettingsSO`에 저장하고 Unity `PlayerSettings`와 실제 빌드 실행에 적용하는 에디터 패키지입니다.

## 설치

```json
{
  "dependencies": {
    "com.actionfit.buildsetting": "https://github.com/ActionFit-Editor/Build_Setting.git#1.1.10"
  }
}
```

## Agent Skills

Custom Package Manager의 `Install or Refresh Agent Skills`를 실행하면 Codex와 Claude에 read-only `build-settings-help`가 설치됩니다.

이 스킬은 `BuildSettingsSO`, Android/iOS `PlayerSettings`, 회사 프로필, Addressables prebuild, 선택 연동과 Build Automation 경계를 설명합니다. Unity 창이나 빌드를 실행하고 설정 에셋, `PlayerSettings`, credential, manifest, `.gitignore`, 게시 상태를 변경하지 않습니다.

## Unity Menu

- Package root: `Tools > Package > Build Setting`.
- README: `Tools > Package > Build Setting > README`.
- Setting SO: `Tools > Package > Build Setting > Setting SO`.
- Package commands stay under the same package root and appear above the separated README/Setting SO entries when those entries exist.

## 구성

- 설정 창 메뉴: `Tools > Package > Build Setting > Setting Window`
- 설정 SO 포커싱 메뉴: `Tools > Package > Build Setting > Setting SO`
- 설정 에셋: `BuildSettingsSO`
- 회사/Team ID 프로필 세팅 에셋: `BuildCompanySettingsSO`
- 패키지에는 설정 에셋을 저장하지 않습니다. 기존 `BuildSettingsSO`가 있으면 자동으로 찾아서 창 필드에 배정하고, 없으면 `Assets/_Data/_BuildSetting/BuildSettingsSO.asset`을 자동 생성합니다.
- `BuildCompanySettingsSO`는 `Assets/_Data/_BuildSetting/BuildCompanySettingsSO.asset`에 생성되며, public 패키지에서는 특정 회사 프로필을 자동 추가하지 않습니다.
- `BuildSettingsSO`에서 `BuildCompanySettingsSO`를 드래그앤드롭으로 연결하고 `Company Profile`을 선택하면 `companyName`과 iOS `Development Team ID`가 함께 세팅됩니다. `Custom / Add Company`는 회사명/Team ID를 입력해 `BuildCompanySettingsSO`에 새 프로필을 저장하는 창을 열고, `Custom / Manual`은 프로필 자동 매칭을 끄고 현재 BuildSettingsSO의 회사명과 Team ID를 직접 입력합니다. 저장된 회사 프로필을 선택한 상태에서는 수동 입력 필드를 숨깁니다.
- 회사별 기본 프로필 자동 세팅은 별도 전용 bootstrap 패키지가 담당합니다.
- 처음 자동 생성되는 `BuildSettingsSO`는 현재 프로젝트의 `PlayerSettings`에서 company name, product name, bundle version, Android/iOS application identifier, bundle number, iOS target OS version 같은 기본값을 1차 초기화로 가져옵니다.
- iOS의 `Target iOS Version` 값은 Unity `PlayerSettings.iOS.targetOSVersionString`과 Xcode `IPHONEOS_DEPLOYMENT_TARGET`에 적용됩니다. 기본값은 기존 동작과 같은 `13.0`입니다.
- iOS `Associated Domains`는 `BuildSettingsSO`의 리스트에 `applinks:actionfit.sng.link`처럼 입력합니다. iOS Xcode post process가 entitlements에 자동 반영하지만, Apple Developer Portal의 App ID와 provisioning profile에도 Associated Domains capability가 활성화되어 있어야 실제 서명/Universal Links가 동작합니다.

## 자동 빌드 연동

BuildCommit, `.build/build_request.json`, Git tag 기반 CI 트리거, GitHub Actions workflow template, macOS self-hosted runner 가이드는 `com.actionfit.buildautomation` 패키지로 분리되었습니다.

자동 빌드를 사용하려면 Build Setting과 함께 Build Automation을 설치합니다. Build Setting은 `BuildSettingsSO`, Android/iOS PlayerSettings 적용, 로컬 빌드 실행 API를 제공하고, Build Automation은 `Tools > Package > Build Automation > AutoBuild`에서 원격 빌드 요청과 CI workflow를 담당합니다.

`BuildSettingsSO`에는 BuildCommit 실험용 request override 값도 저장할 수 있습니다. Google Play service account JSON, App Store Connect API key id, issuer id, P8 값을 BuildCommit 창에서 입력하면 같은 SO에 임시 저장됩니다.

## 선택 연동

- `com.actionfit.customsymbols`가 설치되어 있으면 asmdef `versionDefines`로 `ACTIONFIT_CUSTOM_SYMBOLS`가 켜지고, 빌드 전 심볼 확인/빌드 심볼 적용 흐름을 지원합니다.
- Google Play Games 플러그인이 있으면 Android 빌드 설정 적용 시 GPGS 설정 동기화를 reflection으로 수행합니다. 플러그인이 없으면 경고만 출력하고 건너뜁니다.
