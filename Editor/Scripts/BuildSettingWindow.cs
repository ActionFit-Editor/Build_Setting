#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionFit.SOSingleton;
using ActionFit.SOSingleton.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Serialization;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.

namespace ActionFit.BuildSetting.Editor
{
    [CreateAssetMenu(fileName = "BuildSettings", menuName = "Build/BuildSettings")]
    [ActionFitSettingsAsset("BuildSetting", ActionFitSettingsAssetLifetime.EditorOnly)]
    public class BuildSettingsSO : ScriptableObject, IActionFitSettingsAssetInitializer
    {
        #region Fields

        public const string SOPrefsKey = "LastUsedBuildSettings";
        public const string DefaultSettingsAssetPath = "Assets/_Data/_BuildSetting/BuildSettingsSO.asset";
        public const string DefaultIosTargetOSVersion = "13.0";

        // Android
        public string buildFileName = "[Enter Build File Name]"; // 빌드 파일명
        public string androidBuildPath = "[Enter Android Build Path]"; // Android 빌드 경로
        public bool autoSearchPackageName = true; // 패키지명 자동 탐색 (Firebase)
        public string androidPackageName = "[Enter Android Package Name]"; // Android 패키지명
        public bool autoSearchKeystore = false; // 키스토어 자동 탐색
        public string keyStorePath = "[Enter Keystore Path]"; // 키스토어 경로
        public bool editablePassword = false; // 비밀번호 편집 활성화
        public string keystorePassword = "[Enter KeyStore Password]"; // 키스토어 비밀번호
        public bool autoSearchAlias = false; // Alias 자동 탐색
        public string keyStoreAlias = "[Enter Keystore Alias]"; // 키스토어 Alias
        public string aliasPassword = "[Enter Alias Password]"; // Alias 비밀번호
        public bool settingGPGS = true; // GPGS 설정 활성화
        public string app_id = "[Enter GPGS App ID]"; // GPGS App ID
        public string resourcesDefinition; // GPGS Resources Definition XML
        public string clientID = "[Enter GPGS Client ID]"; // GPGS Client ID

        // iOS
        public string iosBuildPath = "[Enter iOS Build Path]"; // iOS 빌드 경로
        public string iosPackageName = "[Enter iOS Bundle ID]"; // iOS 패키지명
        public string developmentTeamId = "[Enter Apple Team ID]"; // Apple Developer Team ID
        public string iosTargetOSVersion = DefaultIosTargetOSVersion; // iOS Deployment Target
        public bool targetIPhone = true; // iPhone 타겟
        public bool targetIPad = true; // iPad 타겟
        public bool useGameCenter = false; // Game Center
        public bool usePushNotifications = false; // Background Mode (Remote Notifications) 자동 포함
        public bool useICloud = false; // iCloud (Key-value storage)
        public List<string> associatedDomains = new(); // iOS Associated Domains entitlements (e.g. applinks:example.com)
        public List<string> addFrameworks = new(); // 추가 프레임워크 목록
        public List<string> removeFrameworks = new(); // UnityFramework에서 제거할 라이브러리 목록

        // Common
        [FormerlySerializedAs("actionFitBuildSetting")]
        public BuildCompanySettingsSO companySettings; // 회사/Team ID 기본 세팅
        public bool useManualCompanyProfile = false; // Company Profile 자동 매칭 대신 수동 입력 사용
        public string companyName = "[Enter Company Name]"; // 회사명
        public string productName = "[Enter Product Name]"; // 앱 이름
        public bool saveFileInProject = false; // 프로젝트 내 Builds 폴더에 저장
        public string buildVersion = "[Enter Build Version]"; // 빌드 버전
        public string bundleNo = "[Enter Bundle Number]"; // 번들 번호
        public bool developmentBuild = false; // Unity Development Build 활성화
        public bool isDevMode = false; // 개발 모드
        public List<string> defineSymbol = new(); // 추가 심볼
        public bool manageSymbolsOnBuild = true; // 빌드 시 심볼 관리 (CustomSymbols 패키지 설치 시)

        // BuildCommit experimental request overrides
        [TextArea(3, 10)] public string buildCommitGooglePlayServiceAccountJson = ""; // BuildCommit Google Play service account JSON override
        public string buildCommitAppStoreConnectApiKeyId = ""; // BuildCommit App Store Connect API key ID override
        public string buildCommitAppStoreConnectIssuerId = ""; // BuildCommit App Store Connect issuer ID override
        [TextArea(3, 10)] public string buildCommitAppStoreConnectApiKeyP8 = ""; // BuildCommit App Store Connect private key override

        /// <summary>
        /// 기존 빌드 옵션을 보존하면서 Development Build 설정을 적용합니다.
        /// </summary>
        public BuildOptions ResolveBuildOptions(BuildOptions baseOptions)
        {
            return developmentBuild ? baseOptions | BuildOptions.Development : baseOptions;
        }

#if UNITY_EDITOR
        public static BuildSettingsSO FindSettingsAsset()
        {
            var saved = LoadAndRemember(EditorPrefs.GetString(SOPrefsKey, ""));
            if (saved != null) return saved;

            var result = ActionFitSettingsAssetProvider.Resolve(typeof(BuildSettingsSO), false);
            return LoadAndRemember(result.ActualPath);
        }

        public static BuildSettingsSO FindOrCreateSettingsAsset()
        {
            var settings = ActionFitSettingsAssetProvider.GetOrCreate<BuildSettingsSO>();
            return settings == null
                ? null
                : LoadAndRemember(AssetDatabase.GetAssetPath(settings));
        }

        public void InitializeNewSettingsAsset()
        {
            InitializeFromProjectSettings();
        }

        public void InitializeFromProjectSettings()
        {
            EnsureCompanySettings();

            string playerCompanyName = PlayerSettings.companyName;
            if (!string.IsNullOrWhiteSpace(playerCompanyName))
                companyName = playerCompanyName;

            string playerProductName = PlayerSettings.productName;
            if (!string.IsNullOrWhiteSpace(playerProductName))
            {
                productName = playerProductName;
                buildFileName = SanitizeFileName(playerProductName);
            }

            if (!string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
                buildVersion = PlayerSettings.bundleVersion;

            string androidId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (!string.IsNullOrWhiteSpace(androidId))
                androidPackageName = androidId;

            string iosId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            if (!string.IsNullOrWhiteSpace(iosId))
                iosPackageName = iosId;

#if UNITY_ANDROID
            int androidBundleCode = PlayerSettings.Android.bundleVersionCode;
            if (androidBundleCode > 0)
                bundleNo = androidBundleCode.ToString();
#elif UNITY_IOS
            if (!string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber))
                bundleNo = PlayerSettings.iOS.buildNumber;
            if (!string.IsNullOrWhiteSpace(PlayerSettings.iOS.appleDeveloperTeamID))
                developmentTeamId = PlayerSettings.iOS.appleDeveloperTeamID;
            if (!string.IsNullOrWhiteSpace(PlayerSettings.iOS.targetOSVersionString))
                iosTargetOSVersion = PlayerSettings.iOS.targetOSVersionString;
#endif
            SyncDevelopmentTeamIdFromCompanyProfile();
        }

