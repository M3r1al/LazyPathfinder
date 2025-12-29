using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(PlayerInput), typeof(PlayerController))]
public class GemsFinder : MonoBehaviour
{
    // BST для быстрого поиска ресурсов по карте
    private static GemsFinder instance;

    private PlayerController player;
    private PlayerInput playerInput;
    private InputAction findAction;

    private bool playerActive = true;

    private readonly List<Loot> allLoot = new();

    private void Awake()
    {
        instance = this;
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        findAction = playerInput.actions["Find"];
    }

    private void OnEnable()
    {
        findAction.performed += OnFindGem;
    }

    private void OnDisable()
    {
        findAction.performed -= OnFindGem;
    }

    private void Start()
    {
        CollectAllLoot();
    }

    private void OnFindGem(InputAction.CallbackContext ctx)
    {
        if (!playerActive)
            return;
        
        Loot loot = FindNearestLoot(transform.position);
        if (loot != null)
            player.MoveToWorldPoint(loot.position);
        else
            Debug.Log("Is null");
    }

    // Публичный метод — можно вызывать из UI
    public Loot FindNearestLoot(Vector3 from)
    {
        Loot best = null;
        float bestDistance = float.MaxValue;

        foreach (Loot loot in allLoot)
        {
            float d = Vector3.Distance(from, loot.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = loot;
            }
        }

        return best;
    }

    private IEnumerator CollectCoroutine()
    {
        yield return null;

        allLoot.Clear();

        Chunk[] chunks = FindObjectsOfType<Chunk>();
        foreach (Chunk chunk in chunks)
        {
            if (chunk.data == null) continue;
            allLoot.AddRange(chunk.data.resources);
        }
    }

    // Для очистки списка при пересборке чанков
    public static void CollectAllLoot() => instance.StartCoroutine(instance.CollectCoroutine());

    public static void ChangePlayerState(bool state)
    {
        instance.player.enabled = state;
        instance.playerActive = state;
    }
}