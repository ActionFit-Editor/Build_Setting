# Build Setting (com.actionfit.buildsetting)

Android/iOS 빌드 설정을 `BuildSettingsSO`에 저장하고 Unity `PlayerSettings`와 실제 빌드 실행에 적용하는 에디터 패키지입니다.

## 설치

```json
{
  "dependencies": {
    "com.actionfit.buildsetting": "https://github.com/ActionFit-Editor/Build_Setting.git#1.1.1"
  }
}
```

## 구성

- 메뉴: `Tools > ActionFit > Build Setting`
- 설정 에셋: `BuildSettingsSO`
- 패키지에는 설정 에셋을 저장하지 않습니다. 각 프로젝트의 `Assets` 폴더 아래에 `Create New`로 생성해서 사용합니다.

## 자동 빌드 연동

BuildCommit, `.build/build_request.json`, Git tag 기반 CI 트리거, GitHub Actions workflow template, macOS self-hosted runner 가이드는 `com.actionfit.buildautomation` 패키지로 분리되었습니다.

자동 빌드를 사용하려면 Build Setting과 함께 Build Automation을 설치합니다. Build Setting은 `BuildSettingsSO`, Android/iOS PlayerSettings 적용, 로컬 빌드 실행 API를 제공하고, Build Automation은 원격 빌드 요청과 CI workflow를 담당합니다.

`BuildSettingsSO`에는 BuildCommit 실험용 request override 값도 저장할 수 있습니다. Google Play service account JSON, App Store Connect API key id, issuer id, P8 값을 BuildCommit 창에서 입력하면 같은 SO에 임시 저장됩니다.

## 선택 연동

- `com.actionfit.customsymbols`가 설치되어 있으면 asmdef `versionDefines`로 `ACTIONFIT_CUSTOM_SYMBOLS`가 켜지고, 빌드 전 심볼 확인/빌드 심볼 적용 흐름을 지원합니다.
- Google Play Games 플러그인이 있으면 Android 빌드 설정 적용 시 GPGS 설정 동기화를 reflection으로 수행합니다. 플러그인이 없으면 경고만 출력하고 건너뜁니다.
