using System.Collections.Generic;
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
        private List<Vector2> _narrativeScroll = new List<Vector2>();
        private List<Vector2> _dialogueScroll = new List<Vector2>();
        private List<bool> _stepExpanded = new List<bool>();
        private const float Pad = 2f;
        private const float TextBlockLines = 6f;
        private const float EmptyListHeight = 24f;
        private const float CollapsedLineHeight = 20f;

        private void OnEnable()
        {
            _stepsProp = serializedObject.FindProperty("steps");
            _stepsList = new ReorderableList(serializedObject, _stepsProp, true, true, true, true);
            _stepsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps (play in order)", EditorStyles.boldLabel);
            _stepsList.drawElementCallback = DrawStep;
            _stepsList.drawNoneElementCallback = rect => EditorGUI.LabelField(rect, "List is empty", EditorStyles.centeredGreyMiniLabel);
            _stepsList.elementHeightCallback = GetStepHeight;
        }

        private float GetStepHeight(int index)
        {
            if (index < 0 || index >= _stepsProp.arraySize) return CollapsedLineHeight;
            if (!IsStepExpanded(index)) return CollapsedLineHeight;
            SerializedProperty step = _stepsProp.GetArrayElementAtIndex(index);
            SerializedProperty backgroundType = step.FindPropertyRelative("backgroundType");
            bool isCustom = backgroundType != null && backgroundType.enumValueIndex == (int)StoryBackgroundType.Custom;
            return GetStepBlockHeight(isCustom);
        }

        private static float GetStepBlockHeight(bool includeBackgroundAnimation)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float textBlock = line * TextBlockLines;
            float baseH = 1f * line + Pad + 4f * line + 1f * line + Pad + (1f * line + Pad + textBlock + line + Pad) + Pad + 2f * line + Pad + 1f * line + Pad;
            if (includeBackgroundAnimation)
                baseH += 1f * line + Pad + 4f * line + 2f * Pad;
            return baseH;
        }

        private bool IsStepExpanded(int index)
        {
            while (_stepExpanded.Count <= index) _stepExpanded.Add(true);
            return _stepExpanded[index];
        }

        private void SetStepExpanded(int index, bool value)
        {
            while (_stepExpanded.Count <= index) _stepExpanded.Add(true);
            _stepExpanded[index] = value;
        }

        private void EnsureScrollCapacity(int index)
        {
            while (_narrativeScroll.Count <= index) _narrativeScroll.Add(Vector2.zero);
            while (_dialogueScroll.Count <= index) _dialogueScroll.Add(Vector2.zero);
        }

        private void DrawTextAreaScrollable(Rect rect, SerializedProperty prop, string label, int stepIndex, bool isNarrative)
        {
            float line = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, line), label);
            Rect viewRect = new Rect(rect.x, rect.y + line + Pad, rect.width, line * TextBlockLines);
            string text = prop.stringValue ?? "";
            float contentWidth = viewRect.width;
            float contentHeight = EditorStyles.textArea.CalcHeight(new GUIContent(text), contentWidth);
            contentHeight = Mathf.Max(viewRect.height, contentHeight, line * 2f);
            bool needScroll = contentHeight > viewRect.height;
            if (needScroll)
                contentWidth = viewRect.width - 16f;
            Vector2 scrollPos = isNarrative ? _narrativeScroll[stepIndex] : _dialogueScroll[stepIndex];
            if (needScroll)
            {
                Rect contentRect = new Rect(0, 0, contentWidth, contentHeight);
                scrollPos = GUI.BeginScrollView(viewRect, scrollPos, contentRect);
                if (isNarrative)
                    _narrativeScroll[stepIndex] = scrollPos;
                else
                    _dialogueScroll[stepIndex] = scrollPos;
            }
            Rect textRect = needScroll ? new Rect(0, 0, contentWidth, contentHeight) : viewRect;
            EditorGUI.BeginChangeCheck();
            string val = EditorGUI.TextArea(textRect, text, EditorStyles.textArea);
            if (EditorGUI.EndChangeCheck())
                prop.stringValue = val;
            if (needScroll)
                GUI.EndScrollView();
        }

        private void DrawStep(Rect rect, int index, bool isActive, bool isFocused)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float textBlock = line * TextBlockLines;

            SerializedProperty step = _stepsProp.GetArrayElementAtIndex(index);
            SerializedProperty stepId = step.FindPropertyRelative("stepId");
            string idLabel = string.IsNullOrEmpty(stepId.stringValue) ? $"Step {index}" : stepId.stringValue;
            bool expanded = IsStepExpanded(index);

            Rect foldRect = new Rect(rect.x, rect.y, rect.width, line);
            if (EditorGUI.Foldout(foldRect, expanded, idLabel, true) != expanded)
                SetStepExpanded(index, !expanded);
            if (!expanded)
                return;

            float y = rect.y + line + Pad;
            float x = rect.x;
            float w = rect.width;

            Rect r0 = new Rect(x, y, w, line);
            y += line + Pad;
            EditorGUI.PropertyField(r0, stepId, new GUIContent("Id"));

            SerializedProperty backgroundType = step.FindPropertyRelative("backgroundType");
            SerializedProperty backgroundImage = step.FindPropertyRelative("backgroundImage");
            SerializedProperty backgroundColor = step.FindPropertyRelative("backgroundColor");
            SerializedProperty contentType = step.FindPropertyRelative("contentType");
            SerializedProperty narrativeText = step.FindPropertyRelative("narrativeText");
            SerializedProperty speakerName = step.FindPropertyRelative("speakerName");
            SerializedProperty dialogueLine = step.FindPropertyRelative("dialogueLine");
            SerializedProperty textInEffect = step.FindPropertyRelative("textInEffect");
            SerializedProperty textOutEffect = step.FindPropertyRelative("textOutEffect");
            SerializedProperty delayAfterTextComplete = step.FindPropertyRelative("delayAfterTextComplete");
            SerializedProperty useCustomImageFadeIn = step.FindPropertyRelative("useCustomImageFadeIn");
            SerializedProperty customImageFadeInDuration = step.FindPropertyRelative("customImageFadeInDuration");
            SerializedProperty useCustomImageFadeOutOnExit = step.FindPropertyRelative("useCustomImageFadeOutOnExit");

            Rect r1 = new Rect(x, y, w, line);
            y += line + Pad;
            Rect r2 = new Rect(x, y, w, line);
            y += line + Pad;
            Rect r3 = new Rect(x, y, w, line);
            y += line + Pad;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r1, backgroundType, new GUIContent("Background Type"));
            if (EditorGUI.EndChangeCheck())
            {
                if (backgroundType.enumValueIndex == (int)StoryBackgroundType.Custom)
                    backgroundColor.colorValue = Color.white;
                else
                    backgroundImage.objectReferenceValue = null;
            }

            bool isCustom = backgroundType.enumValueIndex == (int)StoryBackgroundType.Custom;
            if (isCustom)
                EditorGUI.PropertyField(r2, backgroundImage, new GUIContent("Background Image"));
            else
                EditorGUI.PropertyField(r2, backgroundColor, new GUIContent("Background Color"));

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r3, contentType, new GUIContent("Content Type"));
            if (EditorGUI.EndChangeCheck())
            {
                if (contentType.enumValueIndex == (int)StoryContentType.Narrative)
                {
                    speakerName.stringValue = "";
                    dialogueLine.stringValue = "";
                }
                else
                {
                    narrativeText.stringValue = "";
                }
            }

            y += Pad;
            EnsureScrollCapacity(index);
            bool isNarrative = contentType.enumValueIndex == (int)StoryContentType.Narrative;

            if (isNarrative)
            {
                Rect rNarr = new Rect(x, y, w, textBlock + line + Pad);
                DrawTextAreaScrollable(rNarr, narrativeText, "Narrative", index, true);
                y += textBlock + line + Pad;
            }
            else
            {
                Rect r4 = new Rect(x, y, w, line);
                y += line + Pad;
                EditorGUI.PropertyField(r4, speakerName, new GUIContent("Speaker"));
                Rect rDial = new Rect(x, y, w, textBlock + line + Pad);
                DrawTextAreaScrollable(rDial, dialogueLine, "Dialogue", index, false);
                y += textBlock + line + Pad;
            }

            y += Pad;
            Rect rIn = new Rect(x, y, w, line);
            y += line + Pad;
            Rect rOut = new Rect(x, y, w, line);
            y += line + Pad;
            Rect rDelay = new Rect(x, y, w, line);
            y += line + Pad;
            EditorGUI.PropertyField(rIn, textInEffect, new GUIContent("Text In Effect"));
            EditorGUI.PropertyField(rOut, textOutEffect, new GUIContent("Text Out Effect"));
            EditorGUI.PropertyField(rDelay, delayAfterTextComplete, new GUIContent("Delay After Text"));

            if (isCustom)
            {
                y += Pad;
                EditorGUI.LabelField(new Rect(x, y, w, line), "Background animation (Custom only)", EditorStyles.miniLabel);
                y += line + Pad;
                Rect rBg1 = new Rect(x, y, w, line);
                y += line + Pad;
                Rect rBg2 = new Rect(x, y, w, line);
                y += line + Pad;
                Rect rBg3 = new Rect(x, y, w, line);
                EditorGUI.PropertyField(rBg1, useCustomImageFadeIn, new GUIContent("Use fade in"));
                EditorGUI.PropertyField(rBg2, customImageFadeInDuration, new GUIContent("Fade in duration"));
                EditorGUI.PropertyField(rBg3, useCustomImageFadeOutOnExit, new GUIContent("Fade out on exit"));
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceId"), new GUIContent("Sequence Id"));
            EditorGUILayout.Space(8f);
            if (_stepsProp.arraySize == 0)
                _stepsList.elementHeight = EmptyListHeight;
            _stepsList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
