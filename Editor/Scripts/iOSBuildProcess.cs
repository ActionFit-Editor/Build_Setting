using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor
{
#if UNITY_IOS

    #region Build Settings Holder

    public static class BuildSettingsHolder
    {
        private static BuildSettingsSO _currentSettings;

        public static void SetSettings(BuildSettingsSO settings)
        {
            _currentSettings = settings;
        }

        public static BuildSettingsSO GetSettings()
        {
            return _currentSettings;
        }
    }

    #endregion

    public class iOSBuildProcess
    {
        #region Build

        /// <summary>
        /// iOS 빌드를 실행합니다.
        /// 기존 빌드가 있을 경우 Append/Replace/Cancel 다이얼로그를 표시합니다.
        /// </summary>
        public static void Build(BuildSettingsSO setting)
        {
            BuildInternal(setting, false);
        }

        public static BuildReport BuildForCI(BuildSettingsSO setting)
        {
            return BuildInternal(setting, true);
        }

        private static BuildReport BuildInternal(BuildSettingsSO setting, bool ciMode)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Debug.LogError("Current platform is not iOS. Please switch to iOS platform first.");
                return null;
            }

            BuildSettingsHolder.SetSettings(setting);
            string buildPath;

            // saveFileInProject 옵션에 따른 빌드 경로 설정
            if (setting.saveFileInProject)
            {
                buildPath = "Builds/iOS";
            }
            else
            {
                buildPath = setting.iosBuildPath;
            }

            if (Path.IsPathRooted(buildPath))
            {
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                buildPath = Path.GetRelativePath(projectPath, buildPath);
            }

            string fullBuildPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", buildPath));
            BuildOptions buildOptions = BuildOptions.None;

            // 기존 Xcode 프로젝트가 있으면 Append/Replace 선택
            if (Directory.Exists(fullBuildPath) &&
                File.Exists(Path.Combine(fullBuildPath, "Unity-iPhone.xcodeproj/project.pbxproj")))
            {
                int choice = ciMode
                    ? 0
                    : EditorUtility.DisplayDialogComplex(
                        "Build Folder Exists",
                        $"The build folder already contains an Xcode project.\n\n{fullBuildPath}",
                        "Replace",
                        "Cancel",
                        "Append"
                    );

                switch (choice)
                {
                    case 0: // Replace
                        buildOptions = BuildOptions.None;
                        break;
                    case 1: // Cancel
                        Debug.Log("[iOSBuildProcess] Build cancelled by user");
                        return null;
                    case 2: // Append
                        buildOptions = BuildOptions.AcceptExternalModificationsToPlayer;
                        break;
                }
            }
            else
            {
                if (!Directory.Exists(fullBuildPath)) Directory.CreateDirectory(fullBuildPath);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = buildOptions
            };

            // Addressables 콘텐츠 빌드 — link.xml/catalog 재생성 보장 (Player 빌드 직전)
            Debug.Log("[Build] Running Addressables BuildPlayerContent before Player build");
            AddressableAssetSettings.BuildPlayerContent();

            Debug.Log($"Starting iOS build ({(buildOptions == BuildOptions.AcceptExternalModificationsToPlayer ? "Append" : "Replace")}) at: {buildPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                    Debug.Log($"Build saved to: {fullBuildPath}");
                    if (!ciMode) EditorUtility.RevealInFinder(fullBuildPath);
                    break;
                case BuildResult.Failed:
                    Debug.LogError($"Build failed with {summary.totalErrors} errors");
                    foreach (var step in report.steps)
                    {
                        foreach (var message in step.messages)
                        {
                            if (message.type == LogType.Error) Debug.LogError($"Build error: {message.content}");
                        }
                    }
                    break;
            }

            return report;
        }

        #endregion

        #region Post Process Build

        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log($"Starting iOS post process... Target: {target}, Path: {pathToBuiltProject}");

            if (target != BuildTarget.iOS)
            {
                Debug.Log("This is not iOS build, skipping post process");
                return;
            }

            try
            {
                BuildSettingsSO setting = BuildSettingsHolder.GetSettings();
                if (setting == null)
                {
                    Debug.LogError("Build Settings not found. Post-processing may be incomplete.");
                    return;
                }

                ApplyInfoPlistSettings(setting, pathToBuiltProject);
                ApplyXcodeProjectSettings(setting, pathToBuiltProject);
                RemoveUnwantedLibraries(setting, pathToBuiltProject);
                ApplyCapabilitySettings(setting, pathToBuiltProject);
                ModifyUnityAppController(pathToBuiltProject);
                Debug.Log("Xcode post-processing completed successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during iOS post process: {e.Message}\n{e.StackTrace}");
            }
        }

        #endregion

        #region Info.plist Settings

        // Info.plist 설정 적용
        private static void ApplyInfoPlistSettings(BuildSettingsSO setting, string pathToBuiltProject)
        {
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            // 버전 정보
            rootDict.SetString("CFBundleShortVersionString", setting.buildVersion);
            rootDict.SetString("CFBundleVersion", setting.bundleNo);

            // ATS 정책 (HTTP 허용)
            PlistElementDict atsDict = rootDict.CreateDict("NSAppTransportSecurity");
            atsDict.SetBoolean("NSAllowsArbitraryLoads", true);

            // 수출입 암호화 관련 설정
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            plist.WriteToFile(plistPath);
        }

        #endregion

        #region Xcode Project Settings

        // Xcode 프로젝트 설정 적용
        private static void ApplyXcodeProjectSettings(BuildSettingsSO buildSetting, string pathToBuiltProject)
        {
            string projPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string mainTargetGuid = proj.GetUnityMainTargetGuid();
            string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

            // 프로젝트 내 모든 타겟 GUID 수집
            var allTargets = new System.Collections.Generic.List<string>();
            allTargets.Add(mainTargetGuid);
            allTargets.Add(frameworkTargetGuid);

            string gameAssemblyGuid = proj.TargetGuidByName("GameAssembly");
            if (!string.IsNullOrEmpty(gameAssemblyGuid)) allTargets.Add(gameAssemblyGuid);

            string testTargetGuid = proj.TargetGuidByName("Unity-iPhone Tests");
            if (!string.IsNullOrEmpty(testTargetGuid)) allTargets.Add(testTargetGuid);

            // Target Device 설정 (1=iPhone, 2=iPad, 1,2=Both)
            string deviceFamily = "";
            if (buildSetting.targetIPhone) deviceFamily += "1";
            if (buildSetting.targetIPad)
            {
                if (deviceFamily.Length > 0) deviceFamily += ",";
                deviceFamily += "2";
            }
            if (string.IsNullOrEmpty(deviceFamily)) deviceFamily = "1,2";
            string iosTargetOSVersion = buildSetting.GetResolvedIosTargetOSVersion();

            // 모든 타겟에 공통 빌드 설정 적용
            foreach (string targetGuid in allTargets)
            {
                proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
                proj.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", iosTargetOSVersion);
                proj.SetBuildProperty(targetGuid, "TARGETED_DEVICE_FAMILY", deviceFamily);
                proj.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");
                proj.SetBuildProperty(targetGuid, "SUPPORTS_XR_DESIGNED_FOR_IPHONE_IPAD", "NO");
                proj.SetBuildProperty(targetGuid, "SUPPORTS_MACCATALYST", "NO");
                proj.SetBuildProperty(targetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
                proj.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
                proj.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Development");

                if (!string.IsNullOrEmpty(buildSetting.developmentTeamId))
                {
                    proj.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", buildSetting.developmentTeamId);
                }
            }

            Debug.Log($"[iOSBuildProcess] Applied settings to {allTargets.Count} targets. TARGETED_DEVICE_FAMILY: {deviceFamily}, IPHONEOS_DEPLOYMENT_TARGET: {iosTargetOSVersion}");

            if (!string.IsNullOrEmpty(buildSetting.developmentTeamId))
            {
                Debug.Log($"[iOSBuildProcess] Set DEVELOPMENT_TEAM: {buildSetting.developmentTeamId}");
            }

            // 추가 프레임워크 (Main 타겟에만)
            if (buildSetting.addFrameworks != null && buildSetting.addFrameworks.Count > 0)
            {
                foreach (string framework in buildSetting.addFrameworks)
                {
                    if (string.IsNullOrEmpty(framework)) continue;
                    string frameworkName = framework.EndsWith(".framework") ? framework : $"{framework}.framework";
                    proj.AddFrameworkToProject(mainTargetGuid, frameworkName, false);
                    Debug.Log($"[iOSBuildProcess] Added framework: {frameworkName}");
                }
            }

            proj.WriteToFile(projPath);
        }

        #endregion

        #region Remove Unwanted Libraries

        // UnityFramework 타겟에서 지정된 라이브러리 제거
        private static void RemoveUnwantedLibraries(BuildSettingsSO setting, string pathToBuiltProject)
        {
            if (setting.removeFrameworks == null || setting.removeFrameworks.Count == 0) return;

            string projPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

            foreach (string libraryName in setting.removeFrameworks)
            {
                if (string.IsNullOrEmpty(libraryName)) continue;

                // 빌드 출력 디렉터리에서 해당 파일 검색
                string[] files = Directory.GetFiles(pathToBuiltProject, libraryName, SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    Debug.LogWarning($"[iOSBuildProcess] Library not found in build: {libraryName}");
                    continue;
                }

                foreach (string filePath in files)
                {
                    string relativePath = filePath.Substring(pathToBuiltProject.Length + 1);
                    string fileGuid = proj.FindFileGuidByProjectPath(relativePath);

                    if (fileGuid != null)
                    {
                        proj.RemoveFileFromBuild(frameworkTargetGuid, fileGuid);
                        Debug.Log($"[iOSBuildProcess] Removed from UnityFramework: {relativePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[iOSBuildProcess] File found but not in project: {relativePath}");
                    }
                }
            }

            proj.WriteToFile(projPath);
        }

        #endregion

        #region Capability Settings

        // Capability 설정 적용
        private static void ApplyCapabilitySettings(BuildSettingsSO setting, string pathToBuiltProject)
        {
            string entitlementsPath = Path.Combine(pathToBuiltProject, "Unity-iPhone/Unity-iPhone.entitlements");
            string projPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");

            ProjectCapabilityManager manager = new ProjectCapabilityManager(
                projPath,
                entitlementsPath,
                "Unity-iPhone"
            );

            if (setting.usePushNotifications)
            {
                manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
                manager.AddPushNotifications(true);
            }
            if (setting.useGameCenter) manager.AddGameCenter();
            if (setting.useICloud) manager.AddiCloud(true, false, false, false, null);

            manager.WriteToFile();
        }

        #endregion

        #region UnityAppController Fix

        // UnityAppController.mm의 MetalDisplayLink return YES를 NO로 변경 (iOS 화면 멈춤 현상 수정)
        private static void ModifyUnityAppController(string buildPath)
        {
            string filePath = Path.Combine(buildPath, "Classes/UnityAppController.mm");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[iOSBuildProcess] UnityAppController.mm not found at path: {filePath}");
                return;
            }

            string text = File.ReadAllText(filePath);

            // pattern: #elif PLATFORM_VISIONOS 전처리 내의 return YES를 NO로 변경
            string pattern = @"(#elif\s+PLATFORM_VISIONOS[\s\S]*?#else\s+return\s+)YES(;\s+#endif)";
            string replacement = "$1NO$2";

            string newText = Regex.Replace(text, pattern, replacement);

            if (text != newText)
            {
                File.WriteAllText(filePath, newText);
                Debug.Log("[iOSBuildProcess] UnityAppController.mm: MetalDisplayLink return value changed to NO");
            }
            else
            {
                Debug.LogWarning("[iOSBuildProcess] UnityAppController.mm: Pattern not found. File may have changed.");
            }
        }

        #endregion
    }

#endif
}
