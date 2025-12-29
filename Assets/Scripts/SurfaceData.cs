using UnityEngine;

[CreateAssetMenu(fileName = "New surface data", menuName = "Surfaces/Create new surface data")]
public class SurfaceData : ScriptableObject
{
    public int id;
    public float viscosity;
    public float smoothness;
    public new string name;
    public SurfaceType surfaceType;
    public Color surfaceColor;
}

public enum SurfaceType
{
    Ground = 0,
    Sand = 1,
    Swamp = 2,
    Stone = 3
}