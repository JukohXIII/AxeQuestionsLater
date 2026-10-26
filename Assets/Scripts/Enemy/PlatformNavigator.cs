using System.Collections.Generic;
using UnityEngine;

public class PlatformEdge
{
    public PlatformNode from, to;
    public float launchX; // where to stand before jumping (jump links)
    public float landX; // where to aim on 'to'
    public bool needsJump; // false = walk off the edge and fall
    public int side; // direction of travel when walking off
    public float cost;
    public float blockedUntil; // set when the link failed
    public float launchMin, launchMax;
    public bool vertical;
    public float requiredJumpVelocity;
}

public class PlatformNavigator
{
    private enum Phase { Approach, Airborne }

    private const float ArriveTolerance = 0.15f;
    private const float MaxAirTime = 3f;
    private const float BlockDuration = 8f;
    private const float ApexClearance = 0.1f;
    private const float MinHeightDifference = 0.1f;
    private string lastPlanLog;
    private readonly EnemyController enemy;

    // Jump model, read from the enemy's real physics settings
    private float gravity, damping, dt, effSpeed;

    private List<PlatformEdge>[] adjacency;
    private int edgeCount;
    private bool warnedNoGraph;

    private PlatformNode lastPlayerNode, goalNode;
    private List<PlatformEdge> path;
    private int edgeIndex;
    private Phase phase;
    private float airbornSince;
    private const float JumpMargin = 0.4f;

    public PlatformNavigator(EnemyController controller)
    {
        enemy = controller;
    }

    public bool IsAirborne => path != null && phase == Phase.Airborne;

    public void Reset()
    {
        path = null;
        edgeIndex = 0;
        goalNode = null;
        phase = Phase.Approach;
    }

    // Returns true if it took control of the movement this frame,
    // false if the normal chase should run (same platform, or no way to get there).
    public bool Step()
    {
        var graph = PlatformGraph.Instance;
        if (graph == null)
        {
            if (!warnedNoGraph)
            {
                Debug.LogWarning("PlatformNavigator : no PlatformGraph in the scene. Platform navigation is off.", enemy);
                warnedNoGraph = true;
            }
            return false;
        }
        if (adjacency == null) BuildAdjacency(graph);

        bool grounded = enemy.IsGrounded();
        PlatformNode me = grounded ? graph.NodeOf(enemy.GroundCollider()) : null;

        // Walking off an edge: the ground disappearing is the plan, not a failure
        if (!grounded && phase == Phase.Approach && path != null
            && edgeIndex < path.Count && !path[edgeIndex].needsJump)
            BeginAirborne();

        if (phase == Phase.Airborne && path != null)
            return StepAirborne(me, grounded);

        PlatformNode target = TrackPlayerPlatform(graph);
        if (target == null || !grounded || me == null || me == target)
        {
            Reset();
            return false;
        }

        if (path == null || goalNode != target || edgeIndex >= path.Count || path[edgeIndex].from != me)
        {
            path = FindPath(me, target);
            string plan = path == null
            ? "AUCUN"
            : string.Join(" ", path.ConvertAll(e => $"#{e.from.index}->#{e.to.index}"));
            string blocked = "";
            foreach (var list in adjacency)
                foreach (var e in list)
                    if (Time.time < e.blockedUntil)
                        blocked += $" #{e.from.index}->#{e.to.index}({e.blockedUntil - Time.time:0}s)";

            string msg = $"[Nav] plan : moi #{me.index}(y={me.topY:0.0}) -> cible #{target.index}(y={target.topY:0.0}) = {plan} | bloques:{(blocked == "" ? " aucun" : blocked)}";
            if (msg != lastPlanLog) { lastPlanLog = msg; Debug.Log(msg, enemy); }
            goalNode = target;
            edgeIndex = 0;
            phase = Phase.Approach;
            if (path == null) return false;
        }

        return StepApproach();
    }

    private float MinJumpVelocity(float deltaY, float neededDistance, float maxV)
    {
        float safety = enemy.Config.jumpReachSafety;

        bool Ok(float v)
        {
            if (JumpPhysics.ApexHeight(v, gravity, damping, dt) < deltaY + JumpMargin) return false;
            float t = JumpPhysics.TimeToDescendThrough(v, deltaY, gravity, damping, dt);
            return t >= 0f && effSpeed * t * safety >= neededDistance;
        }

        if (!Ok(maxV)) return maxV;

        float lo = 0f, hi = maxV;
        for (int i = 0; i < 14; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (Ok(mid)) hi = mid; else lo = mid;
        }
        return hi;
    }

