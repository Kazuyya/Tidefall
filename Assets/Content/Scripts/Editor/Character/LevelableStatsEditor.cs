using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(LevelableStats))]
    public class LevelableStatsEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _level;
        private SerializedProperty _debugPreviewLevel;

        private void OnEnable()
        {
            _config = serializedObject.FindProperty("config");
            _level = serializedObject.FindProperty("level");
            _debugPreviewLevel = serializedObject.FindProperty("debugPreviewLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            int previewLevel = _debugPreviewLevel.intValue;
            if (previewLevel > 0)
            {
                var comp = (LevelableStats)target;
                StatsSnapshot s = comp.GetStatsForLevel(previewLevel);
                float scale = _config.objectReferenceValue is LevelStatsConfigSO cfg ? cfg.GetScale(previewLevel) : 1f;

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Preview Level {previewLevel}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Scale: {scale:F2}  |  HP: {s.maxHealth:F0}  |  ATK: {s.ATK:F0}  |  DEF: {s.DEF:F0}  |  Impact: {s.Impact:F0}");
                if (s.moveSpeed > 0f || s.attackSpeed != 1f || s.attackCooldown > 0f)
                    EditorGUILayout.LabelField($"Move: {s.moveSpeed:F1}  |  AtkSpeed: {s.attackSpeed:F1}x  |  AtkCD: {s.attackCooldown:F1}s");
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
