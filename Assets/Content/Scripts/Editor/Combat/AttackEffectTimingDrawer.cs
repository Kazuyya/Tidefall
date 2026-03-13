using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomPropertyDrawer(typeof(VFXEffectTiming))]
    public class AttackVFXEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Row 1: Effect name
            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            SerializedProperty effectNameProp = property.FindPropertyRelative("effectName");
            EditorGUI.PropertyField(row1, effectNameProp, new GUIContent("VFX Name"));

            // Row 2: Trigger At
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            // Row 3: Position Offset
            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            SerializedProperty offsetProp = property.FindPropertyRelative("positionOffset");
            EditorGUI.PropertyField(row3, offsetProp, new GUIContent("Position Offset"));

            // Row 4: Follow Character
            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            SerializedProperty followProp = property.FindPropertyRelative("followCharacter");
            EditorGUI.PropertyField(row4, followProp, new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(AudioEffectTiming))]
    public class AttackAudioEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Row 1: Effect name
            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            SerializedProperty effectNameProp = property.FindPropertyRelative("effectName");
            EditorGUI.PropertyField(row1, effectNameProp, new GUIContent("Audio Name"));

            // Row 2: Trigger At
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            // Row 3: Position Offset
            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            SerializedProperty offsetProp = property.FindPropertyRelative("positionOffset");
            EditorGUI.PropertyField(row3, offsetProp, new GUIContent("Position Offset"));

            // Row 4: Follow Character
            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            SerializedProperty followProp = property.FindPropertyRelative("followCharacter");
            EditorGUI.PropertyField(row4, followProp, new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ParticleEffectTiming))]
    public class AttackParticleEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Row 1: Effect name
            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            SerializedProperty effectNameProp = property.FindPropertyRelative("effectName");
            EditorGUI.PropertyField(row1, effectNameProp, new GUIContent("Particle Name"));

            // Row 2: Trigger At
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            // Row 3: Position Offset
            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            SerializedProperty offsetProp = property.FindPropertyRelative("positionOffset");
            EditorGUI.PropertyField(row3, offsetProp, new GUIContent("Position Offset"));

            // Row 4: Follow Character
            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            SerializedProperty followProp = property.FindPropertyRelative("followCharacter");
            EditorGUI.PropertyField(row4, followProp, new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TrailEffectTiming))]
    public class AttackTrailEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 4;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float y = position.y;

            Rect row1 = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(row1, property.FindPropertyRelative("effectName"), new GUIContent("Trail Id"));
            y += lineHeight + spacing;

            SerializedProperty windowProp = property.FindPropertyRelative("triggerWindow");
            float min = windowProp.vector2Value.x;
            float max = windowProp.vector2Value.y;
            float labelW = 140f;
            float fieldW = 50f;
            float sliderMinW = 120f;

            Rect labelRect = new Rect(position.x, y, labelW, lineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Open Window", "Start trail at min, stop trail at max (normalized 0-1)"));
            Rect sliderRect = new Rect(position.x + labelW + 5f, y, position.width - labelW - fieldW * 2 - 25f, lineHeight);
            if (sliderRect.width < sliderMinW) sliderRect.width = sliderMinW;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                min = Mathf.Clamp01(min);
                max = Mathf.Clamp01(max);
                if (min > max) max = min;
            }
            float fieldX = position.x + position.width - fieldW * 2 - 15f;
            Rect fieldStartRect = new Rect(fieldX, y, fieldW, lineHeight);
            float newStart = EditorGUI.FloatField(fieldStartRect, min);
            newStart = Mathf.Clamp01(newStart);
            Rect dashRect = new Rect(fieldX + fieldW + 2f, y, 10f, lineHeight);
            EditorGUI.LabelField(dashRect, "-");
            Rect fieldEndRect = new Rect(fieldX + fieldW + 12f, y, fieldW, lineHeight);
            float newEnd = EditorGUI.FloatField(fieldEndRect, max);
            newEnd = Mathf.Clamp01(newEnd);
            min = newStart;
            max = newEnd;
            if (min > max) max = min;
            min = Mathf.Clamp(min, 0f, 1f);
            max = Mathf.Clamp(max, 0f, 1f);
            if (min > max) min = max;
            windowProp.vector2Value = new Vector2(min, max);
            y += lineHeight + spacing;

            Rect row3 = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(row3, property.FindPropertyRelative("stopMode"), new GUIContent("Stop Mode"));
            y += lineHeight + spacing;

            Rect row4 = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(row4, property.FindPropertyRelative("frozenTrailLifetime"), new GUIContent("Frozen Trail Lifetime"));

            EditorGUI.EndProperty();
        }
    }
}
