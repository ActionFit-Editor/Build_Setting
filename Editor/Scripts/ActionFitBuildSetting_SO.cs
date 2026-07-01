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
    public class ActionFitBuildCompanyProfile
    {
        public string companyName = ""; // 회사명
        public string developmentTeamId = ""; // Apple Developer Team ID
    }

    [CreateAssetMenu(fileName = "ActionFitBuildSetting_SO", menuName = "ActionFit/BuildSetting/ActionFitBuildSetting")]
    public class ActionFitBuildSetting_SO : ScriptableObject
    {
        #region Fields

        public const string DefaultSettingsAssetPath = "Assets/_Data/_BuildSetting/ActionFitBuildSetting_SO.asset";
        public const string ActionFitCompanyName = "ActionFit";
        public const string ActionFitDevelopmentTeamId = "49W7A8489P";
        public const string StormbornCompanyName = "Stormborn";
        public const string StormbornDevelopmentTeamId = "MCTHBCST32";

        public List<ActionFitBuildCompanyProfile> companyProfiles = new(); // 회사별 빌드 프로필

        #endregion

        #region Public Methods

        public IReadOnlyList<ActionFitBuildCompanyProfile> CompanyProfiles => companyProfiles;

        public bool EnsureDefaultProfiles()
        {
            bool changed = false;
            changed |= EnsureProfile(ActionFitCompanyName, ActionFitDevelopmentTeamId);
            changed |= EnsureProfile(StormbornCompanyName, StormbornDevelopmentTeamId);
            return changed;
        }

        public bool TryGetProfile(string companyName, out ActionFitBuildCompanyProfile profile)
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

        public static ActionFitBuildSetting_SO FindOrCreateSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ActionFitBuildSetting_SO>(DefaultSettingsAssetPath);
            if (settings == null)
            {
                EnsureFolder(Path.GetDirectoryName(DefaultSettingsAssetPath)?.Replace("\\", "/"));
                settings = CreateInstance<ActionFitBuildSetting_SO>();
                AssetDatabase.CreateAsset(settings, DefaultSettingsAssetPath);
                Debug.Log($"[ActionFitBuildSetting] Settings created: {DefaultSettingsAssetPath}");
            }

            if (settings.EnsureDefaultProfiles())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        #endregion

        #region Private Methods

        private bool EnsureProfile(string companyName, string developmentTeamId)
        {
            var profile = companyProfiles.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.companyName?.Trim(), companyName, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
            {
                companyProfiles.Add(new ActionFitBuildCompanyProfile
                {
                    companyName = companyName,
                    developmentTeamId = developmentTeamId
                });
                return true;
            }

            bool changed = false;
            if (string.IsNullOrWhiteSpace(profile.companyName))
            {
                profile.companyName = companyName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(profile.developmentTeamId))
            {
                profile.developmentTeamId = developmentTeamId;
                changed = true;
            }

            return changed;
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

    [InitializeOnLoad]
    public static class ActionFitBuildSettingBootstrap
    {
        static ActionFitBuildSettingBootstrap()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            ActionFitBuildSetting_SO.FindOrCreateSettingsAsset();
        }
    }
}

#endif