    // ---------- Execution ----------
    private bool StepApproach()
    {
        var edge = path[edgeIndex];
        float speed = enemy.Config.moveSpeed;

        if (!edge.needsJump)
        {
            // Walk off the edge; the fall starts by itself
            enemy.MoveHorizontal(edge.side * speed);
            if (!enemy.IsGrounded()) BeginAirborne();
            return true;
        }

        float takeoffX = Mathf.Clamp(enemy.CenterX, edge.launchMin, edge.launchMax);
        float dx = takeoffX - enemy.CenterX;
        if (Mathf.Abs(dx) > ArriveTolerance)
        {
            enemy.MoveHorizontal(Mathf.Sign(dx) * speed);
            return true;
        }

        enemy.StopHorizontal();
        if (enemy.CanJump())
        {
            if (edge.vertical) edge.landX = takeoffX;   // straight up: land where it took off
            enemy.Jump(edge.requiredJumpVelocity);
            BeginAirborne();
        }
        return true;
    }

    private void BeginAirborne()
    {
        phase = Phase.Airborne;
        airbornSince = Time.time;
    }

    private bool StepAirborne(PlatformNode me, bool grounded)
    {
        var edge = path[edgeIndex];

        bool landed = grounded && Time.time > airbornSince + 0.1f && enemy.Rb.linearVelocity.y <= 0.1f;
        if (landed && !edge.needsJump && me == edge.from) landed = false;
        
        if (landed)
        {
            enemy.StopHorizontal();
            if (me == edge.to)
            {
                edgeIndex++;
                phase = Phase.Approach;
                if (path.Count <= edgeIndex) Reset();
            }
            else
            {
                // Landed somewhere else than planned: don't retry this link for a while
                edge.blockedUntil = Time.time + BlockDuration;
                Reset();
            }
            return true;
        }

        if (Time.time - airbornSince > MaxAirTime)
        {
            edge.blockedUntil = Time.time + BlockDuration;
            Reset();
            return false;
        }

        // Steer toward the landing point
        float dx = edge.landX - enemy.CenterX;
        if (Mathf.Abs(dx) > 0.05f) enemy.MoveHorizontal(Mathf.Sign(dx) * enemy.Config.moveSpeed);
        else enemy.StopHorizontal();
        return true;
    }

    // The platform under the player. Only updated while the player is grounded,
    // so a jump doesn't make the enemy change plans.
    private PlatformNode TrackPlayerPlatform(PlatformGraph graph)
    {
        var movement = enemy.PlayerMovement;
        bool playerGrounded = movement == null || movement.IsGrounded;

        if (playerGrounded || lastPlayerNode == null)
        {
            Vector2 origin = enemy.PlayerCollider != null 
                ? (Vector2)enemy.PlayerCollider.bounds.center
                : (Vector2)enemy.Player.position;

            PlatformNode node = graph.NodeBelow(origin);
            if (node != null) lastPlayerNode = node;
        }
        return lastPlayerNode;
    }

    // ---------- Path search (Dijkstra, the graph is small) ----------

    private List<PlatformEdge> FindPath(PlatformNode from, PlatformNode to)
    {
        int n = adjacency.Length;
        var dist = new float[n];
        var prev = new PlatformEdge[n];
        var arrivalX = new float[n]; // where we stand on each platform when we get there
        var done = new bool[n];
        for (int i = 0; i < n; i++) dist[i] = float.MaxValue;
        dist[from.index] = 0f;
        arrivalX[from.index] = enemy.CenterX;
        float playerX = enemy.Player.position.x;

        for (int iter = 0; iter < n; iter++)
        {
            int u = -1;
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
                if (!done[i] && dist[i] < best) { best = dist[i]; u = i; }

            if (u == -1 || u == to.index) break;
            done[u] = true;

            foreach (var e in adjacency[u])
            {
                float takeoffX = Mathf.Clamp(arrivalX[u], e.launchMin, e.launchMax);
                float landX = e.vertical ? takeoffX : e.landX;
                float walk = Mathf.Abs(takeoffX - arrivalX[u]);

                float nd = dist[u] + e.cost + walk * 0.3f;
                if (e.to == to) nd += Mathf.Abs(landX - playerX) * 0.3f;

                if (nd < dist[e.to.index])
                {
                    dist[e.to.index] = nd;
                    prev[e.to.index] = e;
                    arrivalX[e.to.index] = landX;
                }
            }
        }
    if (prev[to.index] == null) return null;

    var result = new List<PlatformEdge>();
    for (var e = prev[to.index]; e != null; e = prev[e.from.index]) result.Add(e);
    result.Reverse();
    return result;
    }