        public string GetResolvedIosTargetOSVersion()
        {
            return ResolveIosTargetOSVersion(iosTargetOSVersion);
        }

        public bool EnsureCompanySettings()
        {
            bool changed = false;
            if (companySettings == null)
            {
                companySettings = BuildCompanySettingsSO.FindOrCreateSettingsAsset();
                changed = companySettings != null;
            }

            return changed;
        }

        public string GetResolvedCompanyName()
        {
            return string.IsNullOrWhiteSpace(companyName) ? "" : companyName.Trim();
        }

        public string GetResolvedDevelopmentTeamId()
        {
            if (!useManualCompanyProfile &&
                companySettings != null &&
                companySettings.TryGetDevelopmentTeamId(companyName, out string matchedTeamId))
                return matchedTeamId;

            return string.IsNullOrWhiteSpace(developmentTeamId) ? "" : developmentTeamId.Trim();
        }

        public string[] GetResolvedAssociatedDomains()
        {
            if (associatedDomains == null) return Array.Empty<string>();

            return associatedDomains
                .Where(domain => !string.IsNullOrWhiteSpace(domain))
                .Select(domain => domain.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public bool SyncDevelopmentTeamIdFromCompanyProfile()
        {
            if (useManualCompanyProfile) return false;
            if (companySettings == null) return false;
            if (!companySettings.TryGetDevelopmentTeamId(companyName, out string matchedTeamId)) return false;
            if (string.Equals(developmentTeamId, matchedTeamId, StringComparison.Ordinal)) return false;

            developmentTeamId = matchedTeamId;
            return true;
        }

        public static string ResolveIosTargetOSVersion(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DefaultIosTargetOSVersion : value.Trim();
        }

        private static BuildSettingsSO LoadAndRemember(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var settings = AssetDatabase.LoadAssetAtPath<BuildSettingsSO>(path);
            if (settings != null)
            {
                EditorPrefs.SetString(SOPrefsKey, path);
                bool changed = settings.EnsureCompanySettings();
                changed |= settings.SyncDevelopmentTeamIdFromCompanyProfile();
                if (changed)
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
            return settings;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Build";

            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Trim()
                .Select(c => invalid.Contains(c) ? '_' : c)
                .ToArray();
            string sanitized = new string(chars).Trim('_', ' ');
            return string.IsNullOrWhiteSpace(sanitized) ? "Build" : sanitized;
        }
#endif

        #endregion
    }

    public class BuildSettingsWindow : EditorWindow
    {
        #region Fields

        internal const string SOPrefsKey = BuildSettingsSO.SOPrefsKey;

        [SerializeField] private BuildSettingsSO settings;
        private Vector2 _scrollPosition = Vector2.zero;
        private SerializedObject _serializedSettings;

        #endregion

        #region Window

        [MenuItem("Tools/Package/Build Setting/Setting Window", false, 20)]
        public static void ShowWindow()
        {
            BuildSettingsWindow window = GetWindow<BuildSettingsWindow>("Build Settings");
            window.Show();
        }

        [MenuItem("Tools/Package/Build Setting/Setting SO", false, 900)]
        public static void FocusSettingsAsset()
        {
            var asset = BuildSettingsSO.FindOrCreateSettingsAsset();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.FocusProjectWindow();
        }

        private void OnEnable()
        {
            _scrollPosition = Vector2.zero;
            settings = BuildSettingsSO.FindOrCreateSettingsAsset();
            if (settings != null)
            {
                EnsureCompanySettingsReference();
                _serializedSettings = new SerializedObject(settings);
                EditorPrefs.SetString(SOPrefsKey, AssetDatabase.GetAssetPath(settings));
                AutoSearchPackageNameOnEnable();
            }
        }

        // Editor 창 열 때 패키지명이 아직 초기값이면 1회 자동 탐색
        private void AutoSearchPackageNameOnEnable()
        {
            if (settings == null || !settings.autoSearchPackageName) return;

#if UNITY_IOS
            const string packageNamePropertyName = "iosPackageName";
#else
            const string packageNamePropertyName = "androidPackageName";
#endif
            var packageNameProp = _serializedSettings.FindProperty(packageNamePropertyName);
            if (!IsPlaceholderOrEmpty(packageNameProp.stringValue)) return;

            string detectedId = GetPackageNameFromFirebaseConfig(silent: true);
            if (string.IsNullOrEmpty(detectedId)) return;

            packageNameProp.stringValue = detectedId;
            _serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=cyan><b>[Firebase Sync]</b></color> Package ID auto-updated on open: {detectedId}");
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // BuildSettings SO 필드
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Build Settings Asset");

            EditorGUI.BeginChangeCheck();
            settings = (BuildSettingsSO)EditorGUILayout.ObjectField(
                settings,
                typeof(BuildSettingsSO),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                if (settings != null)
                {
                    EnsureCompanySettingsReference();
                    _serializedSettings = new SerializedObject(settings);
                    EditorPrefs.SetString(SOPrefsKey, AssetDatabase.GetAssetPath(settings));
                }
                else
                {
                    _serializedSettings = null;
                }
            }

            if (GUILayout.Button("Create New", GUILayout.Width(80)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Build Settings",
                    "BuildSettings",
                    "asset",
                    "Please enter a file name to save the build settings to"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    var newSettings = CreateInstance<BuildSettingsSO>();
                    newSettings.InitializeFromProjectSettings();
                    AssetDatabase.CreateAsset(newSettings, path);
                    AssetDatabase.SaveAssets();
                    settings = newSettings;
                    EnsureCompanySettingsReference();
                    _serializedSettings = new SerializedObject(settings);
                    EditorPrefs.SetString(SOPrefsKey, path);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Please assign or create a Build Settings asset to configure build options.",
                    MessageType.Warning);

                settings = BuildSettingsSO.FindOrCreateSettingsAsset();
                if (settings != null)
                {
                    EnsureCompanySettingsReference();
                    _serializedSettings = new SerializedObject(settings);
                    EditorPrefs.SetString(SOPrefsKey, AssetDatabase.GetAssetPath(settings));
                }

                return;
            }

            _serializedSettings?.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            DrawBuildInfo();
            DrawPathSettings();
            DrawPackageSettings();
            DrawVersionSettings();
#if UNITY_ANDROID
            DrawGpgsSettings();
            DrawKeyStoreSettings();
#endif
            DrawBuildOptions();
#if ACTIONFIT_CUSTOM_SYMBOLS
            DrawSymbolManageOption();
#endif
#if UNITY_IOS
            DrawiOSCapabilities();
#endif
            DrawBuildButtons();

            EditorGUILayout.EndScrollView();

            ApplyAndSaveIfModified();
        }

        #endregion

        #region Draw Methods

        private void DrawBuildInfo()
        {
#if UNITY_ANDROID
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("buildFileName"));
#endif
            DrawCompanySettings();
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("productName"));
            EditorGUILayout.Space(5);
        }

        private void DrawCompanySettings()
        {
            var buildSettingProperty = _serializedSettings.FindProperty("companySettings");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(buildSettingProperty, new GUIContent("Build Company Settings"));
            if (GUILayout.Button("Default", GUILayout.Width(70)))
                buildSettingProperty.objectReferenceValue = BuildCompanySettingsSO.FindOrCreateSettingsAsset();
            if (GUILayout.Button("Ping", GUILayout.Width(50)) && buildSettingProperty.objectReferenceValue != null)
            {
                Selection.activeObject = buildSettingProperty.objectReferenceValue;
                EditorGUIUtility.PingObject(buildSettingProperty.objectReferenceValue);
                EditorUtility.FocusProjectWindow();
            }
            EditorGUILayout.EndHorizontal();

            var companySettingsAsset = buildSettingProperty.objectReferenceValue as BuildCompanySettingsSO;
            DrawCompanyProfilePopup(companySettingsAsset);
        }

        private void DrawCompanyProfilePopup(BuildCompanySettingsSO companySettingsAsset)
        {
            var companyNameProperty = _serializedSettings.FindProperty("companyName");
            var developmentTeamIdProperty = _serializedSettings.FindProperty("developmentTeamId");
            var useManualCompanyProfileProperty = _serializedSettings.FindProperty("useManualCompanyProfile");
            if (companySettingsAsset == null)
            {
                EditorGUILayout.PropertyField(companyNameProperty);
                EditorGUILayout.PropertyField(developmentTeamIdProperty, new GUIContent("Development Team ID"));
                return;
            }

            var profiles = companySettingsAsset.CompanyProfiles
                .Where(profile => profile != null && !string.IsNullOrWhiteSpace(profile.companyName))
                .ToList();

            int matchedIndex = profiles.FindIndex(profile =>
                string.Equals(profile.companyName?.Trim(), companyNameProperty.stringValue?.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            bool useManualCompanyProfile = useManualCompanyProfileProperty?.boolValue ?? false;
            if (!useManualCompanyProfile && matchedIndex < 0)
            {
                useManualCompanyProfile = true;
                if (useManualCompanyProfileProperty != null)
                    useManualCompanyProfileProperty.boolValue = true;
            }
            int currentOptionIndex = !useManualCompanyProfile && matchedIndex >= 0 ? matchedIndex + 2 : 1;
            string[] options = new[] { "Custom / Add Company", "Custom / Manual" }
                .Concat(profiles.Select(profile => profile.companyName.Trim()))
                .ToArray();

            EditorGUI.BeginChangeCheck();
            int selectedOptionIndex = EditorGUILayout.Popup("Company Profile", currentOptionIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedOptionIndex == 0)
                {
                    CompanyProfileEditWindow.Open(companySettingsAsset, "", "", profile =>
                    {
                        _serializedSettings.Update();
                        _serializedSettings.FindProperty("companyName").stringValue = profile.companyName.Trim();
                        _serializedSettings.FindProperty("developmentTeamId").stringValue =
                            profile.developmentTeamId?.Trim() ?? "";
                        _serializedSettings.FindProperty("useManualCompanyProfile").boolValue = false;
                        _serializedSettings.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        Repaint();
                    });
                    return;
                }

                if (useManualCompanyProfileProperty != null)
                    useManualCompanyProfileProperty.boolValue = selectedOptionIndex == 1;
                useManualCompanyProfile = selectedOptionIndex == 1;

                if (selectedOptionIndex > 1)
                {
                    var selectedProfile = profiles[selectedOptionIndex - 2];
                    companyNameProperty.stringValue = selectedProfile.companyName.Trim();
                    developmentTeamIdProperty.stringValue = selectedProfile.developmentTeamId?.Trim() ?? "";
                }
            }

            if (useManualCompanyProfile)
            {
                DrawManualCompanyProfileControls(companyNameProperty, developmentTeamIdProperty);
                return;
            }

            SyncSerializedDevelopmentTeamId(companySettingsAsset, companyNameProperty, developmentTeamIdProperty);
        }

        private static void DrawManualCompanyProfileControls(
            SerializedProperty companyNameProperty,
            SerializedProperty developmentTeamIdProperty)
        {
            EditorGUILayout.PropertyField(companyNameProperty, new GUIContent("Company Name"));
            EditorGUILayout.PropertyField(developmentTeamIdProperty, new GUIContent("Development Team ID"));
        }

        private void DrawPathSettings()
        {
#if UNITY_ANDROID
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("saveFileInProject"),
                new GUIContent("Save In Project Folder"));

            bool saveFileInProject = _serializedSettings.FindProperty("saveFileInProject").boolValue;
            if (!saveFileInProject)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("androidBuildPath"));
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Choose Android Build Folder", settings.androidBuildPath, "");
                    if (!string.IsNullOrEmpty(path))
                        _serializedSettings.FindProperty("androidBuildPath").stringValue = path;
                }
                EditorGUILayout.EndHorizontal();
            }
#elif UNITY_IOS
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("saveFileInProject"),
                new GUIContent("Save In Project Folder"));

            bool saveFileInProject = _serializedSettings.FindProperty("saveFileInProject").boolValue;
            if (!saveFileInProject)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("iosBuildPath"));
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Choose iOS Build Folder", settings.iosBuildPath, "");
                    if (!string.IsNullOrEmpty(path))
                        _serializedSettings.FindProperty("iosBuildPath").stringValue = path;
                }
                EditorGUILayout.EndHorizontal();
            }
