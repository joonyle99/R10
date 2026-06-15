using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerStunState : StateBase<PlayerBehaviour>
{
    private const float STUN_DURATION = 0.4f;
    private float _timer;

    public override void Enter(PlayerBehaviour owner)
    {
        _timer = STUN_DURATION;

        // 수평 모멘텀 제거, 수직은 소량만 남겨 기절감 연출 후 낙하
        owner.Rigid.linearVelocity = new Vector2(0f, 1.5f);

        owner.IsInvincible = true;
    }

    public override void Exit(PlayerBehaviour owner)
    {
        owner.IsInvincible = false;
    }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        _timer -= deltaTime;

        if (owner.PlatformerSensor.IsGrounded || _timer <= 0f)
            owner.ChangeState<PlayerAirState>();
    }

    public override void FixedUpdate(PlayerBehaviour owner, float fixedDeltaTime) { }
}