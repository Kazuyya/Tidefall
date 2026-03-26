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
            EditorGUILayout.LabelField("Water movement (submerged)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "When CharacterWaterSubmersion reports submerged, land foot effects are skipped. " +
                "Fill the sets below for Normal vs Murky water. Shallow = one spawn per CharacterMovementEffect at its transform (left/right emitters). " +
                "Deep = one spawn at body-check center (only one of the emitters is allowed to fire). " +
                "Speed and emit interval use Water tuning settings (no ground ray check in water). Shallow Y follows water surface if enabled.",
                MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Water tuning", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawGlobalTune(serializedObject.FindProperty("waterGlobalSettings"));

            DrawWaterTuneChannel("Particle", "waterParticleUseCustomSettings", "waterParticleCustom");
            DrawWaterTuneChannel("VFX", "waterVfxUseCustomSettings", "waterVfxCustom");
            DrawWaterTuneChannel("Audio", "waterAudioUseCustomSettings", "waterAudioCustom");
            EditorGUILayout.EndVertical();

            DrawWaterIdsBlock("Normal · Shallow", "waterNormalShallow");
            EditorGUILayout.Space(6f);
            DrawWaterIdsBlock("Murky · Shallow", "waterMurkyShallow");
            EditorGUILayout.Space(6f);
            DrawWaterIdsBlock("Normal · Deep", "waterNormalDeep");
            EditorGUILayout.Space(6f);
            DrawWaterIdsBlock("Murky · Deep", "waterMurkyDeep");
            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("waterSurfaceEffectOffset"),
                new GUIContent("Surface offset (all water FX)", "Added to true water surface Y for particle/audio/VFX."));

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("shallowUseSurfaceY"),
                new GUIContent("Shallow follows surface Y", "If true: shallow Y uses water surface + offset. If false: shallow Y uses the emitter transform Y directly."));
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

        private void DrawWaterIdsBlock(string title, string propertyName)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            SerializedProperty block = serializedObject.FindProperty(propertyName);
            if (block == null)
                return;

            EditorGUILayout.PropertyField(block.FindPropertyRelative("particleEffectId"), new GUIContent("Particle ID"));
            EditorGUILayout.PropertyField(block.FindPropertyRelative("vfxEffectId"), new GUIContent("VFX ID"));
            EditorGUILayout.PropertyField(block.FindPropertyRelative("audioEffectId"), new GUIContent("Audio ID"));
        }

        private void DrawWaterTuneChannel(string title, string useCustomPropertyName, string customPropertyName)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            SerializedProperty useCustom = serializedObject.FindProperty(useCustomPropertyName);
            SerializedProperty custom = serializedObject.FindProperty(customPropertyName);
            if (useCustom == null || custom == null)
                return;

            EditorGUILayout.PropertyField(useCustom, new GUIContent("Use Custom Settings"));
            if (!useCustom.boolValue)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawGlobalTune(custom);
            EditorGUILayout.EndVertical();
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
