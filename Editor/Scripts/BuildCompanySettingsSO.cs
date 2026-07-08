#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor
{
    [Serializable]
    public class BuildCompanyProfile
    {
        public string companyName = ""; // 회사명
        public string developmentTeamId = ""; // Apple Developer Team ID
    }

    [CreateAssetMenu(fileName = "BuildCompanySettingsSO", menuName = "Build/BuildCompanySettings")]
    public class BuildCompanySettingsSO : ScriptableObject
    {
        #region Fields

        public const string DefaultSettingsAssetPath = "Assets/_Data/_BuildSetting/BuildCompanySettingsSO.asset";
        public const string LegacySettingsAssetPath = "Assets/_Data/_BuildSetting/ActionFitBuildSetting_SO.asset";

        public List<BuildCompanyProfile> companyProfiles = new(); // 회사별 빌드 프로필

        #endregion

        #region Public Methods

        public IReadOnlyList<BuildCompanyProfile> CompanyProfiles => companyProfiles;

        public bool EnsureProfile(string companyName, string developmentTeamId, bool overwriteExisting = false)
        {
            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(developmentTeamId)) return false;
            companyProfiles ??= new List<BuildCompanyProfile>();

            var profile = companyProfiles.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.companyName?.Trim(), companyName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                companyProfiles.Add(new BuildCompanyProfile
                {
                    companyName = companyName.Trim(),
                    developmentTeamId = developmentTeamId.Trim()
                });
                return true;
            }

            bool changed = false;
            if (!string.Equals(profile.companyName?.Trim(), companyName.Trim(), StringComparison.Ordinal))
            {
                profile.companyName = companyName.Trim();
                changed = true;
            }

            if ((overwriteExisting || string.IsNullOrWhiteSpace(profile.developmentTeamId)) &&
                !string.Equals(profile.developmentTeamId?.Trim(), developmentTeamId.Trim(), StringComparison.Ordinal))
            {
                profile.developmentTeamId = developmentTeamId.Trim();
                changed = true;
            }

            return changed;
        }

        public bool TryGetProfile(string companyName, out BuildCompanyProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(companyName)) return false;

            string normalizedCompanyName = companyName.Trim();
            profile = companyProfiles.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.companyName?.Trim(), normalizedCompanyName, StringComparison.OrdinalIgnoreCase));
            return profile != null;
        }

        public bool TryGetDevelopmentTeamId(string companyName, out string developmentTeamId)
        {
            developmentTeamId = "";
            if (!TryGetProfile(companyName, out var profile)) return false;
            if (string.IsNullOrWhiteSpace(profile.developmentTeamId)) return false;

            developmentTeamId = profile.developmentTeamId.Trim();
            return true;
        }

        public static BuildCompanySettingsSO FindOrCreateSettingsAsset()
        {
            var settings = LoadSettingsAsset();
            if (settings == null)
            {
                EnsureFolder(Path.GetDirectoryName(DefaultSettingsAssetPath)?.Replace("\\", "/"));
                settings = CreateInstance<BuildCompanySettingsSO>();
                AssetDatabase.CreateAsset(settings, DefaultSettingsAssetPath);
                Debug.Log($"[BuildCompanySettings] Settings created: {DefaultSettingsAssetPath}");
            }

            return settings;
        }

        #endregion

        #region Private Methods

        private static BuildCompanySettingsSO LoadSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BuildCompanySettingsSO>(DefaultSettingsAssetPath);
            return settings != null
                ? settings
                : AssetDatabase.LoadAssetAtPath<BuildCompanySettingsSO>(LegacySettingsAssetPath);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        #endregion
    }
}

#endif
