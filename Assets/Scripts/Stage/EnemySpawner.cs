using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    public int count = 3;
}

[Serializable]
public class Wave
{
    public SpawnEntry[] entries;
    public float delayBeforeWave;
    public float delayBetweenSpawns;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("If empty, found by the 'Player' tag.")]
    [SerializeField] private Transform player;
    [Tooltip("Spawn points closer than this to the player are skipped.")]
    [SerializeField] private float minDistanceFromPlayer = 6f;
    [Tooltip("Max enemies alive at once. The rest waits.")]
    [SerializeField] private int maxAlive = 2;
    [SerializeField] private Wave[] waves;
    private readonly List<Transform> freePoints = new List<Transform>();

    public event Action<int> OnWaveStarted;   // wave index
    public event Action OnAllWavesCleared;

    private readonly HashSet<Health> alive = new ();

    private void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if ( p != null) player = p.transform;
        }
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            Wave wave = waves[i];
            yield return new WaitForSeconds(wave.delayBeforeWave);
            OnWaveStarted?.Invoke(i);
            freePoints.Clear();
            freePoints.AddRange(spawnPoints);

            var queue = new List<GameObject>();
            foreach (var entry in wave.entries)
                for (int n=0; n<entry.count; n++)
                    queue.Add(entry.prefab);
            Shuffle(queue);

            foreach (var prefab in queue)
            {
                yield return new WaitUntil(() => AliveCount() < maxAlive);
                Spawn(prefab);
                yield return new WaitForSeconds(wave.delayBetweenSpawns);
            }

            yield return new WaitUntil(() => AliveCount() == 0);
        }

        OnAllWavesCleared?.Invoke();
        
    }
    
    private int AliveCount()
    {
        alive.RemoveWhere(h => h == null);
        return alive.Count;
    }

    private void Spawn(GameObject prefab)
    {
        Debug.Log($"Spawn demandé : prefab={prefab}, points={spawnPoints.Length}");
        if (prefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Spawn impossible : prefab null ou aucun point de spawn", this);
            return;
        }

        Transform point = PickSpawnPoint();
        
        GameObject enemy = Instantiate(prefab, point.position, Quaternion.identity);

        if (!enemy.TryGetComponent<Health>(out var health)) return;

        alive.Add(health);
        health.OnDeath += () => alive.Remove(health);
    }

    private Transform PickSpawnPoint()
    {
        if (player == null) return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        if (freePoints.Count == 0) freePoints.AddRange(spawnPoints);

        var valid = new List<Transform>();
        Transform farthest = spawnPoints[0];
        float farthestDist = -1f;

        foreach (var point in spawnPoints)
        {
            float distance = player != null 
            ? Vector2.Distance(point.position, player.position)
            : float.MaxValue;
            if (distance >= minDistanceFromPlayer) valid.Add(point);
            if (distance > farthestDist) { farthestDist = distance; farthest = point; }
        }

        Transform chosen = valid.Count > 0 
        ? valid[UnityEngine.Random.Range(0, valid.Count)]
        : farthest;

        freePoints.Remove(chosen);
        return chosen;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.green;
        foreach (var p in spawnPoints)
            if (p != null) Gizmos.DrawWireSphere(p.position, 0.4f);    
    }
}