using UnityEditor;
using UnityEngine;

namespace LittleHeroJourney.EditorTools
{
    [CustomEditor(typeof(AudioSetSO))]
    public class AudioSetSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            SerializedProperty effects = serializedObject.FindProperty("audioEffects");
            if (effects == null)
            {
                EditorGUILayout.HelpBox("audioEffects property not found.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(effects, new GUIContent("Audio Effects"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
