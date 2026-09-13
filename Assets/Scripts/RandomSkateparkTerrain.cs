using System;
using UnityEngine;

/// <summary>Attach to a Terrain to create a smooth, randomized bowl park.</summary>
[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
[DisallowMultipleComponent]
public sealed class RandomSkateparkTerrain : MonoBehaviour
{
    [Header("Random layout")]
    public int seed = 12345;
    [Tooltip("Optional: generate a different layout each time Play starts.")]
    public bool randomizeOnPlay = false;

    [Header("Shape (metres, relative to Terrain position Y)")]
    [Range(8f, 40f)] public float bowlDepth = 24f;
    [Range(40f, 75f)] public float bowlRadius = 58f;
    [Range(0f, 1f)] public float layoutVariation = 0.7f;
    [Tooltip("Keep the same value on adjacent tiles for matching boundary heights.")]
    [Range(0f, 10f)] public float floorHeight = 0f;

    [Header("Terrain")]
    [Tooltip("Vertical heightmap range, not the height of the park's ridges.")]
    [Min(60f)] public float terrainHeight = 600f;

    [SerializeField, HideInInspector] private bool hasGenerated;
    [NonSerialized] private TerrainData runtimeData;
    [NonSerialized] private TerrainData runtimeSource;
    private const int Resolution = 513;
    private const float Size = 300f;

    // Unity calls Reset when this component is first added in the editor.
    private void Reset()
    {
        GenerateNewLayout();
    }

    private void Start()
    {
        if (randomizeOnPlay || !hasGenerated) GenerateNewLayout();
    }

    [ContextMenu("Generate New Random Layout")]
    public void GenerateNewLayout()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Randomize Skatepark Seed");
#endif
        seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        GenerateFromSeed();
    }

    [ContextMenu("Rebuild Using Current Seed")]
    public void GenerateFromSeed()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainCollider collider = GetComponent<TerrainCollider>();
        if (terrain == null || collider == null || terrain.terrainData == null)
        {
            Debug.LogError("A Terrain with TerrainData and TerrainCollider is required.", this);
            return;
        }

        float depth = Mathf.Clamp(bowlDepth, 8f, 40f);
        float floor = Mathf.Clamp(floorHeight, 0f, 10f);
        float verticalRange = Mathf.Max(60f, terrainHeight);
        var field = new SkateparkHeightField(seed, depth,
            Mathf.Clamp(bowlRadius, 40f, 75f), Mathf.Clamp01(layoutVariation), floor);
        float[,] heights = new float[Resolution, Resolution];
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
            heights[z, x] = (float)field.Sample(
                x * Size / (Resolution - 1), z * Size / (Resolution - 1)) / verticalRange;

        // Fresh data avoids overwriting assets shared by other Terrain objects.
        // Transfer only paint/layers; old trees, detail objects and holes are not copied.
        TerrainData source = terrain.terrainData;
        TerrainData data = new TerrainData();
        data.name = "Skatepark_" + seed;
        data.heightmapResolution = Resolution;
        data.size = new Vector3(Size, verticalRange, Size);
        data.terrainLayers = source.terrainLayers;
        if (source.alphamapLayers > 0)
        {
            data.alphamapResolution = source.alphamapResolution;
            data.SetAlphamaps(0, 0, source.GetAlphamaps(
                0, 0, source.alphamapWidth, source.alphamapHeight));
        }
        data.SetHeights(0, 0, heights);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/GeneratedSkateparks"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "GeneratedSkateparks");
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/GeneratedSkateparks/" + data.name + ".asset");
            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, terrain, collider },
                "Generate Skatepark");
        }
