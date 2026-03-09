using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(StorySequenceSO))]
    public class StorySequenceSOEditor : UnityEditor.Editor
    {
        private ReorderableList _stepsList;
        private SerializedProperty _stepsProp;

        private void OnEnable()
        {
            _stepsProp = serializedObject.FindProperty("steps");
            _stepsList = new ReorderableList(serializedObject, _stepsProp, true, true, true, true);
            _stepsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps (play in order)");
            _stepsList.elementHeight = GetStepBlockHeight();
            _stepsList.drawElementCallback = DrawStep;
        }

        private const float ExtraPad = 4f;

        private static float GetStepBlockHeight()
        {
            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;
            float textBlock = line * 6f;
            return 4f * line + 2f * textBlock + 7f * (pad + ExtraPad) + 4f;
        }

        private void DrawStep(Rect rect, int index, bool isActive, bool isFocused)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing + ExtraPad;
            float textBlock = line * 6f;

            SerializedProperty step = _stepsProp.GetArrayElementAtIndex(index);
            SerializedProperty backgroundType = step.FindPropertyRelative("backgroundType");
            SerializedProperty backgroundImage = step.FindPropertyRelative("backgroundImage");
            SerializedProperty backgroundColor = step.FindPropertyRelative("backgroundColor");
            SerializedProperty textTop = step.FindPropertyRelative("textTop");
            SerializedProperty textBottom = step.FindPropertyRelative("textBottom");
            SerializedProperty textInEffect = step.FindPropertyRelative("textInEffect");
            SerializedProperty textOutEffect = step.FindPropertyRelative("textOutEffect");

            bool isCustom = backgroundType.enumValueIndex == (int)StoryBackgroundType.Custom;

            float y = rect.y;
            float x = rect.x;
            float w = rect.width;

            Rect r1 = new Rect(x, y, w, line);
            y += line + pad;
            Rect r2 = new Rect(x, y, w, line);
            y += line + pad;

            y += pad;

            Rect r3 = new Rect(x, y, w, textBlock);
            y += textBlock + pad;
            Rect r4 = new Rect(x, y, w, textBlock);
            y += textBlock + pad;

            y += pad;

            Rect r5 = new Rect(x, y, w, line);
            y += line + pad;
            Rect r6 = new Rect(x, y, w, line);

            EditorGUI.PropertyField(r1, backgroundType, new GUIContent("Background Type"));
            if (isCustom)
                EditorGUI.PropertyField(r2, backgroundImage, new GUIContent("Background Image"));
            else
                EditorGUI.PropertyField(r2, backgroundColor, new GUIContent("Background Color"));
            EditorGUI.PropertyField(r3, textTop, new GUIContent("Text Top (Narration)"), true);
            EditorGUI.PropertyField(r4, textBottom, new GUIContent("Text Bottom (Dialogue)"), true);
            EditorGUI.PropertyField(r5, textInEffect, new GUIContent("Text In Effect"));
            EditorGUI.PropertyField(r6, textOutEffect, new GUIContent("Text Out Effect"));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceId"), new GUIContent("Sequence Id", "Unique id or name (e.g. stage1_intro)."));
            EditorGUILayout.Space(8f);
            _stepsList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
