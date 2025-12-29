using UnityEngine;

public class ResourceController : MonoBehaviour
{
    public Loot loot;
    [SerializeField] private new MeshRenderer renderer;
    private bool isDestroyed = false;
    private Chunk parent;

    public void Init(Chunk parent, Loot lootData)
    {
        isDestroyed = false;
        this.parent = parent;
        loot = lootData;
        if (renderer == null)
            renderer = gameObject.GetComponentInChildren<MeshRenderer>();
        renderer.material.color = loot.lootData.color;
    }

    public void OnTriggerEnter(Collider other)
    {
        // Debug.Log(other.name);
        if (isDestroyed)
            return;
        isDestroyed = true;
        PlayerStatic.Money += loot.lootData.cost * PlayerStatic.LootMultiplierLevel;
        parent.RemoveResource(loot);
        Destroy(gameObject);
    }
}
