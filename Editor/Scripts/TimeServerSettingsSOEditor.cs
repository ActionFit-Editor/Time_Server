#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimeServerSettingsSO))]
public sealed class TimeServerSettingsSOEditor : Editor
{
    private SerializedProperty _useServerTime;
    private SerializedProperty _calendarMode;
    private SerializedProperty _dayBoundaryOffsetHours;

    private void OnEnable()
    {
        _useServerTime = serializedObject.FindProperty("useServerTime");
        _calendarMode = serializedObject.FindProperty("calendarMode");
        _dayBoundaryOffsetHours = serializedObject.FindProperty("additionalOffsetHours");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_useServerTime);
        if (_useServerTime.boolValue)
        {
            EditorGUILayout.PropertyField(_calendarMode, new GUIContent(
                "Game Calendar",
                "UTC 또는 각 디바이스의 로컬 달력을 선택합니다."));

            EditorGUILayout.PropertyField(_dayBoundaryOffsetHours, new GUIContent(
                "Day Boundary Offset Hours",
                "선택한 달력의 자정 이동값입니다. Now에서 이 값을 빼므로 음수는 날짜 경계를 앞당깁니다."));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
