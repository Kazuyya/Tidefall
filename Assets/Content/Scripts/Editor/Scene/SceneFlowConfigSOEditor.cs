using UnityEditor;
using UnityEngine;
using LittleHeroJourney;
using UnityEditorInternal;

namespace LittleHeroJourneyEditors
{
    [CustomEditor(typeof(SceneFlowConfigSO))]
    public class SceneFlowConfigSOEditor : UnityEditor.Editor
    {
        private static readonly string[] RequiredLabels = new[] { "SplashScreen", "Loading", "MainMenu" };
        private ReorderableList _scenesList;
        private SerializedProperty _scenesProp;

        private void OnEnable()
        {
            _scenesProp = serializedObject.FindProperty("scenes");
            EnsureRequiredScenes(_scenesProp);
            SetupScenesList();
        }

        public override void OnInspectorGUI()
        {
            var cfg = (SceneFlowConfigSO)target;
            serializedObject.Update();

            DrawHeader("Flow Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useSplash"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("splashId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainMenuId"));

            EditorGUILayout.Space(8);
            _scenesList.DoLayoutList();


            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitions"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupScenesList()
        {
            _scenesList = new ReorderableList(serializedObject, _scenesProp, true, true, true, true);
            _scenesList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Scenes");
            };
            _scenesList.elementHeight = EditorGUIUtility.singleLineHeight * 4 + 12;
            _scenesList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var entryProp = _scenesProp.GetArrayElementAtIndex(index);
                var kindProp = entryProp.FindPropertyRelative("kind");
                var idProp = entryProp.FindPropertyRelative("id");
                var pathProp = entryProp.FindPropertyRelative("scenePath");

                bool locked = index < 3;
                if (locked)
                {
                    // Force kind in order
                    SceneFlowConfigSO.SceneKind forcedKind = SceneFlowConfigSO.SceneKind.Splash;
                    if (index == 1) forcedKind = SceneFlowConfigSO.SceneKind.Loading;
                    else if (index == 2) forcedKind = SceneFlowConfigSO.SceneKind.MainMenu;
                    kindProp.enumValueIndex = (int)forcedKind;
                }

                var line = rect;
                line.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginDisabledGroup(locked);
                EditorGUI.PropertyField(line, kindProp, new GUIContent("Kind"));
                EditorGUI.EndDisabledGroup();

                line.y += EditorGUIUtility.singleLineHeight + 4;
                idProp.stringValue = EditorGUI.TextField(line, "Id", idProp.stringValue);

                line.y += EditorGUIUtility.singleLineHeight + 4;
                EditorGUI.PropertyField(line, pathProp, new GUIContent("Scene Path"));

                // no extra helpbox, keep UI clean
            };

            _scenesList.onAddCallback = list =>
            {
                int idx = _scenesProp.arraySize;
                _scenesProp.InsertArrayElementAtIndex(idx);
                var entryProp = _scenesProp.GetArrayElementAtIndex(idx);
                entryProp.FindPropertyRelative("kind").enumValueIndex = (int)SceneFlowConfigSO.SceneKind.Gameplay;
                entryProp.FindPropertyRelative("id").stringValue = "NewScene";
                entryProp.FindPropertyRelative("scenePath").stringValue = string.Empty;
            };

            _scenesList.onCanRemoveCallback = list =>
            {
                return list.index >= 3;
            };

            _scenesList.onRemoveCallback = list =>
            {
                if (list.index >= 3)
                {
                    _scenesProp.DeleteArrayElementAtIndex(list.index);
                }
            };

            _scenesList.onReorderCallbackWithDetails = (list, from, to) =>
            {
                // Prevent moving into/out of the first 3 locked entries
                if (from < 3 || to < 3)
                {
                    // revert
                    _scenesProp.MoveArrayElement(to, from);
                }
            };
        }

        private void EnsureRequiredScenes(SerializedProperty scenesProp)
        {
            // Ensure at least 3 entries
            if (scenesProp.arraySize < 3)
            {
                while (scenesProp.arraySize < 3)
                    scenesProp.InsertArrayElementAtIndex(scenesProp.arraySize);
            }

            // Initialize labels if empty
            for (int i = 0; i < 3; i++)
            {
                SerializedProperty entryProp = scenesProp.GetArrayElementAtIndex(i);
                // force correct kind ordering
                var kindProp = entryProp.FindPropertyRelative("kind");
                kindProp.enumValueIndex = i == 0
                    ? (int)SceneFlowConfigSO.SceneKind.Splash
                    : i == 1
                        ? (int)SceneFlowConfigSO.SceneKind.Loading
                        : (int)SceneFlowConfigSO.SceneKind.MainMenu;
                SerializedProperty idProp = entryProp.FindPropertyRelative("id");
                if (string.IsNullOrEmpty(idProp.stringValue))
                {
                    idProp.stringValue = RequiredLabels[i];
                }
            }
        }

        private void DrawHeader(string title)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
        }
    }
}
