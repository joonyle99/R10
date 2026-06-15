using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerStunState : StateBase<PlayerBehaviour>
{
    private float _timer;

    public override void Enter(PlayerBehaviour owner)
    {
        _timer = owner.StunDuration;
        Time.timeScale = 1f;
    }

    public override void Exit(PlayerBehaviour owner) { }

    public override void FixedUpdate(PlayerBehaviour owner, float fixedDeltaTime) { }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            if (owner.PlatformerSensor.IsGrounded)
            {
                owner.ChangeState<PlayerGroundState>();
            }
            else
            {
                owner.IsRecoveringFromStun = true;
                owner.ChangeState<PlayerAirState>();
            }
        }
    }
}
