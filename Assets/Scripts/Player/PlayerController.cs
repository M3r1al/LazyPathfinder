using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    // Через поиск кратчайшего пути в графе (представляем мир как граф)
    // ребра разного веса в зависимости от типа местности
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gridStep = 1f;
    [SerializeField] private int maxIterations = 3000;
    [SerializeField] private float rotationSmoothSpeed = 10f;
    [SerializeField] private float normalSmoothSpeed = 5f;
    

    private Camera cam;
    private PlayerInput playerInput;
    private InputAction clickAction;

    private List<Vector3> path;
    private int pathIndex;
    private Vector3 targetNormal = Vector3.up;
    private Vector3 currentNormal = Vector3.up;

    #region Unity

    private void Awake()
    {
        cam = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        clickAction = playerInput.actions["Attack"];
    }

    private void OnEnable()
    {
        clickAction.performed += OnMoveClick;
    }

    private void OnDisable()
    {
        clickAction.performed -= OnMoveClick;
    }

    private void Update()
    {
        MoveAlongPath();
        UpdateNormal();
    }

    #endregion

    #region Input

    private void OnMoveClick(InputAction.CallbackContext ctx)
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        MoveToWorldPoint(hit.point);
    }

    #endregion

    #region Public API (for GemsFinder)

    // для других систем (GemsFinder)
    public void MoveToWorldPoint(Vector3 worldPoint)
    {
        Vector3 start = Snap(transform.position);
        Vector3 goal = Snap(worldPoint);

        path = FindPath(start, goal);
        pathIndex = 0;
    }

    #endregion

    #region Movement

    private SurfaceData GetSurfaceUnder(Vector3 worldPos)
    {
        ChunkHeight h = LazyHeightSequence.TryGetHeight(worldPos.x, worldPos.z);
        if (h == null)
            return null;

        foreach (SurfaceData s in MapStatic.Surfaces)
            if (s.surfaceType == h.surfaceType)
                return s;

        return null;
    }

    private float GetCurrentSpeed(Vector3 worldPos)
    {
        SurfaceData surface = GetSurfaceUnder(worldPos);

        if (surface == null)
            return moveSpeed;

        float speedModifier = PlayerStatic.SpeedDebuff(surface);

        return moveSpeed + speedModifier;
    }

    private void MoveAlongPath()
    {
        if (path == null || pathIndex >= path.Count)
            return;

        Vector3 target = ApplyHeight(path[pathIndex]);
        Vector3 current = ApplyHeight(transform.position);

        // Вычисляем направление движения
        Vector3 moveDirection = target - current;
        if (moveDirection.magnitude > 0.1f)
        {
            // Сохраняем только горизонтальное направление
            Vector3 horizontalDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
            if (horizontalDirection.magnitude > 0.01f)
            {
                // Поворачиваем игрока по направлению движения с плавной интерполяцией
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection.normalized, currentNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSmoothSpeed * Time.deltaTime);
            }
        }

        transform.position = Vector3.MoveTowards(
            current,
            target,
            (moveSpeed + PlayerStatic.SpeedLevel) * Time.deltaTime
        );

        // Обновляем целевую нормаль для плавного изменения
        UpdateTargetNormal(target);

        if (Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(target.x, target.z)
            ) < 0.1f)
        {
            pathIndex++;
        }
    }

    private void UpdateTargetNormal(Vector3 position)
    {
        // Получаем нормаль поверхности в текущей позиции
        Vector3 normal = GetSurfaceNormal(position);
        if (normal != Vector3.zero)
        {
            targetNormal = normal;
        }
    }

    private void UpdateNormal()
    {
        // Плавно интерполируем текущую нормаль к целевой
        currentNormal = Vector3.Slerp(currentNormal, targetNormal, 
            normalSmoothSpeed * Time.deltaTime);
        
        // Если игрок движется, корректируем поворот с учетом новой нормали
        if (path != null && pathIndex < path.Count)
        {
            Vector3 target = ApplyHeight(path[pathIndex]);
            Vector3 current = ApplyHeight(transform.position);
            Vector3 moveDirection = (target - current);
            
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 horizontalDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
                if (horizontalDirection.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection.normalized, currentNormal);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                        rotationSmoothSpeed * Time.deltaTime);
                }
            }
        }
    }

    private Vector3 GetSurfaceNormal(Vector3 worldPos)
    {
        // Используем центральные разности для вычисления нормали поверхности
        float delta = 0.1f;
        
        // Получаем высоты в соседних точках
        ChunkHeight center = LazyHeightSequence.TryGetHeight(worldPos.x, worldPos.z);
        ChunkHeight right = LazyHeightSequence.TryGetHeight(worldPos.x + delta, worldPos.z);
        ChunkHeight forward = LazyHeightSequence.TryGetHeight(worldPos.x, worldPos.z + delta);
        
        if (center == null || right == null || forward == null)
            return Vector3.up;
        
        // Вычисляем векторы тангенсов
        Vector3 rightVec = new Vector3(delta, (right.height - center.height) * MapStatic.PlaneSize, 0);
        Vector3 forwardVec = new Vector3(0, (forward.height - center.height) * MapStatic.PlaneSize, delta);
        
        // Вычисляем нормаль через векторное произведение
        Vector3 normal = Vector3.Cross(forwardVec, rightVec).normalized;
        
        return normal != Vector3.zero ? normal : Vector3.up;
    }

    #endregion

    #region Pathfinding (2D A*)

    private List<Vector3> FindPath(Vector3 start, Vector3 goal)
    {
        List<Node> open = new();
        HashSet<Vector2Int> closed = new();

        open.Add(new Node(start, null, 0, Heuristic(start, goal)));

        int iterations = 0;

        while (open.Count > 0)
        {
            iterations++;
            if (iterations > maxIterations)
                return null;

            open.Sort((a, b) => a.F.CompareTo(b.F));
            Node current = open[0];
            open.RemoveAt(0);

            Vector2Int key = ToGrid(current.position);
            if (closed.Contains(key))
                continue;

            if (Vector2.Distance(
                    new Vector2(current.position.x, current.position.z),
                    new Vector2(goal.x, goal.z)
                ) < gridStep)
            {
                return ReconstructPath(current);
            }

            closed.Add(key);

            foreach (Vector3 dir in Directions)
            {
                Vector3 nextPos = Snap(current.position + dir * gridStep);
                Vector2Int nextKey = ToGrid(nextPos);

                if (closed.Contains(nextKey))
                    continue;

                float cost = GetSurfaceCost(nextPos);

                open.Add(new Node(
                    nextPos,
                    current,
                    current.G + cost,
                    Heuristic(nextPos, goal)
                ));
            }
        }

        return null;
    }

    private float Heuristic(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }

    private List<Vector3> ReconstructPath(Node node)
    {
        List<Vector3> result = new();
        while (node != null)
        {
            result.Add(node.position);
            node = node.parent;
        }
        result.Reverse();
        return result;
    }

    #endregion

    #region Height & Surface

    private Vector3 ApplyHeight(Vector3 pos)
    {
        ChunkHeight h = LazyHeightSequence.TryGetHeight(
            pos.x,
            pos.z
        );

        pos.y = h.height * MapStatic.PlaneSize;
        return pos;
    }

    private float GetSurfaceCost(Vector3 pos)
    {
        ChunkHeight h = LazyHeightSequence.TryGetHeight(
            pos.x,
            pos.z
        );

        foreach (SurfaceData s in MapStatic.Surfaces)
            if (s.surfaceType == h.surfaceType)
                return MapStatic.CalculateSurfaceDifficulty(s);

        return 1f;
    }

    #endregion

    #region Helpers

    private Vector3 Snap(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / gridStep) * gridStep,
            0,
            Mathf.Round(pos.z / gridStep) * gridStep
        );
    }

    private Vector2Int ToGrid(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(pos.x / gridStep),
            Mathf.RoundToInt(pos.z / gridStep)
        );
    }

    private static readonly Vector3[] Directions =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    #endregion

    #region Internal node

    private class Node
    {
        public Vector3 position;
        public Node parent;
        public float G;
        public float H;
        public float F => G + H;

        public Node(Vector3 pos, Node parent, float g, float h)
        {
            position = pos;
            this.parent = parent;
            G = g;
            H = h;
        }
    }

    #endregion
}