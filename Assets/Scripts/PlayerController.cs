
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
    [SerializeField] private SurfaceData nullData;

    private Camera cam;
    private PlayerInput playerInput;
    private InputAction clickAction;

    private List<Vector3> path;
    private int pathIndex;

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
        {
            surface = nullData;
            Debug.Log("Surface is not found");
        }

        float speedModifier = PlayerStatic.SpeedDebuff(surface);

        return moveSpeed + speedModifier;
    }

    private void MoveAlongPath()
    {
        if (path == null || pathIndex >= path.Count)
            return;

        Vector3 target = ApplyHeight(path[pathIndex]);
        Vector3 current = ApplyHeight(transform.position);

        transform.position = Vector3.MoveTowards(
            current,
            target,
            (moveSpeed + PlayerStatic.SpeedLevel) * Time.deltaTime
        );

        if (Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(target.x, target.z)
            ) < 0.1f)
        {
            pathIndex++;
        }
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