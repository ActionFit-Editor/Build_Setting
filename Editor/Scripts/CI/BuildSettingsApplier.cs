#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor
{
    public static class BuildSettingsApplier
    {
        public static void ApplyVersionSettings(BuildSettingsSO settings)
        {
            if (settings == null)
            {
                Debug.LogError("[BuildSettingsApplier] BuildSettingsSO is null");
                return;
            }

            PlayerSettings.bundleVersion = settings.buildVersion;
            PlayerSettings.iOS.buildNumber = settings.bundleNo;

            if (int.TryParse(settings.bundleNo, out int bundleCode))
            {
                PlayerSettings.Android.bundleVersionCode = bundleCode;
            }
            else
            {
                Debug.LogError($"[BuildSettingsApplier] bundleNo is not an integer: {settings.bundleNo}");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildSettingsApplier] Version settings applied: version={settings.buildVersion}, bundleNo={settings.bundleNo}");
        }
    }
}

#endif
