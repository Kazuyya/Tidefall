using UnityEditor;
using UnityEngine;

namespace LittleHeroJourney.EditorTools
{
    [CustomEditor(typeof(CharacterMovementEffect))]
    public class CharacterMovementEffectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6f);

            EditorGUILayout.LabelField("Ground", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groundLayers"), new GUIContent("Ground Layer"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Global settings", EditorStyles.boldLabel);
            DrawGlobalTune(serializedObject.FindProperty("globalSettings"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Movement effects", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawChannelBlock(
                "Particle",
                "particleEffectId",
                "particleUseCustomSettings",
                "particleCustom");

            EditorGUILayout.Space(8f);
            DrawChannelBlock(
                "VFX",
                "vfxEffectId",
                "vfxUseCustomSettings",
                "vfxCustom");

            EditorGUILayout.Space(8f);
            DrawChannelBlock(
                "Audio",
                "audioEffectId",
                "audioUseCustomSettings",
                "audioCustom");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("showGizmos"),
                new GUIContent("Show Gizmos", "Draws ground debug in Scene view (global ray always; colored rays when Effect IDs are set). No need to select this object."));
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawGlobalTune(SerializedProperty tune)
        {
            if (tune == null)
                return;

            EditorGUILayout.PropertyField(tune.FindPropertyRelative("minSpeed"), new GUIContent("Speed"));
            EditorGUILayout.PropertyField(tune.FindPropertyRelative("emitInterval"), new GUIContent("Interval"));
            EditorGUILayout.PropertyField(tune.FindPropertyRelative("groundRayLength"), new GUIContent("Ray"));
        }

        private void DrawChannelBlock(
            string title,
            string idPropertyName,
            string useCustomPropertyName,
            string customPropertyName)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            SerializedProperty idProp = serializedObject.FindProperty(idPropertyName);
            SerializedProperty useCustomProp = serializedObject.FindProperty(useCustomPropertyName);
            SerializedProperty customProp = serializedObject.FindProperty(customPropertyName);

            if (idProp != null)
                EditorGUILayout.PropertyField(idProp, new GUIContent("Effect ID"));

            if (useCustomProp == null)
                return;

            EditorGUILayout.PropertyField(useCustomProp, new GUIContent("Use Custom Settings"));
            bool showCustom = useCustomProp.boolValue;
            if (!showCustom)
                return;

            if (customProp == null)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Custom settings", EditorStyles.boldLabel);
            DrawGlobalTune(customProp);
            EditorGUILayout.EndVertical();
        }
    }
}
