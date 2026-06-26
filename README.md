# Build Setting (com.actionfit.buildsetting)

Android/iOS 빌드 설정을 `BuildSettingsSO`에 저장하고 Unity `PlayerSettings`와 실제 빌드 실행에 적용하는 에디터 패키지입니다.

## 설치

```json
{
  "dependencies": {
    "com.actionfit.buildsetting": "https://github.com/ActionFit-Editor/Build_Setting.git#1.0.4"
  }
}
```

## 구성

- 메뉴: `Tools > ActionFit > Build Setting`
- 빌드 커밋 메뉴: `Tools > ActionFit > Build Commit`
- 설정 에셋: `BuildSettingsSO`
- 패키지에는 설정 에셋을 저장하지 않습니다. 각 프로젝트의 `Assets` 폴더 아래에 `Create New`로 생성해서 사용합니다.

## Build Commit

`Build Commit` 창은 `BuildSettingsSO`의 버전과 번들 번호를 `PlayerSettings`에 적용한 뒤, `.build/build_request.json`을 생성하고 `[Auto]build: v{version}({bundleNo})` 형식의 메시지로 `git add .`, `git commit`, `git push`를 실행합니다.

자동 빌드 트리거는 `Build Commit`에서만 생성합니다. `Build Setting` 창의 빌드 버튼은 로컬 수동 빌드 용도이며 원격 빌드 요청 파일을 만들지 않습니다.

## CI Build

원격 빌드머신은 `Build Commit`이 커밋에 포함시킨 `.build/build_request.json`을 읽어 같은 `BuildSettingsSO` 기반 빌드를 재현합니다. `CIBuildEntry`는 request의 `triggerSource`가 `BuildCommit`인 경우만 처리합니다.

```bash
Unity -batchmode -quit -projectPath . -executeMethod ActionFit.BuildSetting.Editor.CIBuildEntry.BuildFromRequest
```

GitHub Actions나 다른 CI는 플랫폼별 Unity 실행 환경을 준비한 뒤 위 메서드를 호출합니다. Google Play 또는 TestFlight 업로드는 workflow 단계에서 `BuildRequest.uploadTarget` 값을 기준으로 후속 처리합니다.

기본 GitHub Actions workflow는 `.github/workflows/buildcommit-auto-build.yml`에 있으며, `dev_jewoo`에 push된 `[Auto]build:` 커밋과 `triggerSource=BuildCommit` request만 처리합니다. 현재 workflow는 Android artifact 빌드까지만 수행하며, 스토어 업로드는 별도 단계에서 추가합니다.

필수 GitHub Secrets:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Unity Pro/serial 라이선스를 쓰는 경우 `UNITY_SERIAL`도 함께 등록합니다.

Android signing과 Google Play 업로드, iOS 인증서와 TestFlight 업로드 secret은 업로드 단계 추가 시 별도로 설정합니다.

## 선택 연동

- `com.actionfit.customsymbols`가 설치되어 있으면 asmdef `versionDefines`로 `ACTIONFIT_CUSTOM_SYMBOLS`가 켜지고, 빌드 전 심볼 확인/빌드 심볼 적용 흐름을 지원합니다.
- Google Play Games 플러그인이 있으면 Android 빌드 설정 적용 시 GPGS 설정 동기화를 reflection으로 수행합니다. 플러그인이 없으면 경고만 출력하고 건너뜁니다.
