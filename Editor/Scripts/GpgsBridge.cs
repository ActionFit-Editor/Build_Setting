using System;
using System.Reflection;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor
{
    internal static class GpgsBridge
    {
        public static void Sync(string appId, string clientId, string androidPackageName)
        {
            Type settingsType = FindType("GooglePlayGames.Editor.GPGSProjectSettings");
            Type utilType = FindType("GooglePlayGames.Editor.GPGSUtil");
            if (settingsType == null || utilType == null)
            {
                Debug.LogWarning("[GPGS] Google Play Games plugin is not installed. GPGS sync skipped.");
                return;
            }

            object instance = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (instance == null)
            {
                Debug.LogWarning("[GPGS] GPGSProjectSettings.Instance not found. GPGS sync skipped.");
                return;
            }

            Set(settingsType, instance, GetConst(utilType, "APPIDKEY"), appId);
            if (!string.IsNullOrEmpty(clientId) && !clientId.TrimStart().StartsWith("[Enter", StringComparison.Ordinal))
                Set(settingsType, instance, GetConst(utilType, "WEBCLIENTIDKEY"), clientId);
            Set(settingsType, instance, GetConst(utilType, "ANDROIDBUNDLEIDKEY"), androidPackageName);
            Set(settingsType, instance, GetConst(utilType, "ANDROIDSETUPDONEKEY"), true);

            settingsType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public)?.Invoke(instance, null);
            utilType.GetMethod("UpdateGameInfo", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
            utilType.GetMethod("GenerateAndroidManifest", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);

            Debug.Log($"<color=green><b>[GPGS]</b></color> Setup synced: AppId={appId}, Package={androidPackageName}");
        }

        private static void Set(Type settingsType, object instance, string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            settingsType.GetMethod("Set", new[] { typeof(string), typeof(string) })?.Invoke(instance, new object[] { key, value });
        }

        private static void Set(Type settingsType, object instance, string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            settingsType.GetMethod("Set", new[] { typeof(string), typeof(bool) })?.Invoke(instance, new object[] { key, value });
        }

        private static string GetConst(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }
    }
}