#endif
        }

        private void DrawPackageSettings()
        {
            // Auto Search + 버튼을 한 줄에 배치
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("autoSearchPackageName"),
                new GUIContent("Auto Search Package Name"));
            if (GUILayout.Button("Search Package Name", GUILayout.Width(160)))
            {
                string detectedId = GetPackageNameFromFirebaseConfig();
                if (!string.IsNullOrEmpty(detectedId))
                {
#if UNITY_ANDROID
                    _serializedSettings.FindProperty("androidPackageName").stringValue = detectedId;
#elif UNITY_IOS
                    _serializedSettings.FindProperty("iosPackageName").stringValue = detectedId;
#endif
                    Debug.Log($"<color=cyan><b>[Firebase Sync]</b></color> Package ID updated: {detectedId}");
                }
            }
            EditorGUILayout.EndHorizontal();

            // 자동 탐색 시 패키지명 필드 비활성화
            bool isAuto = _serializedSettings.FindProperty("autoSearchPackageName").boolValue;
            GUI.enabled = !isAuto;
#if UNITY_ANDROID
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("androidPackageName"));
#elif UNITY_IOS
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("iosPackageName"));
#endif
            GUI.enabled = true;
        }

        private void DrawVersionSettings()
        {
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("buildVersion"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("bundleNo"));
        }