    // ---------- Building the links ----------

    private void BuildAdjacency(PlatformGraph graph)
    {
        var nodes = graph.Nodes;
        var cfg = enemy.Config;

        gravity = Mathf.Abs(Physics2D.gravity.y) * enemy.Rb.gravityScale;
        damping = enemy.Rb.linearDamping;
        dt = Time.fixedDeltaTime;
        // Horizontal speed is re-applied every frame, then damped once per physics step
        effSpeed = cfg.moveSpeed / (1f + dt * damping);

        adjacency = new List<PlatformEdge>[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) adjacency[i] = new List<PlatformEdge>();
        edgeCount = 0;

        foreach (var a in nodes)
        {
            foreach (var b in nodes)
            {
                if (a == b) continue;
                float deltaY = b.topY - a.topY;
                if (deltaY < -MinHeightDifference) AddDropEdges(a,b ,deltaY);
                else AddJumpEdge(a, b, deltaY);
            }
        }

        float apex = JumpPhysics.ApexHeight(cfg.jumpVelocity, gravity, damping, dt);
        Debug.Log($"[PlatformNavigator] {enemy.name}: jump apex {apex:0.00} units, " +
                  $"{edgeCount} links between {nodes.Count} platforms", enemy);
    }

    private void Add(PlatformEdge e)
    {
        var list = adjacency[e.from.index];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].to == e.to && list[i].side == e.side) 
            {
                if (e.cost < list[i].cost) list[i] = e;
                return;
            }
        }
        list.Add(e);
        edgeCount++;
    }

    // B is higher than A, or at about the same height: a jump
    private void AddJumpEdge(PlatformNode a, PlatformNode b, float deltaY)
    {
        var cfg = enemy.Config;
        bool higher = deltaY > MinHeightDifference;
        string tag = $"[Nav] #{a.index}->#{b.index} (dY {deltaY:0.00})";

        if (higher && !b.oneWay) { Debug.Log($"{tag} REJET: cible pas oneWay"); return; }

        float aMin = a.minX + cfg.launchInset, aMax = a.maxX - cfg.launchInset;
        float bMin = b.minX + cfg.landingMargin, bMax = b.maxX - cfg.landingMargin;
        if (aMin > aMax || bMin > bMax) { Debug.Log($"{tag} REJET: plateforme trop etroite"); return; }

        bool overlap = aMax >= bMin && bMax >= aMin;
        if (overlap && !higher) return;

        float launchX, landX, launchMin, launchMax;
        if (overlap)
        {
            float lo = Mathf.Max(aMin, bMin), hi = Mathf.Min(aMax, bMax);
            launchMin = lo; launchMax = hi;
            launchX = landX = (lo+hi) * 0.5f;
        }
        else if (bMin > aMax) { launchX = launchMin = launchMax = aMax; landX = bMin; }
        else { launchX = launchMin = launchMax = aMin; landX = bMax; }

        float needed = Mathf.Abs(landX - launchX);

        float apex = JumpPhysics.ApexHeight(cfg.jumpVelocity, gravity, damping, dt);
        if (apex < deltaY + ApexClearance) { Debug.Log($"{tag} REJET: apex {apex:0.00} trop bas"); return; }

        float t = JumpPhysics.TimeToDescendThrough(cfg.jumpVelocity, deltaY, gravity, damping, dt);
        if (t < 0f) { Debug.Log($"{tag} REJET: t<0 (n'atteint pas la hauteur)"); return; }

        float reach = effSpeed * t * cfg.jumpReachSafety;
        if (reach < needed) { Debug.Log($"{tag} REJET: portee {reach:0.00} < distance {needed:0.00}"); return; }

        float jumpV = MinJumpVelocity(deltaY, needed, cfg.jumpVelocity);

        Debug.Log($"{tag} OK");
        Add(new PlatformEdge
        {
            from = a, to = b, launchX = launchX, landX = landX, needsJump = true,
            launchMin = launchMin, launchMax = launchMax, vertical = overlap,
            cost = 2f + needed * 0.1f,
            requiredJumpVelocity = jumpV
        });
    }

    // B is lower than A: walk off an edge, or jump off it if the gap is too wide for a fall
    private void AddDropEdges(PlatformNode a, PlatformNode b, float deltaY)
    {
        var cfg = enemy.Config;
        float clearance = enemy.HalfWidth + 0.1f;

        for (int side = -1; side <= 1; side += 2)
        {
            // u = side * x, so moving "forward" past the edge always increases u
            float edgeX = side > 0 ? a.maxX : a.minX;
            float uOut = side * edgeX + clearance; // the enemy has fully left A
            float uWalk = uOut + 0.15f; // where it walks to
            float uBmin = side > 0 ? b.minX + cfg.landingMargin : -(b.maxX - cfg.landingMargin);
            float uBmax = side > 0 ? b.maxX - cfg.landingMargin : -(b.minX + cfg.landingMargin);
            if (uBmin > uBmax) continue;

            float uLand = Mathf.Max(uWalk, uBmin);
            if (uLand > uBmax) continue; // nothing of B lies beyond this edge
            
            // 1) Walk off and fall
            float tFall = JumpPhysics.TimeToDescendThrough(0f, deltaY, gravity, damping, dt);
            if (tFall > 0f && effSpeed * tFall * cfg.jumpReachSafety >= uLand - uOut)
            {
                Add(new PlatformEdge
                {
                    // walk-off edge: add
                    launchMin = side * uWalk, launchMax = side * uWalk,
                    from = a, to = b, side = side, needsJump = false,
                    launchX = side * uWalk, landX = side * uLand,
                    cost = 1f + (uLand - uOut) * 0.1f 
                });
                continue;
            }

            // 2) Jump off the edge to go farther
            float uLaunch = side * edgeX - cfg.launchInset;
            float tClear = JumpPhysics.TimeToDescendThrough(cfg.jumpVelocity, 0f, gravity, damping, dt);
            float tLand = JumpPhysics.TimeToDescendThrough(cfg.jumpVelocity, deltaY, gravity, damping, dt);
            if (tClear < 0f || tLand < 0f) continue;

            float s = effSpeed * cfg.jumpReachSafety;
            if (s * tClear < uOut - uLaunch) continue;   // wouldn't clear A's edge before coming down
            if (s * tLand < uLand - uLaunch) continue;

            Add(new PlatformEdge
            {
                // jump-off edge: add
                launchMin = side * uLaunch, launchMax = side * uLaunch,
                from = a, to = b, side = side, needsJump = true,
                launchX = side * uLaunch, landX = side * uLand,
                cost = 2f + (uLand - uLaunch) * 0.1f,
                requiredJumpVelocity = cfg.jumpVelocity,
            });
        } 
    }

      // ---------- Debug ----------

    public string Describe()
    {
        if (adjacency == null) return "NAV: pas encore construit";

        var graph = PlatformGraph.Instance;
        PlatformNode me = enemy.IsGrounded() ? graph.NodeOf(enemy.GroundCollider()) : null;

        string s = $"NAV: {edgeCount} liens | moi sur #{(me != null ? me.index.ToString() : "?")}";
        s += $" | joueur sur #{(lastPlayerNode != null ? lastPlayerNode.index.ToString() : "?")}";
        s += $" | phase {phase}";

        if (path == null) s += " | pas de chemin";
        else s += $" | chemin {edgeIndex}/{path.Count}";

        return s;
    }

    public void DrawGizmos()
    {
        if (adjacency == null) return;
        foreach (var list in adjacency)
        {
            foreach (var e in list)
            {
                bool onPath = path != null && path.Contains(e);
                Gizmos.color = onPath ? Color.white
                    : e.needsJump ? new Color(1f, 0.9f, 0.2f, 0.6f)
                    : new Color(0.3f, 1f, 0.3f, 0.6f);

                var p0 = new Vector3(e.launchX, e.from.topY, 0f);
                var p1 = new Vector3(e.landX, e.to.topY, 0f);
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawWireSphere(p0, 0.08f);
            }
        }
    }
}