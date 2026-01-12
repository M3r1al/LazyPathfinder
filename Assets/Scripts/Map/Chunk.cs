using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class Chunk : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private GameObject lootPrefab;
    [HideInInspector] public ChunkData data;
    [HideInInspector] public Mesh Mesh;
    [HideInInspector] public int VertexCount;

    private void Start() => Init();

    public void Init()
    {
        if (gameObject.TryGetComponent(out MeshCollider collider))
            Destroy(collider);
        
        Mesh mesh = meshFilter.mesh;
        Mesh = mesh;
        Vector3[] vertices = mesh.vertices;
        VertexCount = vertices.Length;
        Color[] colors = new Color[vertices.Length];
        SurfaceType[] surfaceTypes = new SurfaceType[vertices.Length];
        List<Loot> resources = new List<Loot>();

        for (int i = 0; i < vertices.Length; i++)
        {
            ChunkHeight chunkHeight = LazyHeightSequence.GetHeight(i, vertices[i].x * MapStatic.PlaneSize + transform.position.x, vertices[i].z * MapStatic.PlaneSize + transform.position.z);
            vertices[i].y = chunkHeight.height;
            surfaceTypes[i] = chunkHeight.surfaceType;
            colors[i] = MapStatic.SurfaceColor(chunkHeight.surfaceType);
            if (chunkHeight.hasLoot)
            {
                resources.Add(chunkHeight.loot);
                GameObject lt = Instantiate(lootPrefab, chunkHeight.loot.position, Quaternion.identity, transform);
                lt.GetComponent<ResourceController>().Init(this, chunkHeight.loot);
            }
        }

        data = new ChunkData(vertices, surfaceTypes, resources);

        mesh.vertices = vertices;
        mesh.colors = colors;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        gameObject.AddComponent<MeshCollider>();
    }

    public void RemoveResource(Loot loot)
    {
        GemsFinder.RemoveLoot(loot);
        if (data.resources.Contains(loot))
        {
            data.resources.Remove(loot);
            return;
        }
        
        for (int i = 0; i < data.resources.Count; i++)
        {
            if ((data.resources[i].position - loot.position).magnitude <= 0.001f)
            {
                data.resources.RemoveAt(i);
                return;
            }
        }
    }
}

public class LazyHeightSequence
{
    private static float Round(float value) => Mathf.Round(value * MapStatic.RoundCount) / MapStatic.RoundCount;
    private static Dictionary<Vector2, ChunkHeight> materialized = new Dictionary<Vector2, ChunkHeight>();
    
    public static ChunkHeight GetHeight(int i, float worldX, float worldZ)
    {
        Vector2 key = new Vector2(Round(worldX), Round(worldZ));
        if (materialized.ContainsKey(key))
            return materialized[key];
        
        float vertex = Mathf.PerlinNoise(worldX / MapStatic.TerrainFrequency, worldZ / MapStatic.TerrainFrequency) * MapStatic.TerrainAmplitude;
        SurfaceType detail = MapStatic.ConvertToSurfaceType(Mathf.PerlinNoise(worldX / MapStatic.DetailFrequency, worldZ / MapStatic.DetailFrequency) * MapStatic.DetailAmplitude + MapStatic.DetailDelta);

        Loot resource = null;
        Vector3 globalPosition = new Vector3(worldX, vertex * MapStatic.PlaneSize, worldZ);
        foreach (LootData loot in MapStatic.RefLoot)
        {
            if (Random.Range(0, 1001) <= loot.chance)
            {
                resource = new Loot(loot, i, globalPosition);
                break;
            }
        }

        ChunkHeight newChunk = new ChunkHeight(vertex, detail, resource);
        materialized.Add(key, newChunk);
        return newChunk;
    }

    public static ChunkHeight TryGetHeight(float worldX, float worldZ)
    {
        return TryGetHeight(new Vector2(Round(worldX), Round(worldZ)));
    }

    public static ChunkHeight TryGetHeight(Vector2 key)
    {
        if (materialized.ContainsKey(key))
            return materialized[key];
        
        float vertex = Mathf.PerlinNoise(key.x / MapStatic.TerrainFrequency, key.y / MapStatic.TerrainFrequency) * MapStatic.TerrainAmplitude;
        SurfaceType detail = MapStatic.ConvertToSurfaceType(Mathf.PerlinNoise(key.x / MapStatic.DetailFrequency, key.y / MapStatic.DetailFrequency) * MapStatic.DetailAmplitude + MapStatic.DetailDelta);

        ChunkHeight newChunk = new ChunkHeight(vertex, detail, null);
        return newChunk;
    }
}