#if UNITY_ANDROID
        private void DrawGpgsSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("GPGS Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("settingGPGS"),
                new GUIContent("Enable GPGS Setup"));

            if (_serializedSettings.FindProperty("settingGPGS").boolValue)
            {
                // Resources Definition XML 입력창 (최우선)
                SerializedProperty xmlProp = _serializedSettings.FindProperty("resourcesDefinition");
                EditorGUILayout.LabelField("Resources Definition (XML)");
                xmlProp.stringValue = EditorGUILayout.TextArea(xmlProp.stringValue, GUILayout.Height(80));

                // XML이 비어있을 때만 App ID 수동 입력 활성화
                bool hasXml = !string.IsNullOrWhiteSpace(xmlProp.stringValue);
                if (hasXml)
                {
                    // XML에서 App ID 추출하여 표시 (읽기 전용)
                    string extractedAppId = ExtractAppIdFromXml(xmlProp.stringValue);
                    GUI.enabled = false;
                    EditorGUILayout.TextField("App ID (from XML)", extractedAppId ?? "(not found)");
                    GUI.enabled = true;

                    // XML에서 Client ID 추출하여 표시 (있는 경우)
                    string extractedClientId = ExtractClientIdFromXml(xmlProp.stringValue);
                    if (!string.IsNullOrEmpty(extractedClientId))
                    {
                        GUI.enabled = false;
                        EditorGUILayout.TextField("Client ID (from XML)", extractedClientId);
                        GUI.enabled = true;
                    }
                    else
                    {
                        // XML에 Client ID가 없으면 수동 입력
                        EditorGUILayout.PropertyField(_serializedSettings.FindProperty("clientID"), new GUIContent("Client ID"));
                    }
                }
                else
                {
                    // XML이 없으면 App ID 수동 입력
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty("app_id"));
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty("clientID"), new GUIContent("Client ID"));
                    EditorGUILayout.HelpBox("XML이 비어있으면 App ID와 Package Name으로 기본 XML이 자동 생성됩니다.", MessageType.Info);
                }
            }
        }

        // XML에서 app_id 추출
        private static string ExtractAppIdFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(xml, @"name=""app_id""[^>]*>([^<]+)</string>");
            return match.Success ? match.Groups[1].Value : null;
        }

        // XML에서 client_id 추출
        private static string ExtractClientIdFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            // oauth_client_id 또는 client_id 패턴 모두 지원
            var match = System.Text.RegularExpressions.Regex.Match(xml, @"name=""(?:oauth_)?client_id""[^>]*>([^<]+)</string>");
            return match.Success ? match.Groups[1].Value : null;
        }

        private void DrawKeyStoreSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("KeyStore Settings", EditorStyles.boldLabel);

            // 비밀번호 편집 토글
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("editablePassword"),
                new GUIContent("Editable Password", "비밀번호 편집 활성화"));

            bool isEditable = _serializedSettings.FindProperty("editablePassword").boolValue;

            // Auto Search Keystore
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoSearchKeystore"),
                new GUIContent("Auto Search Keystore"));

            bool isAutoKeystore = _serializedSettings.FindProperty("autoSearchKeystore").boolValue;
            if (!isAutoKeystore)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("keyStorePath"));
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFilePanel("Choose KeyStore File", settings.keyStorePath, "keystore");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _serializedSettings.FindProperty("keyStorePath").stringValue = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // Keystore Password
            GUI.enabled = isEditable;
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("keystorePassword"),
                new GUIContent("KeyStore Password"));
            GUI.enabled = true;

            // Auto Search Alias
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("autoSearchAlias"),
                new GUIContent("Auto Search Alias"));

            bool isAutoAlias = _serializedSettings.FindProperty("autoSearchAlias").boolValue;
            if (!isAutoAlias)
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("keyStoreAlias"));
            }

            // Alias Password
            GUI.enabled = isEditable;
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("aliasPassword"),
                new GUIContent("Alias Password"));
            GUI.enabled = true;
        }
#endif

        private void DrawBuildOptions()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Build Options", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("developmentBuild"),
                new GUIContent("Development Build", "Unity BuildOptions.Development를 실제 Player 빌드에 적용합니다."));
#if !ACTIONFIT_CUSTOM_SYMBOLS
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("isDevMode"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("defineSymbol"));
#endif
        }

#if ACTIONFIT_CUSTOM_SYMBOLS
        // 심볼 관리 옵션 (CustomSymbols 패키지 설치 시 표시)
        private void DrawSymbolManageOption()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Symbol Management", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var prop = _serializedSettings.FindProperty("manageSymbolsOnBuild");
            EditorGUILayout.PropertyField(prop, new GUIContent("빌드 시 심볼 관리",
                "체크 시: 빌드 전 심볼 불일치 검사 + 빌드용 심볼 교체 + 빌드 후 복원\n해제 시: 현재 에디터 심볼 그대로 빌드"));
            if (EditorGUI.EndChangeCheck())
            {
                _serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }
#endif

