using System.Collections;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private float terrainFrequency = 3;
    [SerializeField] private float terrainAmplitude = 1;
    [SerializeField] private float detailFrequency = 10;
    [SerializeField] private float detailAmplitude = 1;
    [SerializeField] private float detailDelta = 0;
    //[SerializeField] private uint chunkSize = 5;
    [SerializeField] private LootData[] refLoot;
    [SerializeField] private SurfaceData[] surfaces;

    [SerializeField] private Transform player;
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private float planeSize;
    [SerializeField] private int halfTilesX;
    [SerializeField] private int halfTilesZ;

    private Vector3 startPos;
    private Hashtable tiles;
    private const int PlaneScale = 10;

    private void Awake()
    {
        MapStatic.TerrainFrequency = terrainFrequency;
        MapStatic.TerrainAmplitude = terrainAmplitude;
        MapStatic.DetailFrequency = detailFrequency;
        MapStatic.DetailAmplitude = detailAmplitude;
        MapStatic.DetailDelta = detailDelta;
        MapStatic.RefLoot = refLoot;
        MapStatic.Surfaces = surfaces;
        MapStatic.PlaneSize = planeSize / PlaneScale;
    }

    private void Start()
    {
        SetupTiles();
        PlacePlayerOnSurface();
    }

    private void Update()
    {
        UpdateTiles();
    }

    private void SetupTiles()
    {
        tiles = new Hashtable();
        player.transform.position = Vector3.zero;
        startPos = Vector3.zero;
        float time = Time.realtimeSinceStartup;
        for (int x = -halfTilesX; x < halfTilesX; x++)
        {
            for (int z = -halfTilesZ; z < halfTilesZ; z++)
            {
                Vector3 pos = new Vector3(x * planeSize + startPos.x, 0, z * planeSize + startPos.z);
                GameObject go = Instantiate(planePrefab, pos, Quaternion.identity);
                go.transform.localScale = Vector3.one * MapStatic.PlaneSize;
                string key = "Tile_" + ((int)pos.x).ToString() + "_" + ((int)pos.z).ToString();
                go.name = key;
                Tile newTile = new Tile(go, time);
                tiles.Add(key, newTile);
            }
        }

        GemsFinder.CollectAllLoot();
    }

    private void PlacePlayerOnSurface()
    {
        Vector3 pos = player.position;

        ChunkHeight h = LazyHeightSequence.TryGetHeight(pos.x, pos.z);

        pos.y = h.height * MapStatic.PlaneSize;
        player.position = pos;
    }

    private void UpdateTiles()
    {
        int xMove = (int)(player.position.x - startPos.x);
        int zMove = (int)(player.position.z - startPos.z);

        if (Mathf.Abs(xMove) <= planeSize && Mathf.Abs(zMove) <= planeSize)
            return;

        int playerX = (int)(Mathf.Floor(player.transform.position.x / planeSize) * planeSize);
        int playerZ = (int)(Mathf.Floor(player.transform.position.z / planeSize) * planeSize);
        float time = Time.realtimeSinceStartup;

        for (int x = -halfTilesX; x < halfTilesX; x++)
        {
            for (int z = -halfTilesZ; z < halfTilesZ; z++)
            {
                Vector3 pos = new Vector3(x * planeSize + playerX, 0, z * planeSize + playerZ);
                string key = "Tile_" + ((int)pos.x).ToString() + "_" + ((int)pos.z).ToString();
                if (tiles.ContainsKey(key))
                {
                    (tiles[key] as Tile).creationTime = time;
                    continue;
                }
                GameObject go = Instantiate(planePrefab, pos, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * MapStatic.PlaneSize;
                go.name = key;
                Tile newTile = new Tile(go, time);
                tiles.Add(key, newTile);
            }
        }

        Hashtable newTerrain = new Hashtable();
        foreach (Tile tile in tiles.Values)
        {
            if (tile.creationTime != time)
                Destroy(tile.tile);
            else
                newTerrain.Add(tile.tile.name, tile);
        }

        tiles = newTerrain;
        startPos = player.transform.position;

        GemsFinder.CollectAllLoot();
    }
}

public class Tile
{
    public GameObject tile;
    public float creationTime;
    public Tile(GameObject tile, float creationTime)
    {
        this.tile = tile;
        this.creationTime = creationTime;
    }
}