#if UNITY_EDITOR
using ActionFit.SOSingleton.Editor;
using UnityEditor;
using UnityEngine;

namespace ActionFit.Time.Server.Editor
{
    public static class TimeServerPackageMenu
    {
        private const string MenuRoot = "Tools/Package/ActionFit Time Server/";
        private const string ReadmePath = "Packages/com.actionfit.time.server/README.md";
        private const int SettingPriority = 650;
        private const int ReadmePriority = 651;

        [MenuItem(MenuRoot + "Setting SO", false, SettingPriority)]
        private static void FocusSettings()
        {
            ActionFitSettingsAssetResolution result =
                ActionFitSettingsAssetProvider.Resolve(typeof(TimeServerSettingsSO), true);
            if (!result.IsSuccess || result.Asset == null)
            {
                EditorUtility.DisplayDialog(
                    "Time Server Settings",
                    $"설정 에셋을 확인할 수 없습니다.\n{result.Status}: {result.Diagnostic}",
                    "확인");
                return;
            }

            Selection.activeObject = result.Asset;
            EditorGUIUtility.PingObject(result.Asset);
        }

        [MenuItem(MenuRoot + "README", false, ReadmePriority)]
        private static void OpenReadme()
        {
            var readme = AssetDatabase.LoadAssetAtPath<TextAsset>(ReadmePath);
            if (readme == null)
            {
                EditorUtility.DisplayDialog(
                    "Package README",
                    $"README was not found.\n{ReadmePath}",
                    "OK");
                return;
            }

            Selection.activeObject = readme;
            AssetDatabase.OpenAsset(readme);
        }
    }
}
#endif
