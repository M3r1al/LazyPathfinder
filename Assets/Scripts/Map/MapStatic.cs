using System.Collections.Generic;
using UnityEngine;

public class ChunkData
{
    // public Vector2Int coordinates;
    public Vector3[] heights;
    public SurfaceType [] surfaceTypes;
    public List<Loot> resources;

    public ChunkData(Vector3[] heights, SurfaceType [] surfaceTypes, List<Loot> resources)
    {
        // this.coordinates = coordinates;
        this.heights = heights;
        this.surfaceTypes = surfaceTypes;
        this.resources = resources;
    }
}


public class ChunkHeight
{
    public float height;
    public SurfaceType surfaceType;
    public bool hasLoot => loot != null;
    public Loot loot;
    public ChunkHeight(float height, SurfaceType surfaceType, Loot loot)
    {
        this.height = height;
        this.surfaceType = surfaceType;
        this.loot = loot;
    }
}

[System.Serializable]
public class Loot
{
    public LootData lootData;
    public Vector3 position;
    [HideInInspector] public int vertexIndex;
    public Loot(LootData lootData, int index, Vector3 position)
    {
        this.lootData = lootData;
        this.vertexIndex = index;
        this.position = position;
    }
}

public static class MapStatic
{
    public static SurfaceType ConvertToSurfaceType(float value)
    {
        // float gap = 1.0f / (Surfaces.Length - 1);
        // swamp: 0-0.2
        // sand: 0.1-0.3
        // ground: 0.3-0.7
        // stone: 0.7-1
        if (value <= 0.2)
            return SurfaceType.Swamp;
        if (value <= 0.3)
            return SurfaceType.Sand;
        if (value <= 0.7)
            return SurfaceType.Ground;
        return SurfaceType.Stone;
        // return (SurfaceType)(value / gap);
    }

    public static float TerrainFrequency = 3;
    public static float TerrainAmplitude = 1;
    public static float DetailFrequency = 10;
    public static float DetailAmplitude = 1;
    public static float DetailDelta = 0;
    // public static uint ChunkSize = 5;
    public static LootData[] RefLoot;
    public static SurfaceData[] Surfaces;
    public static float PlaneSize;
    public const int RoundCount = 10;
    // Ground, Sand, Swamp, Stone
    // public static readonly float[] SurfacesViscosity = {0.1f, 0.5f, 1f, 0.0f};
    // public static readonly float[] SurfacesSmoothness = {0.4f, 0.2f, 0.3f, 0.9f};

    public static float CalculateSurfaceDifficulty(SurfaceData surface)
    {
        return surface.viscosity / PlayerStatic.ViscosityResistanceLevel + surface.smoothness / PlayerStatic.SmoothnessResistanceLevel;
    }

    public static Color SurfaceColor(SurfaceType surface)
    {
        foreach (SurfaceData surfaceData in Surfaces)
            if (surfaceData.surfaceType == surface)
                return surfaceData.surfaceColor;
        return Color.white;
    }
}