using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using LittleHeroJourney;

namespace LittleHeroJourneyEditors
{
    [CustomEditor(typeof(CanvasFlowConfigSO))]
    public class CanvasFlowConfigSOEditor : Editor
    {
        private SerializedProperty _rulesProp;

        private void OnEnable()
        {
            _rulesProp = serializedObject.FindProperty("transitionRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_rulesProp, new GUIContent("Transitions"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
