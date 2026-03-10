using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using DG.Tweening;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(LoadingUIController))]
    public class LoadingUIControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty showDebugLogProp;
        private SerializedProperty displayModeProp;
        private SerializedProperty selectionModeProp;
        private SerializedProperty titleTextProp;
        private SerializedProperty bodyTextProp;
        private SerializedProperty backgroundImageProp;
        private SerializedProperty iconImageProp;
        private SerializedProperty progressTextProp;
        private SerializedProperty progressTextFormatProp;
        private SerializedProperty loadingStaticTextProp;
        private SerializedProperty progressBarProp;
        private SerializedProperty spinnerImageProp;
        private SerializedProperty spinnerDurationProp;
        private SerializedProperty fadeDurationProp;
        private SerializedProperty fadeEaseProp;
        private SerializedProperty iconFadeEnabledProp;
        private SerializedProperty backgroundSpriteProp;
        private SerializedProperty overridesProp;
        private SerializedProperty openAnimationsProp;
        private SerializedProperty openPlayModeProp;
        private SerializedProperty openUseCustomDurationProp;
        private SerializedProperty openCustomDurationProp;
        private SerializedProperty closeAnimationsProp;
        private SerializedProperty closePlayModeProp;
        private SerializedProperty closeUseCustomDurationProp;
        private SerializedProperty closeCustomDurationProp;

        private ReorderableList openAnimList;
        private ReorderableList closeAnimList;

        private void OnEnable()
        {
            showDebugLogProp = serializedObject.FindProperty("showDebugLog");
            displayModeProp = serializedObject.FindProperty("displayMode");
            selectionModeProp = serializedObject.FindProperty("selectionMode");
            titleTextProp = serializedObject.FindProperty("titleText");
            bodyTextProp = serializedObject.FindProperty("bodyText");
            backgroundImageProp = serializedObject.FindProperty("backgroundImage");
            iconImageProp = serializedObject.FindProperty("iconImage");
            progressTextProp = serializedObject.FindProperty("progressText");
            progressTextFormatProp = serializedObject.FindProperty("progressTextFormat");
            loadingStaticTextProp = serializedObject.FindProperty("loadingStaticText");
            progressBarProp = serializedObject.FindProperty("progressBar");
            spinnerImageProp = serializedObject.FindProperty("spinnerImage");
            spinnerDurationProp = serializedObject.FindProperty("spinnerDuration");
            fadeDurationProp = serializedObject.FindProperty("fadeDuration");
            fadeEaseProp = serializedObject.FindProperty("fadeEase");
            iconFadeEnabledProp = serializedObject.FindProperty("iconFadeEnabled");
            backgroundSpriteProp = serializedObject.FindProperty("backgroundSprite");
            overridesProp = serializedObject.FindProperty("overrides");
            openAnimationsProp = serializedObject.FindProperty("openAnimations");
            openPlayModeProp = serializedObject.FindProperty("openPlayMode");
            openUseCustomDurationProp = serializedObject.FindProperty("openUseCustomDuration");
            openCustomDurationProp = serializedObject.FindProperty("openCustomDuration");
            closeAnimationsProp = serializedObject.FindProperty("closeAnimations");
            closePlayModeProp = serializedObject.FindProperty("closePlayMode");
            closeUseCustomDurationProp = serializedObject.FindProperty("closeUseCustomDuration");
            closeCustomDurationProp = serializedObject.FindProperty("closeCustomDuration");

            openAnimList = BuildAnimationList(openAnimationsProp, openPlayModeProp, openUseCustomDurationProp, openCustomDurationProp, "In Animation");
            closeAnimList = BuildAnimationList(closeAnimationsProp, closePlayModeProp, closeUseCustomDurationProp, closeCustomDurationProp, "Out Animation");
        }

        private ReorderableList BuildAnimationList(
            SerializedProperty animationsProp,
            SerializedProperty playModeProp,
            SerializedProperty useCustomProp,
            SerializedProperty customDurProp,
            string headerLabel)
        {
            var list = new ReorderableList(serializedObject, animationsProp, true, true, true, true);
            list.drawHeaderCallback = rect =>
            {
                float total = CanvasSequenceEditorHelper.ComputeTotalDurationForAnimations(
                    animationsProp,
                    playModeProp.enumValueIndex == (int)LoadingUIController.SequencePlayMode.Sequential,
                    useCustomProp.boolValue,
                    customDurProp.floatValue);
                EditorGUI.LabelField(rect, $"{headerLabel} (Total: {total:0.###} s)", EditorStyles.boldLabel);
            };
            list.headerHeight = 18f;
            list.elementHeight = 40f;
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var animProp = animationsProp.GetArrayElementAtIndex(index);
                var obj = animProp.objectReferenceValue as DOTweenAnimation;
                string desc = obj != null
                    ? CanvasSequenceEditorHelper.GetAnimationDescription(obj, useCustomProp.boolValue, customDurProp.floatValue)
                    : "[Empty]";
                rect.y += 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 26f, 16f), desc, EditorStyles.miniLabel);
                animProp.objectReferenceValue = EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y + 18f, rect.width - 26f, 18f),
                    animProp.objectReferenceValue,
                    typeof(DOTweenAnimation),
                    true);
                if (GUI.Button(new Rect(rect.x + rect.width - 24f, rect.y, 22f, 36f), "-", EditorStyles.miniButton))
                {
                    int removeAt = index;
                    var tgt = serializedObject.targetObject;
                    string path = animationsProp.propertyPath;
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        var so = new SerializedObject(tgt);
                        var list = so.FindProperty(path);
                        if (list != null && removeAt < list.arraySize) { list.DeleteArrayElementAtIndex(removeAt); so.ApplyModifiedProperties(); }
                    };
                }
            };
            return list;
        }

        private void DrawAnimationListWithOptions(ReorderableList list, SerializedProperty playModeProp, SerializedProperty useCustomProp, SerializedProperty customDurProp)
        {
            EditorGUILayout.PropertyField(playModeProp, new GUIContent("Mode"));
            EditorGUILayout.PropertyField(useCustomProp, new GUIContent("Use Custom Duration"));
            if (useCustomProp.boolValue)
                EditorGUILayout.PropertyField(customDurProp, new GUIContent("Custom Duration"));
            EditorGUILayout.Space(6);
            list.DoLayoutList();
            EditorGUILayout.Space(6);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(showDebugLogProp);
            EditorGUILayout.PropertyField(displayModeProp);
            EditorGUILayout.PropertyField(selectionModeProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Positions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(titleTextProp);
            EditorGUILayout.PropertyField(bodyTextProp);
            EditorGUILayout.PropertyField(backgroundImageProp);
            EditorGUILayout.PropertyField(iconImageProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Loading Text", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(progressTextProp);
            EditorGUILayout.PropertyField(progressTextFormatProp);
            EditorGUILayout.PropertyField(loadingStaticTextProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Progress Bar", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(progressBarProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Spinner", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spinnerImageProp);
            EditorGUILayout.PropertyField(spinnerDurationProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Icon Fade", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(iconFadeEnabledProp);
            EditorGUILayout.PropertyField(fadeDurationProp);
            EditorGUILayout.PropertyField(fadeEaseProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(backgroundSpriteProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Overrides (Title, Body, Background)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(overridesProp, true);

            EditorGUILayout.Space(6);
            DrawAnimationListWithOptions(openAnimList, openPlayModeProp, openUseCustomDurationProp, openCustomDurationProp);
            EditorGUILayout.Space(4);
            DrawAnimationListWithOptions(closeAnimList, closePlayModeProp, closeUseCustomDurationProp, closeCustomDurationProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
