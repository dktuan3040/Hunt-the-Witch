using System.Collections.Generic;
using UnityEngine;

// Local A* navigation against solid scenery. Uses the enemy's collider bounds,
// so wide enemies cannot select gaps that only a smaller enemy could traverse.
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyNavigation2D : MonoBehaviour
{
    [SerializeField, Min(.2f)] private float cellSize = .5f;
    [SerializeField, Min(.1f)] private float repathInterval = .6f;
    [SerializeField, Min(1f)] private float detourMargin = 10f;
    [SerializeField, Min(100)] private int maxSearchNodes = 4000;
    private Collider2D bodyCollider;
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly List<Vector2> route = new List<Vector2>();
    private readonly List<Node> heap = new List<Node>();
    private readonly Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    private ContactFilter2D filter;
    private float nextRepath;
    private Vector2 lastDestination;
    private int waypoint;
    private Vector2 origin;
    private Vector2 size;

    private sealed class Node
    {
        public Vector2Int Cell;
        public Node Parent;
        public float Cost, Score;
        public bool Closed;
    }

    private void Awake()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            if (!col.isTrigger) { bodyCollider = col; break; }
        filter = new ContactFilter2D();
        filter.useTriggers = false;
    }

    private Vector2 Center => bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
    private Vector2 Size => bodyCollider != null ? (Vector2)bodyCollider.bounds.size + Vector2.one * .08f : Vector2.one * .6f;

    private bool IsObstacle(Collider2D col)
    {
        if (col == null || col.isTrigger || !col.enabled || col.transform.IsChildOf(transform)) return false;
        if (col.GetComponentInParent<Health>() != null) return false;
        return col.gameObject.layer == LayerMask.NameToLayer("Wall")
            || col.attachedRigidbody == null || col.attachedRigidbody.bodyType == RigidbodyType2D.Static;
    }

    private bool SegmentClear(Vector2 from, Vector2 to, Vector2 footprint)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < .00001f) return true;
        castHits.Clear();
        Physics2D.BoxCast(from, footprint, 0, delta.normalized, filter, castHits, delta.magnitude);
        foreach (var hit in castHits) if (IsObstacle(hit.collider)) return false;
        return true;
    }

    public bool HasLineOfSight(Vector2 destination)
    {
        return SegmentClear(Center, destination, Vector2.one * .03f);
    }

    public Vector2 GetChaseDirection(Vector2 destination)
    {
        Vector2 from = Center;
        size = Size;
        if (SegmentClear(from, destination, size))
        {
            route.Clear();
            return (destination - from).normalized;
        }

        if (Time.time >= nextRepath || (destination - lastDestination).sqrMagnitude > 2.25f)
        {
            nextRepath = Time.time + repathInterval;
            lastDestination = destination;
            BuildRoute(from, destination);
        }
        while (waypoint < route.Count && Vector2.Distance(from, route[waypoint]) < .03f) waypoint++;
        if (waypoint >= route.Count) return Vector2.zero;

        // Skip intermediate nodes only when the entire body fits through the shortcut.
        while (waypoint + 1 < route.Count && SegmentClear(from, route[waypoint + 1], size)) waypoint++;
        if (!SegmentClear(from, route[waypoint], size))
        {
            nextRepath = 0;
            return Vector2.zero;
        }
        return Vector2.ClampMagnitude((route[waypoint] - from) / .2f, 1f);
    }

    public Vector2 GetRetreatDirection(Vector2 away)
    {
        size = Size;
        float[] angles = { 0, 30, -30, 60, -60, 90, -90 };
        foreach (float angle in angles)
        {
            Vector2 direction = Quaternion.Euler(0, 0, angle) * away.normalized;
            if (SegmentClear(Center, Center + direction * .8f, size)) return direction;
        }
        return Vector2.zero;
    }

    private Vector2 Position(Vector2Int cell) => origin + (Vector2)cell * cellSize;

    private bool Fits(Vector2 point)
    {
        overlaps.Clear();
        Physics2D.OverlapBox(point, size, 0, filter, overlaps);
        foreach (var col in overlaps) if (IsObstacle(col)) return false;
        return true;
    }

    private void BuildRoute(Vector2 start, Vector2 goal)
    {
        route.Clear(); waypoint = 0; nodes.Clear(); heap.Clear(); origin = start;
        Vector2Int goalCell = Vector2Int.RoundToInt((goal - start) / cellSize);
        int margin = Mathf.CeilToInt(detourMargin / cellSize);
        int minX = Mathf.Min(0, goalCell.x) - margin, maxX = Mathf.Max(0, goalCell.x) + margin;
        int minY = Mathf.Min(0, goalCell.y) - margin, maxY = Mathf.Max(0, goalCell.y) + margin;
        var first = new Node { Cell = Vector2Int.zero, Cost = 0, Score = Vector2.Distance(start, goal) };
        nodes.Add(first.Cell, first); Push(first);
        Node found = null;
        int expanded = 0;
        while (heap.Count > 0 && expanded++ < maxSearchNodes)
        {
            Node current = Pop();
            if (current.Closed || nodes[current.Cell] != current) continue;
            current.Closed = true;
            Vector2 point = Position(current.Cell);
            if (Vector2.Distance(point, goal) <= cellSize * 1.5f && SegmentClear(point, goal, size))
            { found = current; break; }
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector2Int cell = current.Cell + new Vector2Int(x, y);
                if (cell.x < minX || cell.x > maxX || cell.y < minY || cell.y > maxY) continue;
                Node existing;
                if (nodes.TryGetValue(cell, out existing) && existing.Closed) continue;
                Vector2 next = Position(cell);
                float cost = current.Cost + Vector2.Distance(point, next);
                if (existing != null && cost >= existing.Cost) continue;
                if (!Fits(next) || !SegmentClear(point, next, size)) continue;
                // Insert a new entry rather than mutate a heap key in place.
                var node = new Node { Cell = cell, Parent = current, Cost = cost, Score = cost + Vector2.Distance(next, goal) };
                nodes[cell] = node;
                Push(node);
            }
        }
        if (found == null) return; // A closed gate or unreachable room must remain impassable.
        route.Add(goal);
        for (Node node = found; node.Parent != null; node = node.Parent) route.Add(Position(node.Cell));
        route.Reverse();
    }

    private void Push(Node node)
    {
        int index = heap.Count; heap.Add(node);
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].Score <= node.Score) break;
            heap[index] = heap[parent]; index = parent;
        }
        heap[index] = node;
    }

    private Node Pop()
    {
        Node result = heap[0], last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count == 0) return result;
        int index = 0;
        while (index * 2 + 1 < heap.Count)
        {
            int child = index * 2 + 1;
            if (child + 1 < heap.Count && heap[child + 1].Score < heap[child].Score) child++;
            if (last.Score <= heap[child].Score) break;
            heap[index] = heap[child]; index = child;
        }
        heap[index] = last;
        return result;
    }

    private void OnDisable() { route.Clear(); nextRepath = 0; }
}
