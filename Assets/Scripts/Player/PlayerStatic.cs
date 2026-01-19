using UnityEngine;

public class PlayerStatic
{
    public static int ViscosityResistanceLevel = 1;
    public static int SmoothnessResistanceLevel = 1;
    public static int LootMultiplierLevel = 1;
    public static int SpeedLevel = 1;
    public static int EngineLevel = 1;

    public static int Money;

    public static float SpeedDebuff(SurfaceData surface)
    {
        return SpeedLevel - (surface.viscosity / ViscosityResistanceLevel + surface.smoothness / SmoothnessResistanceLevel);
    }
}
