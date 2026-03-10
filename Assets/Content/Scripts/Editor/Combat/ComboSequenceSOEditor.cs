using UnityEngine;
using UnityEditor;

namespace LittleHeroJourney
{
    [CustomEditor(typeof(ComboSequenceSO))]
    public class ComboSequenceSOEditor : UnityEditor.Editor
    {
        private ComboSequenceSO comboSequence;

        private void OnEnable()
        {
            comboSequence = (ComboSequenceSO)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSequenceInfoSection();
            DrawAttackChainSection();
            DrawAILoopSection(); // Only when sequence name contains "AI"
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSequenceInfoSection()
        {
            EditorGUILayout.LabelField("Sequence Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredWeapon"));
            EditorGUILayout.Space();
        }

        private void DrawAttackChainSection()
        {
            EditorGUILayout.LabelField("Attack Chain", EditorStyles.boldLabel);
            SerializedProperty attackSequence = serializedObject.FindProperty("attackSequence");
            EditorGUILayout.PropertyField(attackSequence, true);
            EditorGUILayout.Space();
        }

        private void DrawAILoopSection()
        {
            string sequenceName = comboSequence.sequenceName;
            bool isAISequence = !string.IsNullOrEmpty(sequenceName) && sequenceName.Contains("AI");

            if (isAISequence)
            {
                EditorGUILayout.LabelField("AI Combo Loop Settings", EditorStyles.boldLabel);

                SerializedProperty allowLoop = serializedObject.FindProperty("allowComboLoop");
                EditorGUILayout.PropertyField(allowLoop, new GUIContent("Allow Combo Loop",
                    "Allow AI to loop combo sequence from last attack back to first? Player always allows loop."));

                SerializedProperty maxLoops = serializedObject.FindProperty("maxComboLoops");
                EditorGUILayout.PropertyField(maxLoops, new GUIContent("Max Combo Loops",
                    "Maximum number of times combo can loop (0 = infinite, only if allowComboLoop = true)"));

                EditorGUILayout.HelpBox("AI Combo Loop Settings visible because sequence name contains 'AI'. Player combo always allows loop.", MessageType.Info);
                EditorGUILayout.Space();
            }
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugInfo"));
            EditorGUILayout.Space();
        }
    }
}
