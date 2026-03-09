using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomPropertyDrawer(typeof(VFXEffectTiming))]
    public class VFXEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
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

            // Row 2: Trigger timing with slider
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(AudioEffectTiming))]
    public class AudioEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
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

            // Row 2: Trigger timing with slider
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ParticleEffectTiming))]
    public class ParticleEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
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

            // Row 2: Trigger timing with slider
            Rect row2 = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            EditorGUI.EndProperty();
        }
    }
}
