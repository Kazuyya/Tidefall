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
        private SerializedProperty _fillDelayIncrease;
        private SerializedProperty _fillDurationDecrease;
        private SerializedProperty _fillDelayDecrease;
        private SerializedProperty _parentAnimTarget;
        private SerializedProperty _animOnIncrease;
        private SerializedProperty _animIncreaseDuration;
        private SerializedProperty _animIncreaseStrength;
        private SerializedProperty _animOnDecrease;
        private SerializedProperty _animDecreaseDuration;
        private SerializedProperty _animDecreaseStrength;
        private SerializedProperty _useGhost;
        private SerializedProperty _useDifferentGhostForIncreaseDecrease;
        private SerializedProperty _ghostAnimOnIncrease;
        private SerializedProperty _ghostAnimOnDecrease;
        private SerializedProperty _ghostBar;
        private SerializedProperty _ghostDuration;
        private SerializedProperty _ghostIncreaseDelay;
        private SerializedProperty _ghostDecreaseDelay;
        private SerializedProperty _ghostBarIncrease;
        private SerializedProperty _ghostBarDecrease;
        private SerializedProperty _ghostDurationIncrease;
        private SerializedProperty _ghostDurationDecrease;
        private SerializedProperty _ghostIncreaseDelayDual;
        private SerializedProperty _ghostDecreaseDelayDual;
        private SerializedProperty _showDebugLog;

        private void OnEnable()
        {
            _mainSliderBar = serializedObject.FindProperty("mainSliderBar");
            _fillOnIncrease = serializedObject.FindProperty("fillOnIncrease");
            _fillOnDecrease = serializedObject.FindProperty("fillOnDecrease");
            _fillDurationIncrease = serializedObject.FindProperty("fillDurationIncrease");
            _fillDelayIncrease = serializedObject.FindProperty("fillDelayIncrease");
            _fillDurationDecrease = serializedObject.FindProperty("fillDurationDecrease");
            _fillDelayDecrease = serializedObject.FindProperty("fillDelayDecrease");
            _parentAnimTarget = serializedObject.FindProperty("parentAnimTarget");
            _animOnIncrease = serializedObject.FindProperty("animOnIncrease");
            _animIncreaseDuration = serializedObject.FindProperty("animIncreaseDuration");
            _animIncreaseStrength = serializedObject.FindProperty("animIncreaseStrength");
            _animOnDecrease = serializedObject.FindProperty("animOnDecrease");
            _animDecreaseDuration = serializedObject.FindProperty("animDecreaseDuration");
            _animDecreaseStrength = serializedObject.FindProperty("animDecreaseStrength");
            _useGhost = serializedObject.FindProperty("useGhost");
            _useDifferentGhostForIncreaseDecrease = serializedObject.FindProperty("useDifferentGhostForIncreaseDecrease");
            _ghostAnimOnIncrease = serializedObject.FindProperty("ghostAnimOnIncrease");
            _ghostAnimOnDecrease = serializedObject.FindProperty("ghostAnimOnDecrease");
            _ghostBar = serializedObject.FindProperty("ghostBar");
            _ghostDuration = serializedObject.FindProperty("ghostDuration");
            _ghostIncreaseDelay = serializedObject.FindProperty("ghostIncreaseDelay");
            _ghostDecreaseDelay = serializedObject.FindProperty("ghostDecreaseDelay");
            _ghostBarIncrease = serializedObject.FindProperty("ghostBarIncrease");
            _ghostBarDecrease = serializedObject.FindProperty("ghostBarDecrease");
            _ghostDurationIncrease = serializedObject.FindProperty("ghostDurationIncrease");
            _ghostDurationDecrease = serializedObject.FindProperty("ghostDurationDecrease");
            _ghostIncreaseDelayDual = serializedObject.FindProperty("ghostIncreaseDelayDual");
            _ghostDecreaseDelayDual = serializedObject.FindProperty("ghostDecreaseDelayDual");
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
            if (_fillOnIncrease.enumValueIndex == (int)FillAnimationType.Lerp)
            {
                EditorGUILayout.PropertyField(_fillDurationIncrease, new GUIContent("Duration Increase"));
                EditorGUILayout.PropertyField(_fillDelayIncrease, new GUIContent("Delay Increase"));
            }
            EditorGUILayout.PropertyField(_fillOnDecrease, new GUIContent("On Decrease"));
            if (_fillOnDecrease.enumValueIndex == (int)FillAnimationType.Lerp)
            {
                EditorGUILayout.PropertyField(_fillDurationDecrease, new GUIContent("Duration Decrease"));
                EditorGUILayout.PropertyField(_fillDelayDecrease, new GUIContent("Delay Decrease"));
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Health Bar Parent Anim", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_parentAnimTarget, new GUIContent("Parent Anim"));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Increase Anim Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animOnIncrease, new GUIContent("Type"));
            if (_animOnIncrease.enumValueIndex != (int)BarAnimationType.None)
            {
                EditorGUILayout.PropertyField(_animIncreaseDuration, new GUIContent("Duration Increase"));
                EditorGUILayout.PropertyField(_animIncreaseStrength, new GUIContent("Strength Increase"));
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Decrease Anim Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animOnDecrease, new GUIContent("Type"));
            if (_animOnDecrease.enumValueIndex != (int)BarAnimationType.None)
            {
                EditorGUILayout.PropertyField(_animDecreaseDuration, new GUIContent("Duration Decrease"));
                EditorGUILayout.PropertyField(_animDecreaseStrength, new GUIContent("Strength Decrease"));
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
                    EditorGUILayout.LabelField("Ghost Increase", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(_ghostAnimOnIncrease, new GUIContent("Anim"));
                    EditorGUILayout.PropertyField(_ghostBarIncrease, new GUIContent("Ghost Bar (Increase)"));
                    if (_ghostAnimOnIncrease.enumValueIndex == (int)FillAnimationType.Lerp)
                    {
                        EditorGUILayout.PropertyField(_ghostDurationIncrease, new GUIContent("Duration Increase"));
                        EditorGUILayout.PropertyField(_ghostIncreaseDelayDual, new GUIContent("Delay Increase"));
                    }
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Ghost Decrease", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(_ghostAnimOnDecrease, new GUIContent("Anim"));
                    EditorGUILayout.PropertyField(_ghostBarDecrease, new GUIContent("Ghost Bar (Decrease)"));
                    if (_ghostAnimOnDecrease.enumValueIndex == (int)FillAnimationType.Lerp)
                    {
                        EditorGUILayout.PropertyField(_ghostDurationDecrease, new GUIContent("Duration Decrease"));
                        EditorGUILayout.PropertyField(_ghostDecreaseDelayDual, new GUIContent("Delay Decrease"));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Ghost Increase", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(_ghostAnimOnIncrease, new GUIContent("Anim"));
                    EditorGUILayout.PropertyField(_ghostBar, new GUIContent("Ghost Bar"));
                    if (_ghostAnimOnIncrease.enumValueIndex == (int)FillAnimationType.Lerp)
                    {
                        EditorGUILayout.PropertyField(_ghostDuration, new GUIContent("Duration Increase"));
                        EditorGUILayout.PropertyField(_ghostIncreaseDelay, new GUIContent("Delay Increase"));
                    }
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Ghost Decrease", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(_ghostAnimOnDecrease, new GUIContent("Anim"));
                    if (_ghostAnimOnDecrease.enumValueIndex == (int)FillAnimationType.Lerp)
                    {
                        EditorGUILayout.PropertyField(_ghostDuration, new GUIContent("Duration Decrease"));
                        EditorGUILayout.PropertyField(_ghostDecreaseDelay, new GUIContent("Delay Decrease"));
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_showDebugLog, new GUIContent("Debug Log"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
