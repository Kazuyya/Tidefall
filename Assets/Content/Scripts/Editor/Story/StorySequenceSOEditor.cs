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
            SerializedProperty contentType = step.FindPropertyRelative("contentType");
            SerializedProperty backgroundInEffect = step.FindPropertyRelative("backgroundInEffect");
            SerializedProperty backgroundOutEffect = step.FindPropertyRelative("backgroundOutEffect");

            bool isNarrative = contentType != null && contentType.enumValueIndex == (int)StoryContentType.Narrative;
            bool inHasDuration = backgroundInEffect != null && backgroundInEffect.enumValueIndex == (int)StoryBackgroundTransitionEffect.Fade;
            bool outHasDuration = backgroundOutEffect != null && backgroundOutEffect.enumValueIndex == (int)StoryBackgroundTransitionEffect.Fade;

            return GetExpandedStepHeight(isNarrative, inHasDuration, outHasDuration);
        }

        private static float GetExpandedStepHeight(bool isNarrative, bool inHasDuration, bool outHasDuration)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float textBlock = line * TextBlockLines;

            float h = 0f;

            // Foldout line
            h += line;

            // Step body starts at +Pad after foldout
            h += Pad;

            // Id
            h += line + Pad;
            // Can skip step
            h += line + Pad;

            // Background Type + Background Image/Color + Content Type
            h += line + Pad;
            h += line + Pad;
            h += line + Pad;

            // Extra pad before text area block
            h += Pad;

            if (isNarrative)
            {
                // Narrative block (label line + pad + text area) as allocated by DrawStep
                h += textBlock + line + Pad;
            }
            else
            {
                // Speaker line
                h += line + Pad;
                // Dialogue block (label line + pad + text area)
                h += textBlock + line + Pad;
            }

            // Pad before text effects
            h += Pad;

            // Text In, Text Out, Delay After Text
            h += line + Pad;
            h += line + Pad;
            h += line + Pad;

            // Pad before background transition section
            h += Pad;

            // Background In + Background Out rows
            h += line + Pad; // Background In
            h += line + Pad; // Background Out

            // Optional durations
            if (inHasDuration) h += line + Pad;
            if (outHasDuration) h += line + Pad;

            return h;
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
            SerializedProperty canSkipStep = step.FindPropertyRelative("canSkipStep");
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

            Rect rSkip = new Rect(x, y, w, line);
            y += line + Pad;
            if (canSkipStep != null)
                EditorGUI.PropertyField(rSkip, canSkipStep, new GUIContent("Can Skip Step"));

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
            SerializedProperty backgroundInEffect = step.FindPropertyRelative("backgroundInEffect");
            SerializedProperty backgroundOutEffect = step.FindPropertyRelative("backgroundOutEffect");
            SerializedProperty backgroundFadeInDuration = step.FindPropertyRelative("backgroundFadeInDuration");
            SerializedProperty backgroundFadeOutDuration = step.FindPropertyRelative("backgroundFadeOutDuration");

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

            y += Pad;
            float labelWidth = 130f;

            // Background In
            Rect bgInLabelRect = new Rect(x, y, labelWidth, line);
            Rect bgInFieldRect = new Rect(x + labelWidth + 4f, y, w - labelWidth - 4f, line);
            EditorGUI.LabelField(bgInLabelRect, "Background In");
            EditorGUI.PropertyField(bgInFieldRect, backgroundInEffect, GUIContent.none);
            y += line + Pad;

            // Fade In Duration (only if In = Fade)
            if (backgroundInEffect.enumValueIndex == (int)StoryBackgroundTransitionEffect.Fade)
            {
                Rect fadeInLabelRect = new Rect(x, y, labelWidth, line);
                Rect fadeInFieldRect = new Rect(x + labelWidth + 4f, y, w - labelWidth - 4f, line);
                EditorGUI.LabelField(fadeInLabelRect, "Fade In Duration");
                EditorGUI.PropertyField(fadeInFieldRect, backgroundFadeInDuration, GUIContent.none);
                y += line + Pad;
            }

            // Background Out
            Rect bgOutLabelRect = new Rect(x, y, labelWidth, line);
            Rect bgOutFieldRect = new Rect(x + labelWidth + 4f, y, w - labelWidth - 4f, line);
            EditorGUI.LabelField(bgOutLabelRect, "Background Out");
            EditorGUI.PropertyField(bgOutFieldRect, backgroundOutEffect, GUIContent.none);
            y += line + Pad;

            // Fade Out Duration (only if Out = Fade)
            if (backgroundOutEffect.enumValueIndex == (int)StoryBackgroundTransitionEffect.Fade)
            {
                Rect fadeOutLabelRect = new Rect(x, y, labelWidth, line);
                Rect fadeOutFieldRect = new Rect(x + labelWidth + 4f, y, w - labelWidth - 4f, line);
                EditorGUI.LabelField(fadeOutLabelRect, "Fade Out Duration");
                EditorGUI.PropertyField(fadeOutFieldRect, backgroundFadeOutDuration, GUIContent.none);
                y += line + Pad;
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
