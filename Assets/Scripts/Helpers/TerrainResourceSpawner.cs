using UnityEngine;

public class TerrainResourceSpawner : MonoBehaviour
{
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

    [ContextMenu("Spawn Resources")]
    public void SpawnResources()
    {
        Terrain[] terrains = GetComponentsInChildren<Terrain>();

        Transform treesParent = GetOrCreateParent("Trees");
        Transform metalsParent = GetOrCreateParent("Metals");

        ClearChildren(treesParent);
        ClearChildren(metalsParent);

        foreach (Terrain terrain in terrains)
        {
            SpawnOnTerrain(
                terrain,
                _treePrefab,
                _treeCount,
                treesParent);

            SpawnOnTerrain(
                terrain,
                _metalPrefab,
                _metalCount,
                metalsParent);
        }
    }

    private Transform GetOrCreateParent(string objectName)
    {
        GameObject parent = GameObject.Find(objectName);

        if (parent == null)
        {
            parent = new GameObject(objectName);
        }

        return parent.transform;
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

    private void SpawnOnTerrain(
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

#if UNITY_EDITOR
            GameObject instance;

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
            GameObject instance = Instantiate(prefab, parent);
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
