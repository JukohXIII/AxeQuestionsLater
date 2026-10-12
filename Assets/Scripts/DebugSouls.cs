using UnityEngine;

public class DebugSouls : MonoBehaviour
{
    [ContextMenu("Add 500 souls")]
    void Add() => MetaProgression.AddSouls(500);

    [ContextMenu("Reset save")]
    void ResetSave() => MetaProgression.ResetAll();
}