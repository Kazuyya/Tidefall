using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomPropertyDrawer(typeof(VFXEffectTiming))]
    public class VFXEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing * 5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(row1, property.FindPropertyRelative("effectName"), new GUIContent("VFX Name"));

            Rect row2 = new Rect(position.x, position.y + (lineHeight + spacing), position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            EditorGUI.PropertyField(row3, property.FindPropertyRelative("positionOffset"), new GUIContent("Position Offset"));

            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            EditorGUI.PropertyField(row4, property.FindPropertyRelative("rotationEuler"), new GUIContent("Rotation (Euler)"));

            Rect row5 = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight);
            EditorGUI.PropertyField(row5, property.FindPropertyRelative("scale"), new GUIContent("Scale"));

            Rect row6 = new Rect(position.x, position.y + (lineHeight + spacing) * 5, position.width, lineHeight);
            EditorGUI.PropertyField(row6, property.FindPropertyRelative("followCharacter"), new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(AudioEffectTiming))]
    public class AudioEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing * 5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(row1, property.FindPropertyRelative("effectName"), new GUIContent("Audio Name"));

            Rect row2 = new Rect(position.x, position.y + (lineHeight + spacing), position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            EditorGUI.PropertyField(row3, property.FindPropertyRelative("positionOffset"), new GUIContent("Position Offset"));

            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            EditorGUI.PropertyField(row4, property.FindPropertyRelative("rotationEuler"), new GUIContent("Rotation (Euler)"));

            Rect row5 = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight);
            EditorGUI.PropertyField(row5, property.FindPropertyRelative("scale"), new GUIContent("Scale"));

            Rect row6 = new Rect(position.x, position.y + (lineHeight + spacing) * 5, position.width, lineHeight);
            EditorGUI.PropertyField(row6, property.FindPropertyRelative("followCharacter"), new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ParticleEffectTiming))]
    public class ParticleEffectTimingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing * 5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect row1 = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(row1, property.FindPropertyRelative("effectName"), new GUIContent("Particle Name"));

            Rect row2 = new Rect(position.x, position.y + (lineHeight + spacing), position.width, lineHeight);
            SerializedProperty timingProp = property.FindPropertyRelative("triggerTime");
            timingProp.floatValue = EditorGUI.Slider(row2, "Trigger At:", timingProp.floatValue, 0f, 1f);

            Rect row3 = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            EditorGUI.PropertyField(row3, property.FindPropertyRelative("positionOffset"), new GUIContent("Position Offset"));

            Rect row4 = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
            EditorGUI.PropertyField(row4, property.FindPropertyRelative("rotationEuler"), new GUIContent("Rotation (Euler)"));

            Rect row5 = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight);
            EditorGUI.PropertyField(row5, property.FindPropertyRelative("scale"), new GUIContent("Scale"));

            Rect row6 = new Rect(position.x, position.y + (lineHeight + spacing) * 5, position.width, lineHeight);
            EditorGUI.PropertyField(row6, property.FindPropertyRelative("followCharacter"), new GUIContent("Follow Character"));

            EditorGUI.EndProperty();
        }
    }
}