#if UNITY_IOS
        private void DrawiOSCapabilities()
        {
            // Code Sign 설정
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("iOS Code Sign", EditorStyles.boldLabel);

            DrawDevelopmentTeamIdField();
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("iosTargetOSVersion"),
                new GUIContent("Target iOS Version"));

            // Target Device 설정
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Target Device", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("targetIPhone"),
                new GUIContent("iPhone"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("targetIPad"),
                new GUIContent("iPad"));

            // 둘 다 미선택 시 경고
            if (!_serializedSettings.FindProperty("targetIPhone").boolValue
                && !_serializedSettings.FindProperty("targetIPad").boolValue)
            {
                EditorGUILayout.HelpBox("At least one target device must be selected.", MessageType.Error);
            }

            // Capabilities 설정
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("iOS Capabilities", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("useGameCenter"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("usePushNotifications"),
                new GUIContent("Push Notifications (+ Background Mode)"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("useICloud"),
                new GUIContent("iCloud (Key-value storage)"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("associatedDomains"),
                new GUIContent("Associated Domains"), true);
            EditorGUILayout.HelpBox("Universal Links require entries such as applinks:actionfit.sng.link. The same capability must also be enabled on the Apple App ID and provisioning profile.",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("addFrameworks"),
                new GUIContent("Additional Frameworks"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("removeFrameworks"),
                new GUIContent("Remove Libraries (UnityFramework)"));
        }
#endif

        private void DrawBuildButtons()
        {
            EditorGUILayout.Space(20);
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Assign Build Settings asset to enable build options.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Build Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

#if UNITY_ANDROID
            if (GUILayout.Button("Android APK Build"))
            {
                ApplyAndSaveIfModified();
                SwitchPlatformAndBuild(BuildTarget.Android, () =>
                {
                    if (!AosBuildSetting()) return;
                    AOSBuildProcess.AndroidBuild1(settings);
                });
            }

            if (GUILayout.Button("Android APK Build And Run"))
            {
                ApplyAndSaveIfModified();
                SwitchPlatformAndBuild(BuildTarget.Android, () =>
                {
                    if (!AosBuildSetting()) return;
                    AOSBuildProcess.AndroidBuildApkRun(settings);
                });
            }

            if (GUILayout.Button("Android AAB Build"))
            {
                ApplyAndSaveIfModified();
                SwitchPlatformAndBuild(BuildTarget.Android, () =>
                {
                    if (!AosBuildSetting()) return;
                    AOSBuildProcess.AndroidBuild2(settings);
                });
            }

            if (GUILayout.Button("Android AAB Build And Run"))
            {
                ApplyAndSaveIfModified();
                SwitchPlatformAndBuild(BuildTarget.Android, () =>
                {
                    if (!AosBuildSetting()) return;
                    AOSBuildProcess.AndroidBuild4(settings);
                });
            }
#elif UNITY_IOS
            if (GUILayout.Button("iOS Build"))
            {
                ApplyAndSaveIfModified();
                SwitchPlatformAndBuild(BuildTarget.iOS, () =>
                {
                    if (!IOSBuildSetting()) return;
                    iOSBuildProcess.Build(settings);
                });
            }
#endif
        }

        #endregion

        #region Build Settings

        private void EnsureCompanySettingsReference()
        {
            if (settings == null) return;

            bool changed = false;
            if (settings.companySettings == null)
            {
                settings.companySettings = BuildCompanySettingsSO.FindOrCreateSettingsAsset();
                changed = true;
            }

            changed |= settings.SyncDevelopmentTeamIdFromCompanyProfile();
            if (!changed) return;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void SyncSerializedDevelopmentTeamId(
            BuildCompanySettingsSO companySettingsAsset,
            SerializedProperty companyNameProperty,
            SerializedProperty developmentTeamIdProperty)
        {
            if (companySettingsAsset == null || companyNameProperty == null || developmentTeamIdProperty == null) return;
            if (!companySettingsAsset.TryGetDevelopmentTeamId(companyNameProperty.stringValue, out string matchedTeamId)) return;
            if (string.Equals(developmentTeamIdProperty.stringValue, matchedTeamId, StringComparison.Ordinal)) return;

            developmentTeamIdProperty.stringValue = matchedTeamId;
        }

        private void DrawDevelopmentTeamIdField()
        {
            var companySettingsAsset = _serializedSettings.FindProperty("companySettings")
                ?.objectReferenceValue as BuildCompanySettingsSO;
            bool useManualCompanyProfile = _serializedSettings.FindProperty("useManualCompanyProfile")?.boolValue ?? false;
            var companyNameProperty = _serializedSettings.FindProperty("companyName");
            var developmentTeamIdProperty = _serializedSettings.FindProperty("developmentTeamId");

            if (useManualCompanyProfile)
                return;

            if (!useManualCompanyProfile &&
                companySettingsAsset != null &&
                companySettingsAsset.TryGetDevelopmentTeamId(companyNameProperty.stringValue, out string matchedTeamId))
            {
                developmentTeamIdProperty.stringValue = matchedTeamId;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Development Team ID", matchedTeamId);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("Development Team ID is matched from the selected Company Profile.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(developmentTeamIdProperty, new GUIContent("Development Team ID"));
        }

        // SerializedObject 변경사항을 SO에 적용하고 저장
        private void ApplyAndSaveIfModified()
        {
            if (_serializedSettings == null || !_serializedSettings.hasModifiedProperties) return;

            _serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private void SwitchPlatformAndBuild(BuildTarget target, System.Action buildAction)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
            {
                buildAction?.Invoke();
                return;
            }

            if (!EditorUtility.DisplayDialog("Platform Switch",
                    $"Current platform is not {target}. Do you want to switch?", "Yes", "No"))
                return;

            BuildTargetGroup targetGroup =
                target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;

            if (EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
            {
                System.Threading.Thread.Sleep(1000);
                AssetDatabase.Refresh();
                buildAction?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to switch to {target} platform");
            }
        }

        private bool ValidateCommonSettings()
        {
            return ValidateRequired("Build Version", settings.buildVersion)
                   && ValidateRequired("Bundle Number", settings.bundleNo)
                   && ValidateRequired("Company Name", settings.companyName)
                   && ValidateRequired("Product Name", settings.productName);
        }

        private static bool ValidateRequired(string label, string value)
        {
            if (!IsPlaceholderOrEmpty(value)) return true;

            EditorUtility.DisplayDialog("Build Failed",
                $"{label} is not configured. Please replace the [Enter ...] placeholder in BuildSettingsSO.",
                "OK");
            return false;
        }

        private static bool IsPlaceholderOrEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith("[Enter", StringComparison.Ordinal);
        }

#if UNITY_IOS
        // iOS 빌드 설정 적용 (false 반환 시 빌드 중단)
        private bool IOSBuildSetting()
        {
#if ACTIONFIT_CUSTOM_SYMBOLS
            // 심볼 불일치 검사 (심볼 관리 활성 시에만)
            if (settings.manageSymbolsOnBuild && !CheckSymbolsMismatch(BuildTarget.iOS)) return false;
#endif

            if (!ValidateCommonSettings()) return false;
            if (!ValidateRequired("iOS Bundle ID", settings.iosPackageName)) return false;
            if (!settings.saveFileInProject && !ValidateRequired("iOS Build Path", settings.iosBuildPath)) return false;
            string resolvedDevelopmentTeamId = settings.GetResolvedDevelopmentTeamId();
            if (!ValidateRequired("Apple Team ID", resolvedDevelopmentTeamId)) return false;
            if (!ValidateRequired("Target iOS Version", settings.iosTargetOSVersion)) return false;

            // 패키지명 자동 탐색
            if (settings.autoSearchPackageName)
            {
                string detectedId = GetPackageNameFromFirebaseConfig(silent: true);
                if (!string.IsNullOrEmpty(detectedId))
                {
                    _serializedSettings.FindProperty("iosPackageName").stringValue = detectedId;
                    _serializedSettings.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"<color=cyan><b>[Firebase Sync]</b></color> Package ID updated for build: {detectedId}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Build Failed",
                        "Auto Search Package Name is enabled but package name could not be found from Firebase config.\nBuild has been cancelled.",
                        "OK");
                    return false;
                }
            }

            var namedBuildTarget = NamedBuildTarget.iOS;
            List<string> definesList;

#if ACTIONFIT_CUSTOM_SYMBOLS
            // CustomSymbolsSO 연동
            ScriptableObject symbolsSO = CustomSymbolsBridge.FindSettingsAsset();
            if (symbolsSO != null)
            {
                if (!ShowCustomSymbolsConfirmDialog(symbolsSO, BuildTarget.iOS)) return false;
                definesList = CustomSymbolsBridge.GetBuildSymbols(symbolsSO, BuildTarget.iOS);
            }
            else
            {
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbolsArr);
                definesList = new List<string>(currentSymbolsArr);
            }
#else
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbolsArr);
            definesList = new List<string>(currentSymbolsArr);

            // Dev 모드 처리
            if (settings.isDevMode)
            {
                if (!definesList.Contains("DEV"))
                    definesList.Add("DEV");
            }
            else
            {
                definesList.RemoveAll(x => x == "DEV");
            }

            // 추가 심볼 처리
            if (settings.defineSymbol.Count > 0)
            {
                foreach (var symbol in settings.defineSymbol)
                {
                    if (!string.IsNullOrEmpty(symbol) && !definesList.Contains(symbol))
                    {
                        definesList.Add(symbol);
                    }
                }
            }
#endif

            // 회사명/앱 이름 설정
            string resolvedCompanyName = settings.GetResolvedCompanyName();
            if (!string.IsNullOrEmpty(resolvedCompanyName))
                PlayerSettings.companyName = resolvedCompanyName;
            if (!string.IsNullOrEmpty(settings.productName))
                PlayerSettings.productName = settings.productName;

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, definesList.ToArray());

            PlayerSettings.bundleVersion = settings.buildVersion;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, settings.iosPackageName);
            PlayerSettings.iOS.buildNumber = settings.bundleNo;
            PlayerSettings.iOS.appleDeveloperTeamID = resolvedDevelopmentTeamId;
            PlayerSettings.iOS.targetOSVersionString = settings.GetResolvedIosTargetOSVersion();

            return true;
        }