#endif

        TerrainData previousRuntimeData = runtimeData;
        if (Application.isPlaying)
        {
            if (runtimeData == null) runtimeSource = source;
            runtimeData = data;
        }
        terrain.terrainData = data;
        collider.terrainData = data;
        terrain.enabled = true;
        collider.enabled = true;
        hasGenerated = true;
        terrain.Flush();
        if (Application.isPlaying && previousRuntimeData != null) Destroy(previousRuntimeData);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(terrain);
            UnityEditor.EditorUtility.SetDirty(collider);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        Debug.Log($"Skatepark generated. Seed: {seed}. Size: 300 x 300. " +
            $"Local height range: {floor} to {floor + depth}.", this);
    }

    private void OnDestroy()
    {
        if (runtimeData == null) return;
        Terrain terrain = GetComponent<Terrain>();
        TerrainCollider collider = GetComponent<TerrainCollider>();
        if (terrain != null && terrain.terrainData == runtimeData) terrain.terrainData = runtimeSource;
        if (collider != null && collider.terrainData == runtimeData) collider.terrainData = runtimeSource;
        Destroy(runtimeData);
    }
}

// Pure height calculation, independent of Unity rendering and physics.
public sealed class SkateparkHeightField
{
    private struct Bowl
    {
        public double x, z, radiusX, radiusZ, cos, sin;
    }

    private readonly Bowl[] bowls = new Bowl[9];
    private readonly double depth, floor;

    public SkateparkHeightField(int seed, double depth, double radius, double variation, double floor)
    {
        this.depth = depth;
        this.floor = floor;
        var random = new System.Random(seed);
        for (int z = 0; z < 3; z++)
        for (int x = 0; x < 3; x++)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            bowls[z * 3 + x] = new Bowl
            {
                x = 50 + x * 100 + Signed(random) * 17 * variation,
                z = 50 + z * 100 + Signed(random) * 17 * variation,
                radiusX = radius * (1 + Signed(random) * 0.22 * variation),
                radiusZ = radius * (1 + Signed(random) * 0.22 * variation),
                cos = Math.Cos(angle), sin = Math.Sin(angle)
            };
        }
    }

    public double Sample(double x, double z)
    {
        double level = 1;
        foreach (Bowl bowl in bowls)
        {
            double dx = x - bowl.x, dz = z - bowl.z;
            double u = (dx * bowl.cos + dz * bowl.sin) / bowl.radiusX;
            double v = (-dx * bowl.sin + dz * bowl.cos) / bowl.radiusZ;
            double r2 = u * u + v * v;
            if (r2 >= 1) continue;
            // Smooth bowl floor -> rising wall -> convex, rounded lip.
            // Product blends overlapping bowls without hard min/max seams.
            level *= Smooth(Math.Sqrt(r2));
        }

        // A flat, constant-height boundary also permits matching adjacent tiles.
        // This is a gentle perimeter bank, not an invisible containment wall.
        double edgeDistance = Math.Min(Math.Min(x, 300 - x), Math.Min(z, 300 - z));
        double interior = Smooth(Math.Max(0, Math.Min(1, edgeDistance / 25)));
        level = 1 + (level - 1) * interior;
        return floor + depth * level;
    }

    private static double Smooth(double t) { return t * t * t * (t * (t * 6 - 15) + 10); }
    private static double Signed(System.Random random) { return random.NextDouble() * 2 - 1; }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(RandomSkateparkTerrain))]
[UnityEditor.CanEditMultipleObjects]
public sealed class RandomSkateparkTerrainEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.HelpBox(
            "300 x 300 smooth bowl park. Height values are relative to Terrain Y. " +
            "Each editor generation saves a separate TerrainData asset; original data is preserved.",
            UnityEditor.MessageType.Info);
        if (GUILayout.Button("Generate New Random Layout"))
            foreach (UnityEngine.Object item in targets)
                ((RandomSkateparkTerrain)item).GenerateNewLayout();
        if (GUILayout.Button("Rebuild Using Current Seed"))
            foreach (UnityEngine.Object item in targets)
                ((RandomSkateparkTerrain)item).GenerateFromSeed();
    }
}
#endif
