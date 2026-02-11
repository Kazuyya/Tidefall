using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using LittleHeroJourney;
using System.Collections.Generic;

namespace LittleHeroJourneyEditors
{
    [CustomEditor(typeof(SplashScreenManager))]
    public class SplashScreenManagerEditor : UnityEditor.Editor
    {
        private ReorderableList _seqList;
        private SerializedProperty _seqProp;
        private readonly System.Collections.Generic.Dictionary<string, ReorderableList> _animListCache = new System.Collections.Generic.Dictionary<string, ReorderableList>();

        private const float LineH = 20f;
        private const float Pad = 2f;

        public override void OnInspectorGUI()
        {
            if (_seqList == null) return;
            serializedObject.Update();
            _seqList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            _seqProp = serializedObject.FindProperty("sequence");
            _seqList = new ReorderableList(serializedObject, _seqProp, true, true, true, true);
            _seqList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Splash Sequence", EditorStyles.boldLabel);
            _seqList.elementHeightCallback = index =>
            {
                var element = _seqProp.GetArrayElementAtIndex(index);
                var inPhaseProp = element.FindPropertyRelative("inPhase");
                var waitPhaseProp = element.FindPropertyRelative("waitPhase");
                var outPhaseProp = element.FindPropertyRelative("outPhase");
                float h = LineH + Pad; // Target GameObject
                h += (LineH + Pad); // "In Animation" label spacing
                h += GetPhaseHeight(inPhaseProp); // In phase content
                h += (LineH + Pad); // "Wait Phase" label spacing
                h += GetWaitHeight(waitPhaseProp); // Wait phase (duration field)
                h += (LineH + Pad); // "Out Animation" label spacing
                h += GetPhaseHeight(outPhaseProp); // Out phase content
                return h + 16f;
            };
            _seqList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _seqProp.GetArrayElementAtIndex(index);
                var canvasProp = element.FindPropertyRelative("targetGameObject");
                var inPhaseProp = element.FindPropertyRelative("inPhase");
                var waitPhaseProp = element.FindPropertyRelative("waitPhase");
                var outPhaseProp = element.FindPropertyRelative("outPhase");

                float y = rect.y + 4f;
                float w = rect.width - 4f;

                EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), canvasProp, new GUIContent("Target GameObject"));
                y += LineH + Pad;

                EditorGUI.LabelField(new Rect(rect.x, y, w, LineH), "In Animation", EditorStyles.miniBoldLabel);
                y += LineH + Pad;
                DrawPhaseBlock(ref y, rect.x, w, inPhaseProp, canvasProp, _seqProp, index, 0);

                EditorGUI.LabelField(new Rect(rect.x, y, w, LineH), "Wait Phase", EditorStyles.miniBoldLabel);
                y += LineH + Pad;
                var waitDurationProp = waitPhaseProp.FindPropertyRelative("duration");
                EditorGUI.PropertyField(new Rect(rect.x + 12f, y, w - 12f, LineH), waitDurationProp, new GUIContent("Duration (s)"));
                y += LineH + Pad;