#endif

#if UNITY_ANDROID
        // Android 빌드 설정 적용 (false 반환 시 빌드 중단)
        private bool AosBuildSetting()
        {
#if ACTIONFIT_CUSTOM_SYMBOLS
            // 심볼 불일치 검사 (심볼 관리 활성 시에만)
            if (settings.manageSymbolsOnBuild && !CheckSymbolsMismatch(BuildTarget.Android)) return false;
#endif

            if (!ValidateCommonSettings()) return false;
            if (!ValidateRequired("Build File Name", settings.buildFileName)) return false;
            if (!settings.saveFileInProject && !ValidateRequired("Android Build Path", settings.androidBuildPath)) return false;
            if (!ValidateRequired("Android Package Name", settings.androidPackageName)) return false;

            // 패키지명 자동 탐색
            if (settings.autoSearchPackageName)
            {
                string detectedId = GetPackageNameFromFirebaseConfig(silent: true);
                if (!string.IsNullOrEmpty(detectedId))
                {
                    _serializedSettings.FindProperty("androidPackageName").stringValue = detectedId;
                    _serializedSettings.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"<color=cyan><b>[Firebase Sync]</b></color> Package ID updated for build: {detectedId}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Build Failed",
                        "Auto Search Package Name is enabled but package name could not be found from Firebase config.\nBuild has been cancelled.",
                        "OK");
                    return false;
                }
            }

            var namedBuildTarget = NamedBuildTarget.Android;
            List<string> definesList;

#if ACTIONFIT_CUSTOM_SYMBOLS
            // CustomSymbolsSO 연동
            ScriptableObject symbolsSO = CustomSymbolsBridge.FindSettingsAsset();
            if (symbolsSO != null)
            {
                if (!ShowCustomSymbolsConfirmDialog(symbolsSO, BuildTarget.Android)) return false;
                definesList = CustomSymbolsBridge.GetBuildSymbols(symbolsSO, BuildTarget.Android);
            }
            else
            {
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbolsArr);
                definesList = new List<string>(currentSymbolsArr);
            }
#else
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbolsArr);
            definesList = new List<string>(currentSymbolsArr);

            // Dev 모드 처리
            if (settings.isDevMode)
            {
                if (!definesList.Contains("DEV")) definesList.Add("DEV");
            }
            else definesList.RemoveAll(x => x == "DEV");

            // 추가 심볼 처리
            if (settings.defineSymbol.Count > 0)
            {
                foreach (var symbol in settings.defineSymbol)
                {
                    if (!string.IsNullOrEmpty(symbol) && !definesList.Contains(symbol)) definesList.Add(symbol);
                }
            }
