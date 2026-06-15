using UnityEngine;
using JoonyleGameDevKit;

public sealed class FlyingEnemyChasingState : StateBase<FlyingEnemyBehaviour>
{
    public override void Enter(FlyingEnemyBehaviour owner)
    {
        
    }

    public override void Exit(FlyingEnemyBehaviour owner)
    {
        owner.Rigid.linearVelocity = Vector2.zero;
    }

    public override void Update(FlyingEnemyBehaviour owner, float deltaTime)
    {
        var player = owner.Player;

        if (player.IsDead)
        {
            owner.ChangeState<FlyingEnemyIdleState>();
            return;
        }

        var distance = Vector2.Distance(owner.transform.position, player.transform.position);
        if (distance > owner.LoseRange)
        {
            owner.ChangeState<FlyingEnemyIdleState>();
            return;
        }

        var direction = ((Vector2)player.transform.position - (Vector2)owner.transform.position).normalized;
        var isFacingRight = direction.x > 0f;
        owner.Rigid.linearVelocity = direction * owner.ChaseSpeed;
        owner.SetFacingDir(isFacingRight);
    }
}
