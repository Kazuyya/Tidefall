using UnityEditor;
using UnityEngine;
using LittleHeroJourney.UI;

namespace LittleHeroJourney.Editor.UI
{
    [CustomEditor(typeof(HealthBar))]
    public class HealthBarEditor : UnityEditor.Editor
    {
        private SerializedProperty _mainSliderBar;
        private SerializedProperty _fillOnIncrease;
        private SerializedProperty _fillOnDecrease;
        private SerializedProperty _fillDurationIncrease;
        private SerializedProperty _fillDurationDecrease;
        private SerializedProperty _parentAnimTarget;
        private SerializedProperty _animOnIncrease;
        private SerializedProperty _animIncreaseDuration;
        private SerializedProperty _animIncreaseStrength;
        private SerializedProperty _animOnDecrease;
        private SerializedProperty _animDecreaseDuration;
        private SerializedProperty _animDecreaseStrength;
        private SerializedProperty _useGhost;
        private SerializedProperty _useDifferentGhostForIncreaseDecrease;
        private SerializedProperty _ghostBar;
        private SerializedProperty _ghostDuration;
        private SerializedProperty _ghostBarIncrease;
        private SerializedProperty _ghostBarDecrease;
        private SerializedProperty _ghostDurationIncrease;
        private SerializedProperty _ghostDurationDecrease;
        private SerializedProperty _showDebugLog;

        private void OnEnable()
        {
            _mainSliderBar = serializedObject.FindProperty("mainSliderBar");
            _fillOnIncrease = serializedObject.FindProperty("fillOnIncrease");
            _fillOnDecrease = serializedObject.FindProperty("fillOnDecrease");
            _fillDurationIncrease = serializedObject.FindProperty("fillDurationIncrease");
            _fillDurationDecrease = serializedObject.FindProperty("fillDurationDecrease");
            _parentAnimTarget = serializedObject.FindProperty("parentAnimTarget");
            _animOnIncrease = serializedObject.FindProperty("animOnIncrease");
            _animIncreaseDuration = serializedObject.FindProperty("animIncreaseDuration");
            _animIncreaseStrength = serializedObject.FindProperty("animIncreaseStrength");
            _animOnDecrease = serializedObject.FindProperty("animOnDecrease");
            _animDecreaseDuration = serializedObject.FindProperty("animDecreaseDuration");
            _animDecreaseStrength = serializedObject.FindProperty("animDecreaseStrength");
            _useGhost = serializedObject.FindProperty("useGhost");
            _useDifferentGhostForIncreaseDecrease = serializedObject.FindProperty("useDifferentGhostForIncreaseDecrease");
            _ghostBar = serializedObject.FindProperty("ghostBar");
            _ghostDuration = serializedObject.FindProperty("ghostDuration");
            _ghostBarIncrease = serializedObject.FindProperty("ghostBarIncrease");
            _ghostBarDecrease = serializedObject.FindProperty("ghostBarDecrease");
            _ghostDurationIncrease = serializedObject.FindProperty("ghostDurationIncrease");
            _ghostDurationDecrease = serializedObject.FindProperty("ghostDurationDecrease");
            _showDebugLog = serializedObject.FindProperty("showDebugLog");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Main Bar", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mainSliderBar, new GUIContent("Main Slider Bar"));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Fill Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_fillOnIncrease, new GUIContent("On Increase"));
            EditorGUILayout.PropertyField(_fillOnDecrease, new GUIContent("On Decrease"));
            if (_fillOnIncrease.enumValueIndex == (int)FillAnimationType.Lerp || _fillOnDecrease.enumValueIndex == (int)FillAnimationType.Lerp)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fillDurationIncrease, new GUIContent("Duration Increase"));
                EditorGUILayout.PropertyField(_fillDurationDecrease, new GUIContent("Duration Decrease"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Health Bar Parent Anim", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_parentAnimTarget, new GUIContent("Parent Anim"));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Increase Anim", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animOnIncrease, new GUIContent("Type"));
            if (_animOnIncrease.enumValueIndex != (int)BarAnimationType.None)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_animIncreaseDuration, new GUIContent("Duration"));
                EditorGUILayout.PropertyField(_animIncreaseStrength, new GUIContent("Strength"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Decrease Anim", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animOnDecrease, new GUIContent("Type"));
            if (_animOnDecrease.enumValueIndex != (int)BarAnimationType.None)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_animDecreaseDuration, new GUIContent("Duration"));
                EditorGUILayout.PropertyField(_animDecreaseStrength, new GUIContent("Strength"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Ghost", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useGhost, new GUIContent("Use Ghost"));
            if (_useGhost.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_useDifferentGhostForIncreaseDecrease, new GUIContent("Use different ghost for increase and decrease"));
                if (_useDifferentGhostForIncreaseDecrease.boolValue)
                {
                    EditorGUILayout.PropertyField(_ghostBarIncrease, new GUIContent("Ghost Bar (Increase)"));
                    EditorGUILayout.PropertyField(_ghostDurationIncrease, new GUIContent("Duration Increase"));
                    EditorGUILayout.PropertyField(_ghostBarDecrease, new GUIContent("Ghost Bar (Decrease)"));
                    EditorGUILayout.PropertyField(_ghostDurationDecrease, new GUIContent("Duration Decrease"));
                }
                else
                {
                    EditorGUILayout.PropertyField(_ghostBar, new GUIContent("Ghost Bar"));
                    EditorGUILayout.PropertyField(_ghostDuration, new GUIContent("Duration"));
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_showDebugLog, new GUIContent("Debug Log"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