#endif

            // 회사명/앱 이름 설정
            string resolvedCompanyName = settings.GetResolvedCompanyName();
            if (!string.IsNullOrEmpty(resolvedCompanyName))
                PlayerSettings.companyName = resolvedCompanyName;
            if (!string.IsNullOrEmpty(settings.productName))
                PlayerSettings.productName = settings.productName;

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, definesList.ToArray());

            PlayerSettings.bundleVersion = settings.buildVersion;

            // 키스토어 자동 탐색
            // autoSearchKeystore가 켜져 있거나, SO에 저장된 경로가 실제 파일에 매칭되지 않으면(다른 PC에서 커밋된 절대 경로 등) 강제 탐색
            string finalKeystorePath = settings.keyStorePath;
            bool needAutoSearch = settings.autoSearchKeystore || string.IsNullOrEmpty(finalKeystorePath) || !File.Exists(finalKeystorePath);
            if (needAutoSearch)
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string[] files = Directory.GetFiles(projectRoot, "*.keystore", SearchOption.AllDirectories);
                string validKeystore = null;
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileName.StartsWith("._"))
                    {
                        validKeystore = file;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(validKeystore))
                {
                    finalKeystorePath = validKeystore;
                    Debug.Log($"<color=green><b>[Build]</b></color> Keystore Auto-detected: {finalKeystorePath} (forced={!settings.autoSearchKeystore})");
                    _serializedSettings.FindProperty("keyStorePath").stringValue = finalKeystorePath;
                    _serializedSettings.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogError($"[Build] No valid .keystore file found under projectRoot='{projectRoot}'. " +
                        $"SO.keyStorePath='{settings.keyStorePath}'. Build will fail signing.");
                }
            }

            // Alias 자동 탐색
            if (settings.autoSearchAlias)
            {
                string detectedAlias = GetFirstAliasFromKeystore(finalKeystorePath, settings.keystorePassword);
                if (!string.IsNullOrEmpty(detectedAlias))
                {
                    _serializedSettings.FindProperty("keyStoreAlias").stringValue = detectedAlias;
                    _serializedSettings.ApplyModifiedProperties();
                    Debug.Log($"<color=cyan><b>[Build]</b></color> Alias Auto-detected: {detectedAlias}");
                    EditorUtility.SetDirty(settings);
                }
            }

            // 키스토어 설정 적용
            if (File.Exists(finalKeystorePath))
            {
                // Custom keystore 사용 플래그 필수 — false면 Unity가 debug.keystore로 서명을 시도하다 실패하며
                // "Unable to sign the application; please provide passwords!" 다이얼로그 발생
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = finalKeystorePath;
                PlayerSettings.Android.keystorePass = settings.keystorePassword;
                PlayerSettings.Android.keyaliasName = settings.keyStoreAlias;
                PlayerSettings.Android.keyaliasPass = settings.aliasPassword;
            }
            else
            {
                Debug.LogError($"[Build] Keystore file not found at: {finalKeystorePath}");
            }

            if (int.TryParse(settings.bundleNo, out int bundleCode))
            {
                PlayerSettings.Android.bundleVersionCode = bundleCode;
            }

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, settings.androidPackageName);

            // GPGS 설정 동기화 (빌드 설정 → GPGS 프로젝트 설정)
            if (settings.settingGPGS)
            {
                GpgsBridge.Sync(settings.app_id, settings.clientID, settings.androidPackageName);
            }

            return true;
        }
#endif

#if ACTIONFIT_CUSTOM_SYMBOLS
        // CustomSymbolsSO 빌드 확인 다이얼로그 (false 반환 시 빌드 중단)
        private static bool ShowCustomSymbolsConfirmDialog(ScriptableObject so, BuildTarget target)
        {
            List<string> excluded = CustomSymbolsBridge.GetExcludedSymbols(so, target);

            if (excluded.Count > 0)
            {
                string excludedNames = string.Join("\n- ", excluded);
                return EditorUtility.DisplayDialog("Custom Symbols",
                    $"다음 심볼이 빌드에서 제외됩니다:\n\n- {excludedNames}\n\n빌드를 진행하시겠습니까?",
                    "Build", "Cancel");
            }

            return EditorUtility.DisplayDialog("Custom Symbols",
                "모든 심볼이 빌드에 포함됩니다.\n빌드를 진행하시겠습니까?",
                "Build", "Cancel");
        }
#endif

#if ACTIONFIT_CUSTOM_SYMBOLS
        // CustomSymbolsSO와 PlayerSettings의 에디터 심볼 불일치 검사 (false 반환 시 빌드 중단)
        // 빌드 시 심볼 교체는 SymbolsBuildProcessor가 담당
        private static bool CheckSymbolsMismatch(BuildTarget target)
        {
            ScriptableObject symbolsSO = CustomSymbolsBridge.FindSettingsAsset();
            if (symbolsSO == null) return true;

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            var expected = new HashSet<string>(CustomSymbolsBridge.GetPlatformSymbols(symbolsSO, target));
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out string[] currentSymbols);
            var actual = new HashSet<string>(currentSymbols);

            var missing = expected.Except(actual).ToList();
            var extra = actual.Except(expected).ToList();

            if (missing.Count == 0 && extra.Count == 0) return true;

            string message = "Custom Symbols 설정과 현재 에디터 심볼이 다릅니다.\n\n";
            if (missing.Count > 0)
                message += $"[미적용] {string.Join(", ", missing)}\n";
            if (extra.Count > 0)
                message += $"[미등록] {string.Join(", ", extra)}\n";
            message += "\n적용 후 빌드하시겠습니까?";

            int choice = EditorUtility.DisplayDialogComplex("Symbol Mismatch",
                message, "적용 후 빌드", "취소", "그대로 빌드");

            switch (choice)
            {
                case 0: // 적용 후 빌드
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, expected.ToArray());
                    Debug.Log($"[Build] Symbols synced for {target}: {string.Join(";", expected)}");
                    return true;
                case 1: // 취소
                    return false;
                case 2: // 그대로 빌드
                    return true;
                default:
                    return false;
            }
        }
