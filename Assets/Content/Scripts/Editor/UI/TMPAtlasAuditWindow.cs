using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleHeroJourney.Editor
{
    public class TMPAtlasAuditWindow : EditorWindow
    {
        private readonly List<AuditEntry> _entries = new List<AuditEntry>();
        private Vector2 _scroll;
        private bool _scanOpenScenes = true;
        private bool _scanPrefabs = true;
        private bool _includeInactive = true;
        private bool _runForceMeshValidation = false;
        private string _pathFilter = "Assets/Content";

        private static readonly Regex SpriteTagRegex = new Regex("<sprite", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private class AuditEntry
        {
            public string severity;
            public string message;
            public string assetPath;
            public string hierarchyPath;
            public TMP_Text sceneText;
        }

        [MenuItem("Little Hero Journey/Debug - TMP Atlas Audit")]
        public static void Open()
        {
            var w = GetWindow<TMPAtlasAuditWindow>("TMP Atlas Audit");
            w.minSize = new Vector2(620f, 360f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TMP Atlas Audit", EditorStyles.boldLabel);
            _scanOpenScenes = EditorGUILayout.Toggle("Scan Open Scenes", _scanOpenScenes);
            _scanPrefabs = EditorGUILayout.Toggle("Scan Prefabs", _scanPrefabs);
            _includeInactive = EditorGUILayout.Toggle("Include Inactive", _includeInactive);
            _runForceMeshValidation = EditorGUILayout.Toggle("ForceMesh Validation", _runForceMeshValidation);
            _pathFilter = EditorGUILayout.TextField("Path Filter", _pathFilter);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Audit", GUILayout.Height(28f)))
                RunAudit();
            if (GUILayout.Button("Auto Fix Open Scenes", GUILayout.Height(28f)))
                AutoFixOpenScenes();
            if (GUILayout.Button("Auto Fix Prefabs", GUILayout.Height(28f)))
                AutoFixPrefabs();
            if (GUILayout.Button("Clear", GUILayout.Height(28f)))
                _entries.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Findings: " + _entries.Count, EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("[" + e.severity + "] " + e.message, EditorStyles.wordWrappedLabel);
                if (!string.IsNullOrEmpty(e.assetPath))
                    EditorGUILayout.LabelField("Asset", e.assetPath);
                if (!string.IsNullOrEmpty(e.hierarchyPath))
                    EditorGUILayout.LabelField("Hierarchy", e.hierarchyPath);

                EditorGUILayout.BeginHorizontal();
                if (e.sceneText != null && GUILayout.Button("Ping Scene Object"))
                {
                    Selection.activeObject = e.sceneText.gameObject;
                    EditorGUIUtility.PingObject(e.sceneText.gameObject);
                }
                if (!string.IsNullOrEmpty(e.assetPath) && GUILayout.Button("Ping Asset"))
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(e.assetPath);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunAudit()
        {
            _entries.Clear();
            if (_scanOpenScenes) ScanOpenScenes();
            if (_scanPrefabs) ScanPrefabs();
            if (_entries.Count == 0)
                _entries.Add(new AuditEntry { severity = "OK", message = "No obvious TMP atlas/fallback issue found by static audit." });
        }

        private void AutoFixOpenScenes()
        {
            int fixedCount = 0;
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int s = 0; s < sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                bool sceneChanged = false;
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    var texts = roots[r].GetComponentsInChildren<TMP_Text>(_includeInactive);
                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (TryAutoFix(texts[i]))
                        {
                            fixedCount++;
                            sceneChanged = true;
                        }
                    }
                }
                if (sceneChanged)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
            AssetDatabase.SaveAssets();
            RunAudit();
            _entries.Insert(0, new AuditEntry { severity = "INFO", message = "Auto Fix Open Scenes applied to " + fixedCount + " TMP object(s)." });
            if (fixedCount == 0) AppendNoChangeHint();
        }

        private void AutoFixPrefabs()
        {
            int fixedCount = 0;
            string[] searchIn = string.IsNullOrEmpty(_pathFilter) ? null : new[] { _pathFilter };
            var guids = AssetDatabase.FindAssets("t:Prefab", searchIn);
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;
                bool prefabChanged = false;
                try
                {
                    var texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int t = 0; t < texts.Length; t++)
                    {
                        if (TryAutoFix(texts[t]))
                        {
                            fixedCount++;
                            prefabChanged = true;
                        }
                    }
                    if (prefabChanged)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            AssetDatabase.SaveAssets();
            RunAudit();
            _entries.Insert(0, new AuditEntry { severity = "INFO", message = "Auto Fix Prefabs applied to " + fixedCount + " TMP object(s)." });
            if (fixedCount == 0) AppendNoChangeHint();
        }

        private void ScanOpenScenes()
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int s = 0; s < sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    var texts = roots[r].GetComponentsInChildren<TMP_Text>(_includeInactive);
                    for (int i = 0; i < texts.Length; i++)
                        Evaluate(texts[i], scene.path, GetTransformPath(texts[i].transform), texts[i]);
                }
            }
        }

        private void ScanPrefabs()
        {
            string[] searchIn = string.IsNullOrEmpty(_pathFilter) ? null : new[] { _pathFilter };
            var guids = AssetDatabase.FindAssets("t:Prefab", searchIn);
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;
                try
                {
                    var texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int t = 0; t < texts.Length; t++)
                        Evaluate(texts[t], path, GetTransformPath(texts[t].transform), null);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private void Evaluate(TMP_Text text, string assetPath, string hierarchyPath, TMP_Text sceneRef)
        {
            if (text == null) return;

            if (text.font == null)
                Add("ERROR", "Font Asset is missing.", assetPath, hierarchyPath, sceneRef);

            if (text.fontSharedMaterial == null)
                Add("ERROR", "Font Material is missing.", assetPath, hierarchyPath, sceneRef);

            if (text.font != null && (text.font.fallbackFontAssetTable == null || text.font.fallbackFontAssetTable.Count == 0))
                Add("WARN", "No fallback fonts configured.", assetPath, hierarchyPath, sceneRef);

            if (!string.IsNullOrEmpty(text.text) && SpriteTagRegex.IsMatch(text.text) && text.spriteAsset == null)
                Add("ERROR", "Text contains <sprite> tag but Sprite Asset is null.", assetPath, hierarchyPath, sceneRef);

            if (text.font != null && text.font.atlasTextures != null)
            {
                for (int i = 0; i < text.font.atlasTextures.Length; i++)
                    if (text.font.atlasTextures[i] == null)
                        Add("ERROR", "Font atlas texture index " + i + " is null.", assetPath, hierarchyPath, sceneRef);
            }

            if (_runForceMeshValidation)
            {
                try
                {
                    text.ForceMeshUpdate(true, true);
                    var info = text.textInfo;
                    if (info != null && info.materialCount > 0 && info.meshInfo != null && info.meshInfo.Length < info.materialCount)
                        Add("WARN", "MeshInfo length is smaller than materialCount.", assetPath, hierarchyPath, sceneRef);
                }
                catch (Exception ex)
                {
                    Add("ERROR", "ForceMeshUpdate exception: " + ex.Message, assetPath, hierarchyPath, sceneRef);
                }
            }
        }

        private bool TryAutoFix(TMP_Text text)
        {
            if (text == null) return false;
            bool changed = false;

            if (text.font == null && TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
                changed = true;
            }

            if (text.fontSharedMaterial == null && text.font != null && text.font.material != null)
            {
                text.fontSharedMaterial = text.font.material;
                changed = true;
            }

            if (!string.IsNullOrEmpty(text.text) && SpriteTagRegex.IsMatch(text.text) && text.spriteAsset == null && TMP_Settings.defaultSpriteAsset != null)
            {
                text.spriteAsset = TMP_Settings.defaultSpriteAsset;
                changed = true;
            }

            if (text.font != null && (text.font.fallbackFontAssetTable == null || text.font.fallbackFontAssetTable.Count == 0))
            {
                var fallback = ResolveFallbackFontAsset(text.font);
                if (fallback != null)
                {
                    if (text.font.fallbackFontAssetTable == null)
                        text.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (!text.font.fallbackFontAssetTable.Contains(fallback))
                    {
                        text.font.fallbackFontAssetTable.Add(fallback);
                        EditorUtility.SetDirty(text.font);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(text);
                text.ForceMeshUpdate(true, true);
            }
            return changed;
        }

        private TMP_FontAsset ResolveFallbackFontAsset(TMP_FontAsset avoid)
        {
            if (TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset != avoid)
                return TMP_Settings.defaultFontAsset;
            var globalFallbacks = TMP_Settings.fallbackFontAssets;
            if (globalFallbacks != null)
            {
                for (int i = 0; i < globalFallbacks.Count; i++)
                {
                    if (globalFallbacks[i] != null && globalFallbacks[i] != avoid)
                        return globalFallbacks[i];
                }
            }
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset", string.IsNullOrEmpty(_pathFilter) ? null : new[] { _pathFilter });
            for (int i = 0; i < guids.Length; i++)
            {
                var p = AssetDatabase.GUIDToAssetPath(guids[i]);
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
                if (fa != null && fa != avoid)
                    return fa;
            }
            return null;
        }

        private void AppendNoChangeHint()
        {
            int meshInfoWarn = 0;
            int fallbackWarn = 0;
            int atlasNullErr = 0;
            int other = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                var msg = _entries[i].message ?? string.Empty;
                if (msg.Contains("MeshInfo length is smaller than materialCount")) meshInfoWarn++;
                else if (msg.Contains("No fallback fonts configured")) fallbackWarn++;
                else if (msg.Contains("Font atlas texture index")) atlasNullErr++;
                else other++;
            }
            _entries.Insert(1, new AuditEntry
            {
                severity = "INFO",
                message = "No object changed because current findings are mostly non-auto-fixable. MeshInfoWarn=" + meshInfoWarn + ", NoFallbackWarn=" + fallbackWarn + ", AtlasNullErr=" + atlasNullErr + ", Other=" + other
            });
        }

        private void Add(string severity, string message, string assetPath, string hierarchyPath, TMP_Text sceneRef)
        {
            _entries.Add(new AuditEntry
            {
                severity = severity,
                message = message,
                assetPath = assetPath,
                hierarchyPath = hierarchyPath,
                sceneText = sceneRef
            });
        }

        private static string GetTransformPath(Transform tr)
        {
            if (tr == null) return string.Empty;
            var stack = new Stack<string>();
            var cur = tr;
            while (cur != null)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }
            return string.Join("/", stack.ToArray());
        }
    }
}
