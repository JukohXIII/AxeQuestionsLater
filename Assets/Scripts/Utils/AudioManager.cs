using System.Collections.Generic;
using UnityEngine;

public static class AudioManager
{
    const int PoolSize = 16;

    static SoundBank bank;
    static Dictionary<Sfx, SoundBank.Entry> map;
    static AudioSource[] pool;
    static int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]    
    static void ResetStatics() { bank = null; map = null; pool = null; next = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        bank = Resources.Load<SoundBank>("SoundBank");
        if (bank == null) { Debug.LogWarning("[Audio] Resources/SoundBank not found : silence."); return;}

        map = new Dictionary<Sfx, SoundBank.Entry>();
        foreach (var e in bank.entries) map[e.id] = e;

        var go = new GameObject("AudioManager");
        Object.DontDestroyOnLoad(go);
        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            pool[i] = go.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f;
        }

        EnemyController.OnAnyEnemyDied += _ => Play(Sfx.EnemyDie); //
    }

    public static void Play(Sfx id, float volumeScale = 1f)
    {
        if (map == null || !map.TryGetValue(id, out var e)) return;
        if (e.clips == null || e.clips.Length == 0) return;
        var clip = e.clips[Random.Range(0, e.clips.Length)];
        if (clip == null) return;

        var src = pool[next];
        next = (next + 1) % PoolSize;
        src.pitch = Random.Range(e.pitchRange.x, e.pitchRange.y);
        src.PlayOneShot(clip, e.volume * volumeScale);
    }
}   