using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(AttackDataSO))]
    public class AttackDataSOEditor : Editor
    {
        private AttackDataSO attackData;

        private void OnEnable()
        {
            attackData = (AttackDataSO)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBasicInfoSection();
            DrawAnimationSection();
            DrawWeaponSection();
            DrawTimingWindowsSection();
            DrawAISettingsSection();
            DrawEffectsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBasicInfoSection()
        {
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackType"));
            EditorGUILayout.Space();
        }

        private void DrawAnimationSection()
        {
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackAnimation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationTriggerName"));
            EditorGUILayout.Space();
        }

        private void DrawWeaponSection()
        {
            EditorGUILayout.LabelField("Weapon Configuration", EditorStyles.boldLabel);

            // Weapon timings list
            SerializedProperty weaponTimings = serializedObject.FindProperty("weaponTimings");
            EditorGUILayout.PropertyField(weaponTimings, new GUIContent("Weapon Timings"));

            EditorGUILayout.Space();

            // Damage configuration
            EditorGUILayout.LabelField("Damage Configuration", EditorStyles.boldLabel);
            SerializedProperty attackDamageData = serializedObject.FindProperty("attackDamageData");
            EditorGUILayout.PropertyField(attackDamageData, new GUIContent("Attack Damage Data", "Damage data for this specific attack"));

            if (attackDamageData.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a DamageData asset to define damage for this attack", MessageType.Info);
            }

            EditorGUILayout.Space();
        }

        private void DrawTimingWindowsSection()
        {
            // Combo Window
            DrawTimingWindowSlider("Combo Window", "inputWindow", "Time window for next combo input");

            // Interruptible Window
            DrawTimingWindowSlider("Interruptible Window", "interruptibleWindow", "Time window when attack can be interrupted");

            // Movement Disable Window
            DrawTimingWindowSlider("Movement Disable Window", "movementDisableWindow", "Player movement input is disabled during this window");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("resetComboOnAnimationEnd"));
            EditorGUILayout.Space();
        }

        private void DrawTimingWindowSlider(string label, string propertyName, string tooltip)
        {
            var windowProp = serializedObject.FindProperty(propertyName);
            float min = windowProp.vector2Value.x;
            float max = windowProp.vector2Value.y;

            EditorGUILayout.BeginHorizontal();

            // Label with left-aligned normal width
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(140));

            // Flexible space to push slider to the right
            GUILayout.FlexibleSpace();

            EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, 1f, GUILayout.MinWidth(200), GUILayout.MaxWidth(500));

            // Editable fields on the right - simple format
            float newStart = EditorGUILayout.FloatField(min, GUILayout.Width(50));
            newStart = Mathf.Clamp01(newStart);

            EditorGUILayout.LabelField("-", GUILayout.Width(10));

            float newEnd = EditorGUILayout.FloatField(max, GUILayout.Width(50));
            newEnd = Mathf.Clamp01(newEnd);

            // Update values
            min = newStart;
            max = newEnd;

            // Ensure start <= end
            if (min > max) max = min;

            EditorGUILayout.EndHorizontal();

            // Validate and apply
            min = Mathf.Clamp(min, 0f, 1f);
            max = Mathf.Clamp(max, 0f, 1f);
            if (min > max) min = max;

            windowProp.vector2Value = new Vector2(min, max);

            // Validation warning for invalid ranges
            if (min >= max && max > 0f)
            {
                EditorGUILayout.HelpBox($"Warning: {label} range is invalid (min >= max)", MessageType.Warning);
            }
        }

        private void DrawTimingFields(string propertyName)
        {
            var windowProp = serializedObject.FindProperty(propertyName);
            float min = windowProp.vector2Value.x;
            float max = windowProp.vector2Value.y;

            // Editable fields
            float newStart = EditorGUILayout.FloatField(min, GUILayout.Width(50));
            newStart = Mathf.Clamp01(newStart);

            EditorGUILayout.LabelField("-", GUILayout.Width(10));

            float newEnd = EditorGUILayout.FloatField(max, GUILayout.Width(50));
            newEnd = Mathf.Clamp01(newEnd);

            // Update values
            min = newStart;
            max = newEnd;

            // Ensure start <= end
            if (min > max) max = min;

            windowProp.vector2Value = new Vector2(min, max);
        }

        private void DrawAISettingsSection()
        {
            // Check if attack name contains "AI" (exactly uppercase, not "Ai" or "ai")
            string attackName = attackData.attackName;
            bool isAIAttack = !string.IsNullOrEmpty(attackName) && attackName.Contains("AI");

            if (isAIAttack)
            {
                EditorGUILayout.LabelField("AI Combo Settings", EditorStyles.boldLabel);
                
                SerializedProperty aiComboChance = serializedObject.FindProperty("aiComboContinueChance");
                EditorGUILayout.Slider(aiComboChance, 0f, 1f, new GUIContent("AI Combo Continue Chance", 
                    "Probability for AI to continue combo after this attack (0-1). Only used by AI, player uses inputWindow instead"));
                
                // Show visual indicator
                EditorGUILayout.HelpBox("AI Combo Settings are visible because attack name contains 'AI' (uppercase)", MessageType.Info);
                
                EditorGUILayout.Space();
            }
        }

        private void DrawEffectsSection()
        {
            EditorGUILayout.LabelField("Effects & Audio", EditorStyles.boldLabel);

            // Particle Effects
            DrawEffectList("Particle Effects", "particleEffects");

            // VFX Effects
            DrawEffectList("VFX Effects", "vfxEffects");

            // Audio Effects
            DrawEffectList("Audio Effects", "audioEffects");

            EditorGUILayout.Space();
        }

        private void DrawEffectList(string label, string effectsProperty)
        {
            SerializedProperty effectsProp = serializedObject.FindProperty(effectsProperty);
            EditorGUILayout.PropertyField(effectsProp, new GUIContent(label), true);

            EditorGUILayout.Space(5);
        }

        private void DrawSingleEffect(SerializedProperty effectTimingProp, int index, string effectType)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Row 1: Effect object
            SerializedProperty effectProp = effectTimingProp.FindPropertyRelative("effect");
            EditorGUILayout.PropertyField(effectProp, new GUIContent($"{effectType} {index + 1}"));

            // Row 2: Trigger timing — slider biasa 0–1 (bukan window/range)
            SerializedProperty timingProp = effectTimingProp.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUILayout.Slider("Trigger At:", timingProp.floatValue, 0f, 1f);

            EditorGUILayout.EndVertical();
        }

    }
}