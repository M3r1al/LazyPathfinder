using System;
using System.Collections.Generic;

public class BinarySearchTree<TKey, TValue>
{
    private class Node
    {
        public TKey Key;
        public TValue Value;
        public Node Left;
        public Node Right;
        public int Height;
        
        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Height = 1;
        }
    }
    
    private Node _root;
    private int _count;
    private readonly IComparer<TKey> _comparer;
    
    public int Count => _count;
    public bool IsEmpty => _root == null;
    
    // Конструктор с компаратором по умолчанию
    public BinarySearchTree() : this(Comparer<TKey>.Default) { }
    
    // Конструктор с пользовательским компаратором
    public BinarySearchTree(IComparer<TKey> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    public void Insert(TKey key, TValue value)
    {
        _root = Insert(_root, key, value);
        _count++;
    }
    
    private Node Insert(Node node, TKey key, TValue value)
    {
        if (node == null)
            return new Node(key, value);
        
        int cmp = _comparer.Compare(key, node.Key);
        
        if (cmp < 0)
            node.Left = Insert(node.Left, key, value);
        else if (cmp > 0)
            node.Right = Insert(node.Right, key, value);
        else
            node.Value = value; // Обновление существующего
        
        // Обновляем высоту
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        
        // Балансировка
        return Balance(node);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        var node = Find(_root, key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        
        value = default;
        return false;
    }

    public bool TryFindNearest(TKey targetKey, out TValue nearestValue)
    {
        if (_root == null)
        {
            nearestValue = default;
            return false;
        }
        
        Node nearest = null;
        double bestDistance = double.MaxValue;
        
        FindNearest(_root, targetKey, ref nearest, ref bestDistance);
        
        if (nearest != null)
        {
            nearestValue = nearest.Value;
            return true;
        }
        
        nearestValue = default;
        return false;
    }

    public bool TryFindNearest(TKey targetKey, out TValue nearestValue, out TKey nearestKey)
    {
        if (_root == null)
        {
            nearestValue = default;
            nearestKey = default;
            return false;
        }
        
        Node nearest = null;
        double bestDistance = double.MaxValue;
        
        FindNearest(_root, targetKey, ref nearest, ref bestDistance);
        
        if (nearest != null)
        {
            nearestValue = nearest.Value;
            nearestKey = nearest.Key;
            return true;
        }
        
        nearestValue = default;
        nearestKey = default;
        return false;
    }
    
    private void FindNearest(Node node, TKey target, ref Node bestNode, ref double bestDistance)
    {
        if (node == null) return;
        
        // Вычисляем расстояние между ключами
        double distance = CalculateDistance(target, node.Key);
        
        // Обновляем лучший найденный узел
        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestNode = node;
        }
        
        int cmp = _comparer.Compare(target, node.Key);
        
        if (cmp < 0)
            FindNearest(node.Left, target, ref bestNode, ref bestDistance);
        else if (cmp > 0)
            FindNearest(node.Right, target, ref bestNode, ref bestDistance);
    }
    
    private double CalculateDistance(TKey a, TKey b)
    {
        // Для числовых типов
        if (a is float aFloat && b is float bFloat)
            return Math.Abs(aFloat - bFloat);
        if (a is int aInt && b is int bInt)
            return Math.Abs(aInt - bInt);
        if (a is double aDouble && b is double bDouble)
            return Math.Abs(aDouble - bDouble);
        
        // Для Vector2Int
        if (a is UnityEngine.Vector2Int aVec2 && b is UnityEngine.Vector2Int bVec2)
            return UnityEngine.Vector2Int.Distance(aVec2, bVec2);
        
        // Для других типов используем сравнение
        int cmp = _comparer.Compare(a, b);
        return Math.Abs(cmp);
    }

    public bool Remove(TKey key)
    {
        int initialCount = _count;
        _root = Remove(_root, key);
        return _count < initialCount;
    }

    private Node Remove(Node node, TKey key)
    {
        if (node == null) return null;
        
        int cmp = _comparer.Compare(key, node.Key);
        
        if (cmp < 0)
            node.Left = Remove(node.Left, key);
        else if (cmp > 0)
            node.Right = Remove(node.Right, key);
        else
        {
            // Найден узел для удаления
            if (node.Left == null || node.Right == null)
            {
                // Один или нет детей
                node = node.Left ?? node.Right;
                _count--;
            }
            else
            {
                // Два ребенка
                var successor = FindMin(node.Right);
                node.Key = successor.Key;
                node.Value = successor.Value;
                node.Right = Remove(node.Right, successor.Key);
            }
        }
        
        if (node == null) return null;
        
        // Обновляем высоту и балансируем
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        return Balance(node);
    }
    
    // Обход в порядке возрастания ключей
    public IEnumerable<KeyValuePair<TKey, TValue>> InOrderTraversal()
    {
        var stack = new Stack<Node>();
        var current = _root;
        
        while (current != null || stack.Count > 0)
        {
            while (current != null)
            {
                stack.Push(current);
                current = current.Left;
            }
            
            current = stack.Pop();
            yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
            
            current = current.Right;
        }
    }
    
    // Обход в ширину (BFS)
    public IEnumerable<KeyValuePair<TKey, TValue>> BreadthFirstTraversal()
    {
        if (_root == null) yield break;
        
        var queue = new Queue<Node>();
        queue.Enqueue(_root);
        
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            yield return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            
            if (node.Left != null) queue.Enqueue(node.Left);
            if (node.Right != null) queue.Enqueue(node.Right);
        }
    }

    public bool TryGetMin(out TValue minValue, out TKey minKey)
    {
        if (_root == null)
        {
            minValue = default;
            minKey = default;
            return false;
        }
        
        var minNode = FindMin(_root);
        minValue = minNode.Value;
        minKey = minNode.Key;
        return true;
    }

    public bool TryGetMax(out TValue maxValue, out TKey maxKey)
    {
        if (_root == null)
        {
            maxValue = default;
            maxKey = default;
            return false;
        }
        
        var maxNode = FindMax(_root);
        maxValue = maxNode.Value;
        maxKey = maxNode.Key;
        return true;
    }

    public void Clear()
    {
        _root = null;
        _count = 0;
    }
    
    // Вспомогательные методы
    private Node Find(Node node, TKey key)
    {
        while (node != null)
        {
            int cmp = _comparer.Compare(key, node.Key);
            if (cmp == 0) return node;
            node = cmp < 0 ? node.Left : node.Right;
        }
        return null;
    }
    
    private Node FindMin(Node node)
    {
        while (node.Left != null) node = node.Left;
        return node;
    }
    
    private Node FindMax(Node node)
    {
        while (node.Right != null) node = node.Right;
        return node;
    }
    
    private int Height(Node node) => node?.Height ?? 0;
    
    private int GetBalance(Node node) => node == null ? 0 : Height(node.Left) - Height(node.Right);
    
    private Node Balance(Node node)
    {
        int balance = GetBalance(node);
        
        // Left Heavy
        if (balance > 1)
        {
            if (GetBalance(node.Left) < 0)
                node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }
        
        // Right Heavy
        if (balance < -1)
        {
            if (GetBalance(node.Right) > 0)
                node.Right = RotateRight(node.Right);
            return RotateLeft(node);
        }
        
        return node;
    }
    
    private Node RotateRight(Node y)
    {
        var x = y.Left;
        var T2 = x.Right;
        
        x.Right = y;
        y.Left = T2;
        
        y.Height = 1 + Math.Max(Height(y.Left), Height(y.Right));
        x.Height = 1 + Math.Max(Height(x.Left), Height(x.Right));
        
        return x;
    }
    
    private Node RotateLeft(Node x)
    {
        var y = x.Right;
        var T2 = y.Left;
        
        y.Left = x;
        x.Right = T2;
        
        x.Height = 1 + Math.Max(Height(x.Left), Height(x.Right));
        y.Height = 1 + Math.Max(Height(y.Left), Height(y.Right));
        
        return y;
    }
}