#endif

        #endregion

        #region Utility Methods

        // 입력된 XML을 그대로 저장
        private static void WriteGpgsXml(string xmlContent)
        {
            string resourcesPath = Path.Combine(Application.dataPath, "../Temp/StagingArea/res/values");
            string xmlFilePath = Path.Combine(resourcesPath, "games-ids.xml");

            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }

            try
            {
                File.WriteAllText(xmlFilePath, xmlContent, System.Text.Encoding.UTF8);
                Debug.Log($"<color=green><b>[GPGS]</b></color> XML written from input: {xmlFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GPGS] XML write failed: {e.Message}");
            }
        }

        // App ID와 Package Name으로 기본 XML 자동 생성
        private static void CreateGpgsXml(string appId, string packageName)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(packageName))
            {
                Debug.LogError($"[GPGS] Cannot create XML - appId:{appId} packageName:{packageName}");
                return;
            }

            string xmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
  <string name=""app_id"" translatable=""false"">{appId}</string>
  <string name=""package_name"" translatable=""false"">{packageName}</string>
</resources>";

            WriteGpgsXml(xmlContent);
            Debug.Log($"<color=cyan><b>[GPGS]</b></color> XML auto-generated from App ID: {appId}");
        }

        // Firebase 설정 파일에서 패키지명/번들 ID 가져오기 (silent: true이면 다이얼로그 생략)
        public static string GetPackageNameFromFirebaseConfig(bool silent = false)
        {
            string assetsRoot = Application.dataPath;

#if UNITY_ANDROID
            string[] files = Directory.GetFiles(assetsRoot, "google-services.json", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Error", "google-services.json not found in project.", "OK");
                return null;
            }

            try
            {
                string jsonText = File.ReadAllText(files[0]);
                FirebaseData data = JsonUtility.FromJson<FirebaseData>(jsonText);
                if (data?.client != null && data.client.Length > 0)
                {
                    string pName = data.client[0].client_info.android_client_info.package_name;
                    if (!string.IsNullOrEmpty(pName)) return pName;
                }
                if (!silent)
                    EditorUtility.DisplayDialog("Error", "package_name not found in google-services.json.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase Parsing Error] {e.Message}");
            }
#elif UNITY_IOS
            string[] files = Directory.GetFiles(assetsRoot, "GoogleService-Info.plist", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Error", "GoogleService-Info.plist not found in project.", "OK");
                return null;
            }

            try
            {
                string plistText = File.ReadAllText(files[0]);
                string bundleIdKey = "<key>BUNDLE_ID</key>";
                int keyIndex = plistText.IndexOf(bundleIdKey);
                if (keyIndex >= 0)
                {
                    int stringStart = plistText.IndexOf("<string>", keyIndex) + "<string>".Length;
                    int stringEnd = plistText.IndexOf("</string>", stringStart);
                    if (stringStart > "<string>".Length && stringEnd > stringStart)
                    {
                        return plistText.Substring(stringStart, stringEnd - stringStart);
                    }
                }
                if (!silent)
                    EditorUtility.DisplayDialog("Error", "BUNDLE_ID not found in GoogleService-Info.plist.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase Parsing Error] {e.Message}");
            }
#endif
            return null;
        }

#if UNITY_ANDROID
        [System.Serializable]
        private class FirebaseData
        {
            public FirebaseClient[] client;
        }

        [System.Serializable]
        private class FirebaseClient
        {
            public FirebaseClientInfo client_info;
        }

        [System.Serializable]
        private class FirebaseClientInfo
        {
            public FirebaseAndroidInfo android_client_info;
        }

        [System.Serializable]
        private class FirebaseAndroidInfo
        {
            public string package_name;
        }
#endif

#if UNITY_ANDROID
        // 키스토어에서 첫 번째 Alias 추출
        private static string GetFirstAliasFromKeystore(string keystorePath, string password)
        {
            try
            {
                string jdkPath = UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath;
                string keytoolPath = Path.Combine(jdkPath, "bin", "keytool");

                if (Application.platform == RuntimePlatform.WindowsEditor)
                    keytoolPath += ".exe";

                if (!File.Exists(keytoolPath))
                {
                    Debug.LogError("Keytool not found. Check JDK path.");
                    return string.Empty;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = keytoolPath,
                    Arguments = $"-list -keystore \"{keystorePath}\" -storepass {password}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        string[] lines = result.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrEmpty(line) && line.Contains(","))
                            {
                                return line.Split(',')[0].Trim();
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Alias extraction failed: {e.Message}");
            }
            return string.Empty;
        }

#endif

        #endregion
    }

    internal sealed class CompanyProfileEditWindow : EditorWindow
    {
        private BuildCompanySettingsSO _companySettings;
        private Action<BuildCompanyProfile> _onSaved;
        private string _companyName = "";
        private string _developmentTeamId = "";

        public static void Open(
            BuildCompanySettingsSO companySettings,
            string companyName,
            string developmentTeamId,
            Action<BuildCompanyProfile> onSaved)
        {
            var window = CreateInstance<CompanyProfileEditWindow>();
            window.titleContent = new GUIContent("Add Company Profile");
            window.minSize = new Vector2(360, 130);
            window._companySettings = companySettings;
            window._companyName = companyName?.Trim() ?? "";
            window._developmentTeamId = developmentTeamId?.Trim() ?? "";
            window._onSaved = onSaved;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            _companyName = EditorGUILayout.TextField("Company Name", _companyName);
            _developmentTeamId = EditorGUILayout.TextField("Development Team ID", _developmentTeamId);
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                Close();

            EditorGUI.BeginDisabledGroup(!CanSave());
            if (GUILayout.Button("Save", GUILayout.Width(90)))
                Save();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private bool CanSave()
        {
            return _companySettings != null &&
                   !string.IsNullOrWhiteSpace(_companyName) &&
                   !string.IsNullOrWhiteSpace(_developmentTeamId);
        }

        private void Save()
        {
            string companyName = _companyName.Trim();
            string developmentTeamId = _developmentTeamId.Trim();
            bool alreadyExists = _companySettings.TryGetProfile(companyName, out var existingProfile);
            if (alreadyExists &&
                !string.Equals(existingProfile.developmentTeamId?.Trim(), developmentTeamId, StringComparison.Ordinal))
            {
                bool update = EditorUtility.DisplayDialog(
                    "Company Profile",
                    $"`{companyName}` already exists.\n\nUpdate its Development Team ID?",
                    "Update",
                    "Cancel");
                if (!update) return;
            }

            _companySettings.EnsureProfile(companyName, developmentTeamId, alreadyExists);
            EditorUtility.SetDirty(_companySettings);
            AssetDatabase.SaveAssets();

            if (!_companySettings.TryGetProfile(companyName, out var savedProfile))
                savedProfile = new BuildCompanyProfile
                {
                    companyName = companyName,
                    developmentTeamId = developmentTeamId
                };

            _onSaved?.Invoke(savedProfile);
            Close();
        }
    }
}

#endif
