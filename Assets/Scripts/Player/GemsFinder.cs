using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerInput), typeof(PlayerController))]
public class GemsFinder : MonoBehaviour
{
    // BST для быстрого поиска ресурсов по карте
    private static GemsFinder instance;
    
    private PlayerController player;
    private PlayerInput playerInput;
    private InputAction findAction;
    
    private bool playerActive = true;
    
    // Два BST для быстрого поиска по разным ключам
    private BinarySearchTree<float, Loot> distanceTree;    // По расстоянию до игрока
    private BinarySearchTree<Vector2Int, Loot> coordTree;  // По координатам (для удаления)
    
    // Для связи между ключами
    private Dictionary<Loot, float> lootToDistance = new Dictionary<Loot, float>();
    private Dictionary<Loot, Vector2Int> lootToCoord = new Dictionary<Loot, Vector2Int>();
    
    private Vector3 lastPlayerPosition;
    private float updateTimer = 0f;
    private const float UpdateInteval = 0.5f;

    private void Awake()
    {
        instance = this;
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        findAction = playerInput.actions["Find"];
        
        distanceTree = new BinarySearchTree<float, Loot>();
        
        // Создаем компаратор для Vector2Int
        coordTree = new BinarySearchTree<Vector2Int, Loot>(Comparer<Vector2Int>.Create((a, b) =>
        {
            int xComparison = a.x.CompareTo(b.x);
            if (xComparison != 0)
                return xComparison;
            return a.y.CompareTo(b.y);
        }));
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
        lastPlayerPosition = transform.position;
        StartCoroutine(CollectAllLootCoroutine());
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= UpdateInteval)
        {
            UpdateDistances();
            updateTimer = 0f;
        }
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
        if (distanceTree.IsEmpty)
            return null;
        
        if (distanceTree.TryFindNearest(0f, out Loot nearest, out float distance))
            return nearest;
        
        return null;
    }

    private void AddLootToBST(Loot loot)
    {
        if (loot == null)
            return;
        
        float distance = Vector3.Distance(loot.position, transform.position);
        Vector2Int coordKey = new Vector2Int(Mathf.RoundToInt(loot.position.x * 10), Mathf.RoundToInt(loot.position.z * 10));
        
        distanceTree.Insert(distance, loot);
        coordTree.Insert(coordKey, loot);
        
        // Сохраняем связи
        lootToDistance[loot] = distance;
        lootToCoord[loot] = coordKey;
    }

    // Удаление лута из BST
    public static void RemoveLoot(Loot loot) => instance?.DeleteLoot(loot);

    public void DeleteLoot(Loot loot)
    {
        if (loot == null) return;
        
        if (lootToDistance.TryGetValue(loot, out float distance) &&
            lootToCoord.TryGetValue(loot, out Vector2Int coord))
        {
            // Удаляем из обоих деревьев
            distanceTree.Remove(distance);
            coordTree.Remove(coord);
            
            // Удаляем из словарей
            lootToDistance.Remove(loot);
            lootToCoord.Remove(loot);
        }
    }

    // Обновление расстояний в BST при движении игрока
    private void UpdateDistances()
    {
        if (Vector3.Distance(transform.position, lastPlayerPosition) < 0.1f)
            return;
        
        lastPlayerPosition = transform.position;
        
        if (distanceTree.IsEmpty) return;
        
        // Создаем временное дерево с обновленными расстояниями
        var newDistanceTree = new BinarySearchTree<float, Loot>();
        var newLootToDistance = new Dictionary<Loot, float>();
        
        // Переносим все луты с обновленными расстояниями
        foreach (var kvp in distanceTree.InOrderTraversal())
        {
            Loot loot = kvp.Value;
            float newDistance = Vector3.Distance(loot.position, transform.position);
            
            // Вставляем с новым расстоянием
            newDistanceTree.Insert(newDistance, loot);
            newLootToDistance[loot] = newDistance;
        }
        
        // Заменяем старое дерево новым
        distanceTree = newDistanceTree;
        lootToDistance = newLootToDistance;
    }

    private IEnumerator CollectAllLootCoroutine()
    {
        yield return null;
        
        // Очищаем BST
        distanceTree.Clear();
        coordTree.Clear();
        lootToDistance.Clear();
        lootToCoord.Clear();
        
        Chunk[] chunks = FindObjectsOfType<Chunk>();
        foreach (Chunk chunk in chunks)
        {
            if (chunk.data == null || chunk.data.resources == null) 
                continue;
                
            foreach (Loot loot in chunk.data.resources)
                AddLootToBST(loot);
        }
    }

    public static void CollectAllLoot() => instance?.StartCoroutine(instance.CollectAllLootCoroutine());

    public static void ChangePlayerState(bool state)
    {
        if (instance != null)
        {
            instance.player.enabled = state;
            instance.playerActive = state;
        }
    }
}