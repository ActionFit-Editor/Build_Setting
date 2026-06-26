using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor
{
    internal static class CustomSymbolsBridge
    {
        public static ScriptableObject FindSettingsAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:CustomSymbolsSO");
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }

        public static List<string> GetPlatformSymbols(ScriptableObject settings, BuildTarget target)
        {
            return InvokeSymbolList(settings, "GetPlatformSymbols", target);
        }

        public static List<string> GetBuildSymbols(ScriptableObject settings, BuildTarget target)
        {
            return InvokeSymbolList(settings, "GetBuildSymbols", target);
        }

        public static List<string> GetExcludedSymbols(ScriptableObject settings, BuildTarget target)
        {
            return InvokeSymbolList(settings, "GetExcludedSymbols", target);
        }

        private static List<string> InvokeSymbolList(ScriptableObject settings, string methodName, BuildTarget target)
        {
            if (settings == null) return new List<string>();

            MethodInfo method = settings.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null) return new List<string>();

            return method.Invoke(settings, new object[] { target }) as List<string> ?? new List<string>();
        }
    }
}
