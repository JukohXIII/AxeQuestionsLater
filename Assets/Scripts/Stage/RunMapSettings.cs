using UnityEditor.SceneManagement;
using UnityEngine;

// Content of the map generator. Lives in Resources so RunMap can find it from any scene.
[CreateAssetMenu(fileName = "RunMapSettings", menuName = "Run/Map settings")]
public class RunMapSettings : ScriptableObject
{
    public StageDefinition[] stagePool;
    public StageDefinition bossStage;
}