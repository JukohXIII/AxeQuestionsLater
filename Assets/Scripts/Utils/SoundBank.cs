using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Sound bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sfx id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.7f;
        public Vector2 pitchRange = new(0.95f, 1.05f);
    }

    public Entry[] entries;

#if UNITY_EDITOR
    [ContextMenu("Fill entries")]
    void FillEntries()
    {
        var list = new System.Collections.Generic.List<Entry>(entries ?? new Entry[0]);
        foreach (Sfx sound in Enum.GetValues(typeof(Sfx)))
            if (!list.Exists(e => e.id == sound))
                list.Add(new Entry { id = sound, clips = new AudioClip[0] });
        entries = list.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}