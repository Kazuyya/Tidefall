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
}
