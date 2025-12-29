using UnityEngine;

[CreateAssetMenu(fileName = "New loot data", menuName = "Loot/Create loot data")]
public class LootData : ScriptableObject
{
    public uint id;
    public new string name;
    [Range(0, 100)] public int chance;
    public int cost;
    public Color color = Color.white;
}