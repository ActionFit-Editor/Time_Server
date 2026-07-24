#if UNITY_EDITOR
using ActionFit.SOSingleton.Editor;
using UnityEditor;

[InitializeOnLoad]
public static class TimeServerSettingsBootstrap
{
    static TimeServerSettingsBootstrap()
    {
        EditorApplication.delayCall += EnsureAsset;
    }

    private static void EnsureAsset()
    {
        ActionFitSettingsAssetProvider.GetOrCreate<TimeServerSettingsSO>();
    }
}
#endif
