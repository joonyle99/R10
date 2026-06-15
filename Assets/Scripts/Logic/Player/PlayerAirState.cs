using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerAirState : StateBase<PlayerBehaviour>
{
    private Rigidbody2D _rigid;
    private SlingConfig _config;
    private SlingState _budget; // 튕김 자격(누적 거리/남은 시간/횟수) 추적용 — 이동은 rigidbody + ApplyGravity가 담당
    private int _bonusKicksLeft;
    private bool _isHitPausing;
    private bool _noChargeFeedbackSent;

    public override void Enter(PlayerBehaviour owner)
    {
        // owner.PlayPlayerAnimation(PlayerAnimationState.Roll);

        _isHitPausing = false;
        _noChargeFeedbackSent = false;

        var sling = owner.SlingBehaviour;

        _rigid  = sling.Rigid;
        _config = sling.Config;

        _bonusKicksLeft = _config.bonusKickCount;

        if (sling.ConsumeSling())
        {
            var shotDir = sling.LastShotDir;
            owner.SetFacingDir(shotDir.x > 0f);
            _budget = SlingState.Create(_rigid.position, shotDir, _config);

            _isHitPausing = true;

            owner.SquashStretch.HoldSquash(); // 예비동작: 웅크린 채 유지
            owner.PauseAndLaunch(owner.LaunchPauseDuration, _budget.Velocity, () =>
            {
                _isHitPausing = false;
                owner.SquashStretch.PlayStretch();
            });
        }
        else
        {
            // 슬링 없이 진입(낙하 등): Remaining 0 → 벽 킥 비활성
            _budget = default;
            _budget.Position = _rigid.position;
        }
    }

    public override void Exit(PlayerBehaviour owner) { }

    public override void FixedUpdate(PlayerBehaviour owner, float fixedDeltaTime)
    {
        if (_isHitPausing) return;

        _budget.Remaining -= fixedDeltaTime;
        _budget.Total += (_rigid.position - _budget.Position).magnitude;
        _budget.Position = _rigid.position;
    }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        if (_isHitPausing) return;

        // 상승 가드: 히트포즈 해제(Update 단계)와 센서 갱신(FixedUpdate 단계) 사이에
        // 낡은 IsGrounded가 남아 있을 수 있다 — 그대로 믿으면 GroundState가 발사 직후 x속도를 지워버린다
        if (owner.PlatformerSensor.IsGrounded && _rigid.linearVelocity.y <= 0f)
        {
            owner.ChangeState<PlayerGroundState>();
        }
        else if (owner.PointerInput.IsDragging && owner.SlingBehaviour.HasCharge)
        {
            // 공중 재조준: 차지가 남아 있을 때만 (0이면 드래그해도 조준 진입 자체가 안 됨)
            owner.ChangeState<PlayerAimState>();
        }
        else if (owner.PointerInput.IsDragging && !_noChargeFeedbackSent)
        {
            _noChargeFeedbackSent = true;
            owner.PlayerDisplay.ShowNoChargeEffect();
        }
        else if (!owner.PointerInput.IsDragging)
        {
            _noChargeFeedbackSent = false;
        }
    }

    public void OnCollisionEnter(PlayerBehaviour owner, Collision2D collision)
    {
        if (_isHitPausing) return;
        if ((owner.PlatformerSensor.GroundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;

            if (!SlingSimulator.IsWall(normal, _config)) continue;

            if (SlingSimulator.CanBounce(in _budget, _config))
            {
                // 예산 킥: 조준선이 예측한 튕김
                SlingSimulator.Bounce(ref _budget, normal, _config);
                WallKick(owner, _budget.Velocity);
            }
            else if (_bonusKicksLeft > 0)
            {
                // 보너스 킥: 예산이 소진돼도 벽에 닿으면 추가 점프 — 조준선에는 표시되지 않는 규칙
                _bonusKicksLeft--;
                WallKick(owner, SlingSimulator.Kick(normal, _config.kick));
            }
            else
            {
                // 데드 바운스: 킥 불가면 벽 반대로 살짝 밀어낸다 (벽을 타고 떨어지는 그림 방지, Poinpy식)
                var vel = _rigid.linearVelocity;
                vel.x = normal.x * _config.wallRepelSpeed;
                _rigid.linearVelocity = vel;
                owner.SetFacingDir(vel.x > 0f);
            }

            return;
        }
    }

    private void WallKick(PlayerBehaviour owner, Vector2 vel)
    {
        _isHitPausing = true;

        owner.SetFacingDir(vel.x > 0f);
        owner.SquashStretch.SetContactSurface(ContactSurface.Wall);
        owner.PlayPlayerAnimation(PlayerAnimationState.Wall);
        owner.SquashStretch.HoldSideSquash();
        owner.PauseAndLaunch(owner.BouncePauseDuration, vel, () =>
        {
            _isHitPausing = false;
            owner.SquashStretch.PlayStretch();
            owner.PlayPlayerAnimation(PlayerAnimationState.Roll);
        });
    }
}
