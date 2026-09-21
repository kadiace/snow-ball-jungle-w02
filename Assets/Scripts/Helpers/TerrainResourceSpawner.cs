
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainResourceSpawner : MonoBehaviour
{
    [Header("Terrains")]
    [SerializeField] private List<Terrain> _terrains = new();

    [Header("Prefabs")]
    [SerializeField] private GameObject _treePrefab;
    [SerializeField] private GameObject _metalPrefab;

    [Header("Spawn Count Per Terrain")]
    [SerializeField] private int _treeCount = 10;
    [SerializeField] private int _metalCount = 5;

    [Header("Height")]
    [SerializeField] private float _heightOffset = 0.1f;

    [Header("Random")]
    [SerializeField] private bool _randomRotation = true;

    private const string TreesName = "Trees";
    private const string MetalsName = "Metals";

    private void Start()
    {

        StartCoroutine(RefillResourcesRoutine());
    }

    private IEnumerator RefillResourcesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            RefillResources();
        }
    }

    [ContextMenu("Spawn Resources")]
    public void SpawnResources()
    {
        foreach (Terrain terrain in _terrains)
        {
            if (terrain == null)
                continue;

            SpawnOnTerrain(terrain);
        }
    }
    private void RefillResources()
    {
        foreach (Terrain terrain in _terrains)
        {
            if (terrain == null)
                continue;

            RefillResource(
                terrain,
                TreesName,
                _treePrefab,
                _treeCount);

            RefillResource(
                terrain,
                MetalsName,
                _metalPrefab,
                _metalCount);
        }
    }

    private void RefillResource(
        Terrain terrain,
        string parentName,
        GameObject prefab,
        int targetCount)
    {
        Transform parent = GetOrCreateParent(
            terrain,
            parentName);

        int currentCount = parent.childCount;

        int spawnCount = targetCount - currentCount;

        if (spawnCount <= 0)
            return;

        SpawnResourcesOfType(
            terrain,
            prefab,
            1,
            parent);
    }

    private void SpawnOnTerrain(Terrain terrain)
    {
        Transform treesParent = GetOrCreateParent(
            terrain,
            TreesName);

        Transform metalsParent = GetOrCreateParent(terrain, MetalsName);

        ClearChildren(treesParent);
        ClearChildren(metalsParent);

        SpawnResourcesOfType(terrain, _treePrefab, _treeCount, treesParent);

        SpawnResourcesOfType(terrain, _metalPrefab, _metalCount, metalsParent);
    }

    private Transform GetOrCreateParent(
        Terrain terrain,
        string objectName)
    {
        Transform parent = terrain.transform.Find(objectName);

        if (parent == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(terrain.transform, false);

            parent = go.transform;
        }

        return parent;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private void SpawnResourcesOfType(
        Terrain terrain,
        GameObject prefab,
        int count,
        Transform parent)
    {
        if (prefab == null)
            return;

        TerrainData terrainData = terrain.terrainData;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, terrainSize.x);
            float z = Random.Range(0f, terrainSize.z);

            float normalizedX = x / terrainSize.x;
            float normalizedZ = z / terrainSize.z;

            float y = terrainData.GetInterpolatedHeight(
                normalizedX,
                normalizedZ);

            Vector3 spawnPosition =
                terrainPosition + new Vector3(x, y, z);

            spawnPosition.y += _heightOffset;

            GameObject instance;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)UnityEditor.PrefabUtility
                    .InstantiatePrefab(prefab, parent);
            }
            else
            {
                instance = Instantiate(prefab, parent);
            }
#else
            instance = Instantiate(prefab, parent);
#endif

            instance.transform.position = spawnPosition;
            instance.name = prefab.name;

            if (_randomRotation)
            {
                instance.transform.rotation = Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f);
            }
        }
    }
}