                EditorGUI.LabelField(new Rect(rect.x, y, w, LineH), "Out Animation", EditorStyles.miniBoldLabel);
                y += LineH + Pad;
                DrawPhaseBlock(ref y, rect.x, w, outPhaseProp, canvasProp, _seqProp, index, 2);
            };
        }

        private float GetPhaseHeight(SerializedProperty phaseProp)
        {
            var animsProp = phaseProp.FindPropertyRelative("animations");
            var useCustomProp = phaseProp.FindPropertyRelative("useCustomDuration");
            int n = animsProp?.arraySize ?? 0;
            
            // ReorderableList actual height: header(18) + elements(38*n) + footer(28)
            float listH;
            if (n == 0)
                listH = 80f; // Proper height for empty list
            else
                listH = 18f + n * 38f + 28f + 8f; // Proper buffer
            
            float height = listH + Pad;        // After DoList
            height += LineH + Pad;              // After Total label
            height += LineH + Pad;              // After Mode property
            height += LineH + Pad;              // After "Optional: Override Duration" label
            height += LineH + Pad;              // After UseCustom checkbox
            
            if (useCustomProp.boolValue)
            {
                height += LineH + Pad;          // CustomDuration field
            }
            
            return height;
        }

        private float GetWaitHeight(SerializedProperty waitPhaseProp)
        {
            return LineH + Pad;
        }

        private void DrawPhaseBlock(ref float y, float x, float w, SerializedProperty phaseProp, SerializedProperty containerProp, SerializedProperty seqProp, int stepIdx, int phaseIdx)
        {
            var animsProp = phaseProp.FindPropertyRelative("animations");
            var modeProp = phaseProp.FindPropertyRelative("playMode");
            var useCustomProp = phaseProp.FindPropertyRelative("useCustomDuration");
            var customDurProp = phaseProp.FindPropertyRelative("customDuration");

            float indent = 12f;
            float innerW = w - indent;
            var containerObj = containerProp.objectReferenceValue as GameObject;
            string cacheKey = animsProp.propertyPath;

            if (!_animListCache.TryGetValue(cacheKey, out var animList))
            {
                animList = new ReorderableList(serializedObject, animsProp, true, true, true, true);
                animList.headerHeight = 18f;
                animList.elementHeight = 38f;
                animList.drawElementCallback = (rect, i, isActive, isFocused) =>
                {
                    var ap = animsProp.GetArrayElementAtIndex(i);
                    var obj = ap.objectReferenceValue as DG.Tweening.DOTweenAnimation;
                    string desc = obj != null ? GetAnimDesc(obj, useCustomProp.boolValue, customDurProp.floatValue, containerObj) : "[Empty]";
                    rect.y += 2;
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 26f, 16f), desc, EditorStyles.miniLabel);
                    ap.objectReferenceValue = EditorGUI.ObjectField(new Rect(rect.x, rect.y + 18f, rect.width - 26f, 16f), ap.objectReferenceValue, typeof(DG.Tweening.DOTweenAnimation), true);
                    if (GUI.Button(new Rect(rect.x + rect.width - 24f, rect.y, 22f, 34f), "-", EditorStyles.miniButton))
                    {
                        int ri = i;
                        var tgt = serializedObject.targetObject;
                        string pn = phaseIdx == 0 ? "inPhase" : "outPhase";
                        EditorApplication.delayCall += () =>
                        {
                            var so = new SerializedObject(tgt);
                            var seq = so.FindProperty("sequence");
                            if (seq != null && stepIdx < seq.arraySize)
                            {
                                var elem = seq.GetArrayElementAtIndex(stepIdx);
                                var phase = elem.FindPropertyRelative(pn);
                                var list = phase?.FindPropertyRelative("animations");
                                if (list != null && ri < list.arraySize) { list.DeleteArrayElementAtIndex(ri); so.ApplyModifiedProperties(); _animListCache.Clear(); }
                            }
                        };
                    }
                };
                animList.onAddCallback = _ => animsProp.arraySize++;
                _animListCache[cacheKey] = animList;
            }
            float listH = animList.GetHeight();
            animList.DoList(new Rect(x, y, w, listH));
            y += listH + Pad;

            bool isSeq = modeProp.enumValueIndex == 0;
            var list2 = new List<DG.Tweening.DOTweenAnimation>(animsProp.arraySize);
            for (int j = 0; j < animsProp.arraySize; j++)
            {
                var a = animsProp.GetArrayElementAtIndex(j).objectReferenceValue as DG.Tweening.DOTweenAnimation;
                if (a != null) list2.Add(a);
            }
            float total = Helper.GetSequenceTotalDuration(list2, isSeq, useCustomProp.boolValue, customDurProp.floatValue);
            EditorGUI.LabelField(new Rect(x + indent, y, innerW, LineH), $"Total: {total:F2}s ({(isSeq ? "Sequential" : "Parallel")})", EditorStyles.helpBox);
            y += LineH + Pad;

            EditorGUI.PropertyField(new Rect(x + indent, y, innerW, LineH), modeProp, new GUIContent("Mode"));
            y += LineH + Pad;
            
            // Draw "Optional: Override Duration" section
            EditorGUI.LabelField(new Rect(x + indent, y, innerW, LineH), "Optional: Override Duration", EditorStyles.miniBoldLabel);
            y += LineH + Pad;
            
            EditorGUI.PropertyField(new Rect(x + indent, y, innerW, LineH), useCustomProp, new GUIContent("Use Custom Duration"));
            y += LineH + Pad;
            
            if (useCustomProp.boolValue)
            {
                EditorGUI.PropertyField(new Rect(x + indent, y, innerW, LineH), customDurProp, new GUIContent("Duration (s)"));
                y += LineH + Pad;
            }
        }

        private static string GetAnimDesc(DG.Tweening.DOTweenAnimation obj, bool useCustom, float customDur, GameObject container)
        {
            if (obj == null) return "null";
            string type = obj.animationType.ToString();
            string name = obj.gameObject != null ? obj.gameObject.name : "?";
            float dur = Helper.GetTweenEffectiveDuration(obj, useCustom, customDur);
            bool self = (container != null && obj.gameObject == container);
            return $"{name} - {type} {(self ? "(Self)" : "(Other)")} - {dur:F2}s";
        }
    }
}
