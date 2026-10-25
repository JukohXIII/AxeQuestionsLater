using System.Collections.Generic;
using UnityEngine;

public class PlatformNode
{
    public int index;
    public Collider2D collider;
    public float minX, maxX, topY;
    public bool oneWay;
}

public class PlatformGraph : MonoBehaviour
{
    public static PlatformGraph Instance { get; private set; }

    [Tooltip("Walkable surfaces: same layers as the enemies' Ground Layer.")]
    [SerializeField] private LayerMask platformLayers;
    [Tooltip("Colliders narrower than this are ignored.")]
    [SerializeField] private float minPlatformWidth = 1.5f;
    [SerializeField] private bool drawGizmos = true;

    public IReadOnlyList<PlatformNode> Nodes => nodes;

    private readonly List<PlatformNode> nodes = new();
    private readonly Dictionary<Collider2D, PlatformNode> byCollider = new ();

    void Awake()
    {
        Instance = this;
        Rebuild();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [ContextMenu("Rebuild (preview in editor)")]
    public void Rebuild()
    {
        nodes.Clear();
        byCollider.Clear();

        foreach (var col in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if(!col.enabled || col.isTrigger) continue;
            if(((1 << col.gameObject.layer) & platformLayers) == 0) continue;

            Bounds bounds = col.bounds;
            if (bounds.size.x < minPlatformWidth || bounds.size.y > bounds.size.x) continue;

            var effector = col.GetComponent<PlatformEffector2D>();
            var node = new PlatformNode
            {
                index = nodes.Count,
                collider = col,
                minX = bounds.min.x,
                maxX = bounds.max.x,
                topY = bounds.max.y,
                oneWay = effector != null && col.usedByEffector && effector.useOneWay
            };
            nodes.Add(node);
            byCollider[col] = node;
        }
    }

    public PlatformNode NodeOf(Collider2D col) => 
        col != null && byCollider.TryGetValue(col, out var node) ? node : null;

    public PlatformNode NodeBelow(Vector2 position, float maxDistance = 50f)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, maxDistance, platformLayers);
        return hit.collider != null ? NodeOf(hit.collider) : null;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        foreach (var n in nodes)
        {
            Gizmos.color = n.oneWay ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.5f, 0.2f);
            Gizmos.DrawLine(new Vector3(n.minX, n.topY, 0f), new Vector3(n.maxX, n.topY, 0f));
        }       
    }
}