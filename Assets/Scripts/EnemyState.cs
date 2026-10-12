using UnityEngine;

public interface IEnemyState
{
    void Enter();
    void Tick();
    void Exit();
}

public abstract class EnemyState : IEnemyState
{
    protected EnemyController enemyController;

    public EnemyState(EnemyController enemyController)
    {
        this.enemyController = enemyController;
    }

    public virtual void Enter() {}
    public virtual void Tick() {}
    public virtual void Exit() {}
}