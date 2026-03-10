using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using DG.Tweening;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(GameCanvas))]
    public class GameCanvasSequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty canvasIdProp;
        private SerializedProperty showDebugLogProp;
        private SerializedProperty disableGameObjectOnCloseProp;
        private SerializedProperty openOnLoadingFinishedProp;
        private SerializedProperty openOnlyInSceneProp;
        private SerializedProperty allowOpenWhenActiveProp;
        private SerializedProperty openSequenceProp;
        private SerializedProperty closeSequenceProp;

        private ReorderableList openList;
        private ReorderableList closeList;
        private readonly System.Collections.Generic.Dictionary<string, ReorderableList> _animListCache = new System.Collections.Generic.Dictionary<string, ReorderableList>();

        private const float LineH = 20f;
        private const float Pad = 2f;

        private void OnEnable()
        {
            canvasIdProp = serializedObject.FindProperty("canvasID");
            showDebugLogProp = serializedObject.FindProperty("showDebugLog");
            disableGameObjectOnCloseProp = serializedObject.FindProperty("disableGameObjectOnClose");
            openOnLoadingFinishedProp = serializedObject.FindProperty("openOnLoadingFinished");
            openOnlyInSceneProp = serializedObject.FindProperty("openOnlyInScene");
            allowOpenWhenActiveProp = serializedObject.FindProperty("allowOpenWhenActive");
            openSequenceProp = serializedObject.FindProperty("openSequence");
            closeSequenceProp = serializedObject.FindProperty("closeSequence");

            openList = BuildStepList(openSequenceProp, "In Animation");
            closeList = BuildStepList(closeSequenceProp, "Out Animation");
        }

        private ReorderableList BuildStepList(SerializedProperty listProp, string headerLabel)
        {
            var list = new ReorderableList(serializedObject, listProp, true, true, true, true);
            list.drawHeaderCallback = rect =>
            {
                float total = CanvasSequenceEditorHelper.ComputeGameCanvasSequenceTotal(listProp);
                EditorGUI.LabelField(rect, $"{headerLabel} (Total: {total:0.###} s)", EditorStyles.boldLabel);
            };
            list.elementHeightCallback = index =>
            {
                var item = listProp.GetArrayElementAtIndex(index);
                return GetStepElementHeight(item);
            };
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var item = listProp.GetArrayElementAtIndex(index);
                DrawStepElement(rect, item, index, listProp);
            };
            return list;
        }

        private float GetStepElementHeight(SerializedProperty itemProp)
        {
            var animationsProp = itemProp.FindPropertyRelative("animations");
            int animCount = animationsProp?.arraySize ?? 0;
            var useCustomProp = itemProp.FindPropertyRelative("useCustomDuration");
            float h = LineH * 2; // target parent + disable
            h += LineH * 2;     // play mode + useCustom
            if (useCustomProp != null && useCustomProp.boolValue)
                h += LineH + Pad; // customDuration
            h += LineH;         // "Animations" header
            h += Pad;
            h += 2f;            // list header
            h += animCount * (LineH * 2 + Pad);
            
            // Proper footer height - more space needed for empty list
            if (animCount == 0)
                h += 38f;       // Extra space for empty list footer with +/- buttons
            else
                h += 26f;       // Normal footer height
            
            h += LineH + Pad;   // Populate button
            return h + 12f;
        }

        private void DrawStepElement(Rect rect, SerializedProperty itemProp, int stepIndex, SerializedProperty listProp)
        {
            var targetParentProp = itemProp.FindPropertyRelative("targetParent");
            var disableTargetProp = itemProp.FindPropertyRelative("disableTargetParentWhenHidden");
            var playModeProp = itemProp.FindPropertyRelative("playMode");
            var useCustomProp = itemProp.FindPropertyRelative("useCustomDuration");
            var customDurProp = itemProp.FindPropertyRelative("customDuration");
            var animationsProp = itemProp.FindPropertyRelative("animations");

            float y = rect.y + 4f;
            float w = rect.width - 4f;

            EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), targetParentProp, new GUIContent("Target Parent"));
            y += LineH + Pad;

            EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), disableTargetProp, new GUIContent("Disable Target Parent When Hidden"));
            y += LineH + Pad;

            EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), playModeProp);
            y += LineH + Pad;

            EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), useCustomProp);
            if (useCustomProp.boolValue)
            {
                y += LineH + Pad;
                EditorGUI.PropertyField(new Rect(rect.x, y, w, LineH), customDurProp);
            }
            y += LineH + Pad;

            EditorGUI.LabelField(new Rect(rect.x, y, w, LineH), "Animations", EditorStyles.miniBoldLabel);
            y += LineH + Pad;

            string cacheKey = animationsProp.propertyPath;
            if (!_animListCache.TryGetValue(cacheKey, out var animList))
            {
                animList = new ReorderableList(serializedObject, animationsProp, true, true, true, true);
                animList.headerHeight = 2f;
                animList.elementHeight = LineH * 2 + Pad;
                animList.drawElementCallback = (r, j, isActive, isFocused) =>
                {
                    var animProp = animationsProp.GetArrayElementAtIndex(j);
                    var obj = animProp.objectReferenceValue as DOTweenAnimation;
                    string desc = obj != null
                        ? CanvasSequenceEditorHelper.GetAnimationDescription(obj, useCustomProp.boolValue, customDurProp.floatValue)
                        : "[Empty]";
                    EditorGUI.LabelField(new Rect(r.x, r.y, r.width - 28f, LineH), desc, EditorStyles.miniLabel);
                    animProp.objectReferenceValue = EditorGUI.ObjectField(
                        new Rect(r.x, r.y + LineH, r.width - 28f, LineH - 2f),
                        animProp.objectReferenceValue,
                        typeof(DOTweenAnimation),
                        true);
                    if (GUI.Button(new Rect(r.x + r.width - 24f, r.y, 22f, LineH * 2 - 2f), "-", EditorStyles.miniButton))
                    {
                        int removeAt = j;
                        var tgt = serializedObject.targetObject;
                        string listPath = listProp.propertyPath;
                        int stepIdx = stepIndex;
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            var so = new SerializedObject(tgt);
                            var list = so.FindProperty(listPath);
                            if (list != null && stepIdx < list.arraySize)
                            {
                                var step = list.GetArrayElementAtIndex(stepIdx);
                                var anims = step.FindPropertyRelative("animations");
                                if (anims != null && removeAt < anims.arraySize) { anims.DeleteArrayElementAtIndex(removeAt); so.ApplyModifiedProperties(); _animListCache.Clear(); }
                            }
                        };
                    }
                };
                animList.onAddCallback = _ => animationsProp.InsertArrayElementAtIndex(animationsProp.arraySize);
                _animListCache[cacheKey] = animList;
            }
            float listH = animList.GetHeight();
            animList.DoList(new Rect(rect.x, y, w, listH));
            y += listH + Pad;

            if (GUI.Button(new Rect(rect.x, y, w * 0.5f - 2f, LineH), "Populate from Parent", EditorStyles.miniButton))
            {
                CanvasSequenceEditorHelper.PopulateAnimationsFromParent(targetParentProp.objectReferenceValue as Transform, animationsProp);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Game Canvas", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(canvasIdProp, new GUIContent("Canvas ID"));
            EditorGUILayout.PropertyField(showDebugLogProp, new GUIContent("Show Debug Log"));
            EditorGUILayout.PropertyField(disableGameObjectOnCloseProp, new GUIContent("Disable GameObject On Close"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Auto Open", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(openOnLoadingFinishedProp, new GUIContent("Open On Loading Finished"));
            EditorGUILayout.PropertyField(openOnlyInSceneProp, new GUIContent("Open Only In Scene"));
            EditorGUILayout.PropertyField(allowOpenWhenActiveProp, new GUIContent("Allow Open When Active"));

            EditorGUILayout.Space(6);
            openList.DoLayoutList();
            EditorGUILayout.Space(4);
            closeList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
