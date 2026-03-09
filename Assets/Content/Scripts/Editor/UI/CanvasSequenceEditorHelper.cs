using UnityEditor;
using UnityEngine;
using DG.Tweening;
using LittleHeroJourney.UI;
using System.Reflection;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    /// <summary>Shared editor helpers for GameCanvas and LoadingUIController sequence/animations to avoid duplicate code.</summary>
    public static class CanvasSequenceEditorHelper
    {
        public static string GetFieldOrPropertyString(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            var t = target.GetType();
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var v = f.GetValue(target);
                return v != null ? v.ToString() : null;
            }
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(target, null);
                return v != null ? v.ToString() : null;
            }
            return null;
        }

        /// <summary>Total duration for a flat list of DOTweenAnimation with play mode and optional custom duration.</summary>
        public static float ComputeTotalDurationForAnimations(
            SerializedProperty animationsProp,
            bool isSequential,
            bool useCustomDuration,
            float customDuration)
        {
            if (animationsProp == null) return 0f;
            var list = new List<DOTweenAnimation>(animationsProp.arraySize);
            for (int j = 0; j < animationsProp.arraySize; j++)
            {
                var animObj = animationsProp.GetArrayElementAtIndex(j).objectReferenceValue as DOTweenAnimation;
                if (animObj != null) list.Add(animObj);
            }
            return Helper.GetSequenceTotalDuration(list, isSequential, useCustomDuration, customDuration);
        }

        /// <summary>Duration for one GameCanvas.SequenceItem (step with targetParent, playMode, animations).</summary>
        public static float ComputeGameCanvasStepDuration(SerializedProperty itemProp)
        {
            if (itemProp == null) return 0f;
            var playModeProp = itemProp.FindPropertyRelative("playMode");
            var useCustomDurationProp = itemProp.FindPropertyRelative("useCustomDuration");
            var customDurationProp = itemProp.FindPropertyRelative("customDuration");
            var animationsProp = itemProp.FindPropertyRelative("animations");
            if (animationsProp == null) return 0f;

            bool useCustom = useCustomDurationProp.boolValue;
            float customDur = customDurationProp.floatValue;
            int modeIndex = playModeProp.enumValueIndex;
            bool sequential = (modeIndex == (int)GameCanvas.SequencePlayMode.Sequential);

            return ComputeTotalDurationForAnimations(animationsProp, sequential, useCustom, customDur);
        }

        /// <summary>Total duration for GameCanvas open/close sequence list.</summary>
        public static float ComputeGameCanvasSequenceTotal(SerializedProperty listProp)
        {
            if (listProp == null) return 0f;
            float total = 0f;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                total += ComputeGameCanvasStepDuration(listProp.GetArrayElementAtIndex(i));
            }
            return total;
        }

        /// <summary>One-line description for an animation (type, target, duration, ease) for Inspector.</summary>
        public static string GetAnimationDescription(DOTweenAnimation obj, bool useCustomDuration, float customDuration)
        {
            if (obj == null) return "null";
            string type = GetFieldOrPropertyString(obj, "animationType");
            string ease = GetFieldOrPropertyString(obj, "easeType");
            if (string.IsNullOrEmpty(ease)) ease = GetFieldOrPropertyString(obj, "ease");
            string name = obj.gameObject != null ? obj.gameObject.name : "Unknown";
            string target = obj.target != null ? obj.target.name : "None";
            float duration = Helper.GetTweenEffectiveDuration(obj, useCustomDuration, customDuration);
            return $"{name} [{(string.IsNullOrEmpty(type) ? "?" : type)}] • Target={target} • {duration:0.###}s • Ease={(string.IsNullOrEmpty(ease) ? "Default" : ease)}";
        }

        /// <summary>Description with Self/Other and delay for Splash-style editor.</summary>
        public static string GetAnimationDescriptionRich(DOTweenAnimation obj, bool useCustomDuration, float customDuration, UnityEngine.Object containerObject)
        {
            if (obj == null) return "null";
            string type = GetFieldOrPropertyString(obj, "animationType");
            string name = obj.gameObject != null ? obj.gameObject.name : "Unknown";
            float duration = Helper.GetTweenEffectiveDuration(obj, useCustomDuration, customDuration);
            bool isSelf = (containerObject != null && obj.gameObject != null && containerObject == (UnityEngine.Object)obj.gameObject);
            string selfOther = isSelf ? "(Self)" : "(Other)";
            return $"{name} - {type} {selfOther} - {duration:F2}s";
        }

        /// <summary>Populate animations list from DOTweenAnimation children of a transform.</summary>
        public static void PopulateAnimationsFromParent(Transform parent, SerializedProperty animationsProp)
        {
            if (parent == null || animationsProp == null) return;
            var list = parent.GetComponentsInChildren<DOTweenAnimation>(true);
            animationsProp.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++)
            {
                animationsProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }
    }
}
