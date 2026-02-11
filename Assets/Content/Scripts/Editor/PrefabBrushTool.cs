using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LittleHeroJourney
{
    public class PrefabBrushTool : EditorWindow
{
    private PrefabBrushSettings currentSettings;
    private Vector2 scrollPosition;
    private bool isPainting = false;
    private GameObject previewObject;
    private static PrefabBrushTool instance;
    [SerializeField] private bool showDebugLog = false;

    // Menu item to open the tool
    [MenuItem("Tools/Prefab Brush Tool")]
    public static void ShowWindow()
    {
        instance = GetWindow<PrefabBrushTool>("Prefab Brush");
        instance.minSize = new Vector2(350, 500);
    }

    private void OnEnable()
    {
        instance = this;
        SceneView.duringSceneGui += OnSceneGUI;
        LoadSettings();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void LoadSettings()
    {
        // Try to load existing settings or create new one
        string[] guids = AssetDatabase.FindAssets("t:LittleHeroJourney.PrefabBrushSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            currentSettings = AssetDatabase.LoadAssetAtPath<PrefabBrushSettings>(path);
        }
        else
        {
            // Create default settings
            currentSettings = CreateInstance<PrefabBrushSettings>();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Brush Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Settings file field
        EditorGUI.BeginChangeCheck();
        currentSettings = (PrefabBrushSettings)EditorGUILayout.ObjectField("Settings", currentSettings, typeof(PrefabBrushSettings), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (currentSettings == null)
            {
                currentSettings = CreateInstance<PrefabBrushSettings>();
            }
        }

        EditorGUILayout.Space();

        // Brush Settings
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
        currentSettings.brushSize = EditorGUILayout.Slider("Brush Size", currentSettings.brushSize, 1f, 50f);
        currentSettings.brushDensity = EditorGUILayout.Slider("Density", currentSettings.brushDensity, 0.1f, 5f);

        EditorGUILayout.Space();

        // Scale Settings
        GUILayout.Label("Scale Randomization", EditorStyles.boldLabel);
        EditorGUILayout.MinMaxSlider("Scale Range", ref currentSettings.randomScaleMin, ref currentSettings.randomScaleMax, 0.1f, 3f);
        EditorGUILayout.BeginHorizontal();
        currentSettings.randomScaleMin = EditorGUILayout.FloatField(currentSettings.randomScaleMin);
        currentSettings.randomScaleMax = EditorGUILayout.FloatField(currentSettings.randomScaleMax);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Placement Rules
        GUILayout.Label("Placement Rules", EditorStyles.boldLabel);
        currentSettings.alignToSurface = EditorGUILayout.Toggle("Align to Surface", currentSettings.alignToSurface);
        currentSettings.avoidOverlapping = EditorGUILayout.Toggle("Avoid Overlapping", currentSettings.avoidOverlapping);
        if (currentSettings.avoidOverlapping)
        {
            currentSettings.minDistanceBetweenObjects = EditorGUILayout.Slider("Min Distance", currentSettings.minDistanceBetweenObjects, 0.5f, 5f);
        }

        EditorGUILayout.Space();

        // Randomization
        GUILayout.Label("Randomization", EditorStyles.boldLabel);
        currentSettings.randomizeRotation = EditorGUILayout.Toggle("Random Rotation", currentSettings.randomizeRotation);
        if (currentSettings.randomizeRotation)
        {
            currentSettings.maxRandomRotation = EditorGUILayout.Slider("Max Rotation", currentSettings.maxRandomRotation, 0f, 360f);
        }

        EditorGUILayout.Space();

        // Prefab List
        GUILayout.Label("Prefabs", EditorStyles.boldLabel);

        // Add prefab button
        if (GUILayout.Button("Add Prefab"))
        {
            currentSettings.prefabs.Add(null);
        }

        // Prefab list with scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        for (int i = 0; i < currentSettings.prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            // Prefab field
            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                $"Prefab {i + 1}",
                currentSettings.prefabs[i],
                typeof(GameObject),
                false
            );

            if (newPrefab != currentSettings.prefabs[i])
            {
                currentSettings.prefabs[i] = newPrefab;
                MarkSettingsDirty();
            }

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                currentSettings.prefabs.RemoveAt(i);
                MarkSettingsDirty();
                break; // Exit loop to avoid index issues
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "Hold Ctrl+Left Click in Scene View to paint prefabs.\n" +
            "Hold Shift+Left Click to erase prefab instances within brush radius.\n\n" +
            "Erase targets are instances of prefabs listed in this brush.",
            MessageType.Info
        );

        // Save settings button
        if (GUILayout.Button("Save Settings"))
        {
            SaveSettings();
        }

        // Erase all button
        EditorGUILayout.Space();
        if (GUILayout.Button("Erase All Brush Objects", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Erase All",
                "Are you sure you want to erase ALL objects created by this brush?",
                "Yes", "No"))
            {
                EraseAllBrushObjects();
            }
        }

        // Selected prefab index for selective erase
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selective Erase:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Erase Selected Prefab"))
        {
            EraseSelectedPrefab();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void SaveSettings()
    {
        if (currentSettings == null) return;

        string path = "Assets/Content/Scripts/Runtime/PrefabBrushSettings.asset";
        AssetDatabase.CreateAsset(currentSettings, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDebugLog) Debug.Log("Prefab Brush Settings saved to: " + path);
    }

    private void MarkSettingsDirty()
    {
        if (currentSettings != null)
        {
            EditorUtility.SetDirty(currentSettings);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Always show brush preview if we have settings
        if (currentSettings != null)
        {
            DrawBrushPreview();
        }

        if (currentSettings == null || currentSettings.prefabs.Count == 0)
        {
            // Show warning in scene view
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 300, 50), "No prefabs loaded! Add prefabs to the brush first.");
            Handles.EndGUI();
            return;
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;

        // Handle mouse events - more flexible detection
        bool controlPressed = e.control || Event.current.control;
        bool shiftPressed = e.shift || Event.current.shift;

        // Handle mouse events
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (controlPressed)
            {
                if (showDebugLog) Debug.Log("Paint mode activated!");
                // Paint mode
                isPainting = true;
                e.Use();
                PaintAtMousePosition();
            }
            else if (shiftPressed)
            {
                if (showDebugLog) Debug.Log("Erase mode activated!");
                // Erase mode
                isPainting = true;
                e.Use();
                EraseAtMousePosition();
            }
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isPainting = false;
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isPainting)
        {
            if (controlPressed)
            {
                PaintAtMousePosition();
            }
            else if (shiftPressed)
            {
                EraseAtMousePosition();
            }
        }

        // Show status
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 300, 50),
            $"Brush Ready! Ctrl+Click: Paint | Shift+Click: Erase\nPrefabs: {currentSettings.prefabs.Count}");
        Handles.EndGUI();
    }

    private void PaintAtMousePosition()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        // First try to find a surface
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, currentSettings.placementLayers))
        {
            if (showDebugLog) Debug.Log($"Surface found at {hit.point}, placing prefabs...");
            Vector3 center = hit.point;

            // Calculate number of objects based on density and brush size
            int objectCount = Mathf.CeilToInt(currentSettings.brushDensity * currentSettings.brushSize);
            if (showDebugLog) Debug.Log($"Calculated object count: {objectCount} (density: {currentSettings.brushDensity}, size: {currentSettings.brushSize})");

            int placedCount = 0;
            for (int i = 0; i < objectCount; i++)
            {
                // Get random position within brush radius
                Vector2 randomCircle = Random.insideUnitCircle * (currentSettings.brushSize * 0.5f);
                Vector3 randomPosition = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Raycast down to find ground
                if (Physics.Raycast(randomPosition + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 20f, currentSettings.placementLayers))
                {
                    // Check if position is valid (not too close to existing objects)
                    if (IsValidPlacementPosition(groundHit.point))
                    {
                        PlacePrefabAt(groundHit);
                        placedCount++;
                    }
                    else
                    {
                        if (showDebugLog) Debug.Log($"Position {groundHit.point} invalid (too close to existing objects)");
                    }
                }
                else
                {
                    if (showDebugLog) Debug.Log($"No ground found at random position: {randomPosition}");
                }
            }
            if (showDebugLog) Debug.Log($"Placed {placedCount} out of {objectCount} objects");

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            // Fallback: place on ground plane if no surface found
            if (showDebugLog) Debug.Log("No surface found, placing on ground plane...");

            // Find intersection with ground plane (Y = 0)
            Plane groundPlane = new Plane(Vector3.up, 0f);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 center = ray.GetPoint(distance);

                // Calculate number of objects based on density and brush size
                int objectCount = Mathf.CeilToInt(currentSettings.brushDensity * currentSettings.brushSize);

                for (int i = 0; i < objectCount; i++)
                {
                    // Get random position within brush radius
                    Vector2 randomCircle = Random.insideUnitCircle * (currentSettings.brushSize * 0.5f);
                    Vector3 randomPosition = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                    // Create fake raycast hit for ground placement
                    RaycastHit groundHit = new RaycastHit();
                    groundHit.point = randomPosition;
                    groundHit.normal = Vector3.up;

                    if (IsValidPlacementPosition(randomPosition))
                    {
                        PlacePrefabAt(groundHit);
                    }
                }

                // Mark scene as dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            else
            {
                if (showDebugLog) Debug.LogWarning("Could not place prefabs - no surface or ground plane found!");
            }
        }
    }

    private void EraseAtMousePosition()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, currentSettings.placementLayers))
        {
            if (showDebugLog) Debug.Log($"Erase mode activated at {hit.point}");

            // Find all objects within brush radius and remove matching prefab instances
            Collider[] colliders = Physics.OverlapSphere(hit.point, currentSettings.brushSize * 0.5f);
            int erasedCount = 0;

            foreach (Collider collider in colliders)
            {
                GameObject target = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;
                if (IsInstanceOfBrushPrefabs(target))
                {
                    if (showDebugLog) Debug.Log($"Erasing object: {target.name}");
                    Undo.DestroyObjectImmediate(target);
                    erasedCount++;
                }
            }

            if (showDebugLog) Debug.Log($"Erased {erasedCount} brush objects");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            if (showDebugLog) Debug.Log("No surface found for erasing");
        }
    }

    private bool IsValidPlacementPosition(Vector3 position)
    {
        if (!currentSettings.avoidOverlapping)
        {
            if (showDebugLog) Debug.Log($"Position {position} valid (overlapping disabled)");
            return true;
        }

        // Check distance to nearby brush instances only (ignore environment colliders)
        Collider[] nearbyColliders = Physics.OverlapSphere(position, currentSettings.minDistanceBetweenObjects);
        if (showDebugLog) Debug.Log($"Found {nearbyColliders.Length} nearby colliders at position {position} (radius: {currentSettings.minDistanceBetweenObjects})");

        foreach (Collider collider in nearbyColliders)
        {
            if (collider == null) continue;
            if (collider.isTrigger) continue;
            // Filter by placement layers
            if ((currentSettings.placementLayers.value & (1 << collider.gameObject.layer)) == 0) continue;

            GameObject target = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;
            if (IsInstanceOfBrushPrefabs(target))
            {
                if (showDebugLog) Debug.Log($"Position {position} invalid - too close to placed brush object {target.name}");
                return false;
            }
        }

        if (showDebugLog) Debug.Log($"Position {position} valid");
        return true;
    }

    private void PlacePrefabAt(RaycastHit hit)
    {
        if (showDebugLog) Debug.Log($"PlacePrefabAt called at position: {hit.point}");

        GameObject prefab = currentSettings.GetRandomPrefab();
        if (prefab == null)
        {
            if (showDebugLog) Debug.LogWarning("GetRandomPrefab returned null! Check if prefabs are assigned.");
            return;
        }

        if (showDebugLog) Debug.Log($"Using prefab: {prefab.name}");

        // Create the object
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (newObject == null)
        {
            if (showDebugLog) Debug.LogWarning("InstantiatePrefab returned null! Prefab might be corrupted.");
            return;
        }

        if (showDebugLog) Debug.Log($"Instantiated object: {newObject.name}");

        // Set position
        Vector3 position = hit.point;
        if (currentSettings.alignToSurface)
        {
            position += hit.normal * 0.01f; // Slight offset to avoid z-fighting
        }
        newObject.transform.position = position;
        if (showDebugLog) Debug.Log($"Set position to: {position}");

        // Set rotation
        if (currentSettings.alignToSurface)
        {
            newObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * currentSettings.GetRandomRotation();
        }
        else
        {
            newObject.transform.rotation = currentSettings.GetRandomRotation();
        }

        // Set scale
        float scale = currentSettings.GetRandomScale();
        newObject.transform.localScale = Vector3.one * scale;
        if (showDebugLog) Debug.Log($"Set scale to: {scale}");

        // Register for undo
        Undo.RegisterCreatedObjectUndo(newObject, "Paint Prefab");

        if (showDebugLog) Debug.Log($"Successfully placed prefab: {newObject.name} at {position}");
    }

    private void EraseAllBrushObjects()
    {
        int erasedCount = 0;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (IsInstanceOfBrushPrefabs(obj))
            {
                Undo.DestroyObjectImmediate(obj);
                erasedCount++;
            }
        }

        if (showDebugLog) Debug.Log($"Erased all {erasedCount} brush objects");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void EraseSelectedPrefab()
    {
        if (currentSettings.prefabs.Count == 0)
        {
            if (showDebugLog) Debug.LogWarning("No prefabs in the brush to erase");
            return;
        }

        // For now, erase objects from the first prefab (you can enhance this to select which prefab)
        GameObject selectedPrefab = currentSettings.prefabs[0];
        if (selectedPrefab == null)
        {
            if (showDebugLog) Debug.LogWarning("Selected prefab is null");
            return;
        }

        int erasedCount = 0;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (IsInstanceOfSpecificPrefab(obj, selectedPrefab))
            {
                Undo.DestroyObjectImmediate(obj);
                erasedCount++;
            }
        }

        if (showDebugLog) Debug.Log($"Erased {erasedCount} objects from selected prefab");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private bool IsInstanceOfBrushPrefabs(GameObject obj)
    {
        if (obj == null || currentSettings == null) return false;
        var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj) ?? obj;
        var source = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
        var sourceGO = source as GameObject;
        if (sourceGO != null && currentSettings.prefabs.Contains(sourceGO)) return true;
        return false;
    }

    private bool IsInstanceOfSpecificPrefab(GameObject obj, GameObject prefab)
    {
        if (obj == null || prefab == null) return false;
        var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj) ?? obj;
        var source = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
        if (source == prefab) return true;
        return false;
    }

    private void DrawBrushPreview()
    {
        if (currentSettings == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        // Try to find surface first
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, currentSettings.placementLayers))
        {
            // Draw brush circle
            Handles.color = new Color(0f, 1f, 0f, 0.3f);
            Handles.DrawWireDisc(hit.point, hit.normal, currentSettings.brushSize * 0.5f);

            // Draw brush center
            Handles.color = Color.green;
            Handles.DrawSolidDisc(hit.point, hit.normal, 0.1f);
        }
        else
        {
            // Fallback: show preview on ground plane
            Plane groundPlane = new Plane(Vector3.up, 0f);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 groundPoint = ray.GetPoint(distance);

                // Draw brush circle on ground
                Handles.color = new Color(1f, 1f, 0f, 0.3f); // Yellow for ground placement
                Handles.DrawWireDisc(groundPoint, Vector3.up, currentSettings.brushSize * 0.5f);

                // Draw brush center
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(groundPoint, Vector3.up, 0.1f);
            }
        }
    }

    private void CleanupPreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
    }
}
}
