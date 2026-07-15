using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.

namespace ActionFit.BuildSetting.Editor
{
#if UNITY_ANDROID
public class AOSBuildProcess
{
    #region Build Methods

    /// <summary>
    /// APK 빌드 (DEV 모드 비허용)
    /// </summary>
    public static void AndroidBuild1(BuildSettingsSO setting)
    {
#if DEV
        Debug.LogError("[Build Fail] build process is not support in DEV define");
        return;
#endif

        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = false;
        AndroidBuildProcess(setting);
    }

    /// <summary>
    /// AAB 빌드 (DEV 모드 비허용)
    /// </summary>
    public static void AndroidBuild2(BuildSettingsSO setting)
    {
#if DEV
        Debug.LogError("[Build Fail] build process is not support in DEV define");
        return;
#endif

        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = true;
        AndroidBuildProcess(setting, true);
    }

    /// <summary>
    /// DEV 모드 APK 빌드
    /// </summary>
    public static void AndroidBuild3(BuildSettingsSO setting)
    {
#if !DEV
        Debug.LogError("[Build Fail] must be in set dev mode");
        return;
#endif

        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = false;
        AndroidBuildProcess(setting, false, "dev_");
    }

    /// <summary>
    /// AAB 빌드 후 실행
    /// </summary>
    public static void AndroidBuild4(BuildSettingsSO setting)
    {
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = true;
        AndroidBuildProcess(setting, true, "", BuildOptions.AutoRunPlayer);
    }

    /// <summary>
    /// APK 빌드 후 실행 (DEV 모드 허용)
    /// </summary>
    public static void AndroidBuildApkRun(BuildSettingsSO setting)
    {
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = false;
        AndroidBuildProcess(setting, false, "", BuildOptions.AutoRunPlayer);
    }

    public static BuildReport BuildForCI(BuildSettingsSO setting, bool aab)
    {
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.buildAppBundle = aab;
        return AndroidBuildProcess(setting, aab, "", BuildOptions.None, true);
    }

    #endregion

    #region Build Process

    // 실제 빌드 프로세스 수행
    static BuildReport AndroidBuildProcess(BuildSettingsSO setting, bool aab = false, string buildPrefix = "", BuildOptions buildOptions = BuildOptions.None, bool ciMode = false)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Current platform is not Android. Please switch to Android platform first.");
            return null;
        }

        string buildName = setting.buildFileName;
        string version = setting.buildVersion;
        string bundleCode = setting.bundleNo;
        string buildPath;

        // saveFileInProject 옵션에 따른 빌드 경로 설정
        if (setting.saveFileInProject)
        {
            string folderName = $"{buildName}_v{version}({bundleCode})";
            buildPath = $"Builds/{folderName}";
            string absolutePath = Path.GetFullPath(buildPath);
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                AssetDatabase.Refresh();
            }
        }
        else
        {
            buildPath = setting.androidBuildPath + $"/{buildName}_v{version}({bundleCode})/";
        }

        if (Path.IsPathRooted(buildPath))
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            buildPath = Path.GetRelativePath(projectPath, buildPath);
        }

        string fullBuildPath = Path.Combine(Application.dataPath, "..", buildPath);
        if (!Directory.Exists(fullBuildPath))
        {
            Directory.CreateDirectory(fullBuildPath);
        }

        string fileName = $"{buildPrefix}{buildName}_v{version}({bundleCode}).{(aab ? "aab" : "apk")}";
        string buildFilePath = Path.Combine(buildPath, fileName);
        buildOptions = setting.ResolveBuildOptions(buildOptions);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray(),
            locationPathName = buildFilePath,
            target = BuildTarget.Android,
            options = buildOptions
        };

        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        // Addressables 콘텐츠 빌드 — link.xml/catalog 재생성 보장 (Player 빌드 직전)
        Debug.Log("[Build] Running Addressables BuildPlayerContent before Player build");
        AddressableAssetSettings.BuildPlayerContent();

        Debug.Log($"Starting Android build: path={buildFilePath}, developmentBuild={setting.developmentBuild}, options={buildOptions}");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                string absoluteBuildPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", buildPath));
                Debug.Log($"Build saved to: {absoluteBuildPath}");
                if (!ciMode) EditorUtility.RevealInFinder(absoluteBuildPath);
                break;
            case BuildResult.Failed:
                Debug.LogError($"Build failed with {summary.totalErrors} errors");
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            Debug.LogError($"Build error: {message.content}");
                        }
                    }
                }
                break;
        }

        return report;
    }

    #endregion
}
#endif
}
