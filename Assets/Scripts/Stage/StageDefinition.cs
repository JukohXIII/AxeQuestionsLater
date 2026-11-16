using UnityEngine;

[CreateAssetMenu(fileName = "NewStage", menuName = "Run/Stage Definition")]
public class StageDefinition : ScriptableObject
{
    [Tooltip("Scene name. The scene must be in Build Profile.")]
    public string sceneName;
    public string displayName;
    public Sprite mapIcon;
}