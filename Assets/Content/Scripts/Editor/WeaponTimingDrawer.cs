using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomPropertyDrawer(typeof(WeaponTiming))]
    public class WeaponTimingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Row 1: Weapon name
            Rect weaponNameRect = new Rect(position.x, position.y, position.width, lineHeight);
            SerializedProperty weaponName = property.FindPropertyRelative("weaponName");
            EditorGUI.PropertyField(weaponNameRect, weaponName, new GUIContent("Weapon Name"));

            // Row 2: Timing controls - label left, slider/fields right
            Rect sliderRowRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

            // Label on the left
            float labelWidth = 80f;
            Rect labelRect = new Rect(sliderRowRect.x, sliderRowRect.y, labelWidth, lineHeight);
            EditorGUI.LabelField(labelRect, "Hit Window:");

            // Calculate remaining space for controls (anchored to right)
            float remainingWidth = sliderRowRect.width - labelWidth - 10; // 10px gap
            float controlsStartX = sliderRowRect.x + sliderRowRect.width - remainingWidth;

            // Controls from right to left
            float fieldWidth = 45f;
            float dashWidth = 10f;
            float sliderWidth = remainingWidth - (fieldWidth * 2 + dashWidth);

            float currentX = sliderRowRect.xMax;

            // End field (rightmost)
            currentX -= fieldWidth;
            Rect endFieldRect = new Rect(currentX, sliderRowRect.y, fieldWidth, lineHeight);

            // Dash
            currentX -= dashWidth;
            Rect dashRect = new Rect(currentX, sliderRowRect.y, dashWidth, lineHeight);

            // Start field
            currentX -= fieldWidth;
            Rect startFieldRect = new Rect(currentX, sliderRowRect.y, fieldWidth, lineHeight);

            // Slider takes remaining space
            currentX -= sliderWidth;
            Rect sliderRect = new Rect(currentX, sliderRowRect.y, sliderWidth, lineHeight);

            // Draw timing controls
            SerializedProperty timingWindow = property.FindPropertyRelative("colliderTriggerWindow");
            float min = timingWindow.vector2Value.x;
            float max = timingWindow.vector2Value.y;

            // Min-max slider
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0f, 1f);

            // Editable start field
            float newStart = EditorGUI.FloatField(startFieldRect, min);
            newStart = Mathf.Clamp01(newStart);

            // Dash label
            EditorGUI.LabelField(dashRect, "-");

            // Editable end field
            float newEnd = EditorGUI.FloatField(endFieldRect, max);
            newEnd = Mathf.Clamp01(newEnd);

            // Update values
            min = newStart;
            max = newEnd;
            if (min > max) max = min;

            timingWindow.vector2Value = new Vector2(min, max);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}