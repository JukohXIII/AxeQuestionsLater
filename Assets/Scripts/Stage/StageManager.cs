using UnityEngine;
using System.Collections;

public class StageManager : MonoBehaviour
{
    [SerializeField] private Health[] enemies;
    private int enemiesLeft;
    [SerializeField] private Collider2D exitWall;
    [SerializeField] private GameObject goSign;
    [SerializeField] private GameObject stageExit;

    void Start()
    {
        enemiesLeft = enemies.Length;
        foreach (Health enemy in enemies)
        {
            enemy.OnDeath += HandleEnemyDeath;
        }
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
}