using UnityEngine;
using System.Collections;

public class StageManager : MonoBehaviour
{
    [SerializeField] private VictoryMenu victoryMenu;   // only set in the LAST stage
    [SerializeField] private float clearDelay = 1.5f;   // let the last death animation play first

    [Header("Enemies")]
    [Tooltip("Hades-style waves. If set, the placed enemies below are ignored.")]
    [SerializeField] private EnemySpawner spawner;
    [Tooltip("Fallback: enemies already placed in the scene.")]
    [SerializeField] private Health[] enemies;
    [SerializeField] private Collider2D exitWall;
    [SerializeField] private GameObject goSign;
    [SerializeField] private GameObject stageExit;
    
    private int enemiesLeft;

    void Start()
    {
        if (spawner != null)
        {
            spawner.OnAllWavesCleared += HandleStageCleared;
            return;
        }

        enemiesLeft = enemies.Length;
        foreach (Health enemy in enemies)
        {
            enemy.OnDeath += HandleEnemyDeath;
        }
    }

    void OnDestroy()
    {
        if (spawner != null) spawner.OnAllWavesCleared -= HandleStageCleared;
    }

    void HandleEnemyDeath()
    {
        enemiesLeft--;

        if (enemiesLeft <= 0)
        {
            Debug.Log("Stage Clear!");
            StartCoroutine(OpenExitWhenGone());
        }
    }

    IEnumerator OpenExitWhenGone()
    {
        while (!AllEnemiesGone())
            yield return null;   // check again next frame

        HandleStageCleared();
    }

    void OpenExit()
    {
        Debug.Log("Stage clear!");
        exitWall.enabled = false;
        goSign.SetActive(true);
        if (stageExit != null) stageExit.SetActive(true);
    }

    bool AllEnemiesGone()
    {
        foreach (Health enemy in enemies)
        {
            if (enemy != null) return false;
        }
        return true;
    }

    void HandleStageCleared()
    {
        StartCoroutine(ClearStage());
    }

    IEnumerator ClearStage()
    {
        yield return new WaitForSeconds(clearDelay);

        if (victoryMenu != null)
        {
            victoryMenu.Show();   // last stage: you win
            yield break;
        }
        OpenExit();
    }
}