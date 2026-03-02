using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public class PrefabBrushSettings : ScriptableObject
{
    [Header("Brush Settings")]
    [Range(1f, 50f)]
    public float brushSize = 5f;

    [Range(0.1f, 5f)]
    public float brushDensity = 1f;

    [Range(0f, 1f)]
    public float randomScaleMin = 0.8f;

    [Range(0f, 1f)]
    public float randomScaleMax = 1.2f;

    [Header("Placement Rules")]
    public bool alignToSurface = true;
    public bool avoidOverlapping = false; // Disabled by default for easier testing
    [Range(0.5f, 5f)]
    public float minDistanceBetweenObjects = 1f;
    public LayerMask placementLayers = -1; // All layers by default

    [Header("Randomization")]
    public bool randomizeRotation = true;
    [Range(0f, 360f)]
    public float maxRandomRotation = 360f;

    [Header("Prefabs")]
    public List<GameObject> prefabs = new List<GameObject>();

    // Get a random prefab from the list
    public GameObject GetRandomPrefab()
    {
        if (prefabs.Count == 0) return null;
        return prefabs[Random.Range(0, prefabs.Count)];
    }

    // Get random scale within the specified range
    public float GetRandomScale()
    {
        return Random.Range(randomScaleMin, randomScaleMax);
    }

    // Get random rotation
    public Quaternion GetRandomRotation()
    {
        if (!randomizeRotation) return Quaternion.identity;

        float randomY = Random.Range(0f, maxRandomRotation);
        return Quaternion.Euler(0f, randomY, 0f);
    }
}
}
