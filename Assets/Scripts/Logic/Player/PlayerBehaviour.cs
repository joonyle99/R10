using System;
using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections;
using System.Collections.Generic;

public enum PlayerAnimationState
{
    Idle,
    Roll,
    Wall,
}

public sealed class PlayerBehaviour : SlingEntity
{
    public CameraController CameraController { get; private set; }
    public IPointerInput PointerInput { get; private set; }

    [Space]

    [SerializeField] private float _launchPauseDuration = 0.06f;
    [SerializeField] private float _bouncePauseDuration = 0.05f;
    [SerializeField] private float _enemyHitPauseDuration = 0.06f;
    public float LaunchPauseDuration => _launchPauseDuration;
    public float BouncePauseDuration => _bouncePauseDuration;

    [SerializeField] private float _knockbackX = 12f;
    [SerializeField] private float _knockbackY = 8f;
    [SerializeField] private float _invincibleDuration = 1.5f;
    [SerializeField] private float _blinkInterval = 0.08f;

    [Header("Propulsion Judgment")] 
    [SerializeField] private float _propelHighThresholdSqr = 120f; // Propelled 상태로 진입하는 속도² 임계값 (히스테리시스 밴드)
    [SerializeField] private float _propelLowThresholdSqr = 80f; //  Drifting 상태로 탈출하는 속도² 임계값
    [SerializeField] private float _propelGraceDuration = 0.3f;

    private bool _isPropelled;
    private float _propelGraceTimer;
    public bool IsPropelled => _isPropelled;

    private ComboSystem _combo;
    public ComboSystem Combo => _combo;

    private Coroutine _invincibleCoroutine;

    private PlatformerSensor _platformerSensor;
    public PlatformerSensor PlatformerSensor => _platformerSensor;

    private PlayerDisplay _playerDisplay;
    public PlayerDisplay PlayerDisplay => _playerDisplay;

    private PointerInputVisualizer _pointerVisualizer;
    public PointerInputVisualizer PointerVisualizer => _pointerVisualizer;

    private SquashStretch _squashStretch;
    public SquashStretch SquashStretch => _squashStretch;

    private Headband _headbandTrail;
    public Headband HeadbandTrail => _headbandTrail;
    
    private StateMachine<PlayerBehaviour> _fsm;
    public StateMachine<PlayerBehaviour> FSM => _fsm;

    public bool CanAim => FSM.CurrState is PlayerGroundState or PlayerAimState
                        || (FSM.CurrState is PlayerAirState && SlingBehaviour.HasCharge);

#if UNITY_EDITOR
    private string _debugState;
#endif

    private static readonly int IDLE = Animator.StringToHash("Idle");
    private static readonly int ROLL = Animator.StringToHash("Roll");
    private static readonly int WALL = Animator.StringToHash("Wall");
    
    private static readonly float HIT_COLOR_MIN = 0f;
    private static readonly float HIT_COLOR_MAX = 0.7f;
    private static readonly float HIT_DURATION = 0.2f;

    private Sequence _hitColorSequence;

    private bool _isAimSlowing;

    private void OnDestroy()
    {
        if (PointerInput != null)
        {
            PointerInput.OnPress -= OnPointerPress;
            PointerInput.OnRelease -= OnPointerRelease;
        }
    }

    public void Initialize(CameraController cameraController, IPointerInput pointerInput, Action<int> onDamaged, Action onDead)
    {
        _platformerSensor = GetComponent<PlatformerSensor>();
        _platformerSensor.Initialize();

        InitSlingEntity(onDamaged, onDead, _platformerSensor.GroundLayer);

        CameraController = cameraController;
        CameraController.ActivateFollow(transform);
        PointerInput = pointerInput;
        PointerInput.OnPress += OnPointerPress;
        PointerInput.OnRelease += OnPointerRelease;

        _playerDisplay = GetComponentInChildren<PlayerDisplay>();
        _playerDisplay.Initialize(SlingBehaviour);

        _pointerVisualizer = FindFirstObjectByType<PointerInputVisualizer>();
        _pointerVisualizer.Initialize(pointerInput, () => CanAim);

        _squashStretch = GetComponent<SquashStretch>();
        _squashStretch.Initialize(Pivot, Animator.transform);

        _headbandTrail = GetComponentInChildren<Headband>();
        _headbandTrail.Initialize();

        _combo = new ComboSystem();

        _fsm = new StateMachine<PlayerBehaviour>(this);
        _fsm.AddState(new PlayerGroundState());
        _fsm.AddState(new PlayerAimState());
        _fsm.AddState(new PlayerAirState());
        _fsm.AddState(new PlayerStunState());

        ChangeState<PlayerAirState>();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (IsDead) return;

        base.FixedTick(fixedDeltaTime);

        _platformerSensor?.FixedTick(fixedDeltaTime);

        if (_platformerSensor != null && !_platformerSensor.IsGrounded)
            ApplyGravity(fixedDeltaTime);

        _fsm?.FixedUpdate(fixedDeltaTime);
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead) return;

        base.Tick(deltaTime);

        _fsm?.Update(deltaTime);
        _pointerVisualizer?.Tick(deltaTime);

        UpdatePropelState(deltaTime);
    }

    public void ChangeState<TState>() where TState : StateBase<PlayerBehaviour>
    {
#if UNITY_EDITOR
        _debugState = typeof(TState).Name;
#endif

        _fsm.ChangeState<TState>();
    }

    // ============ ... ============

    protected override void OnDamaged(int damage, Vector2 sourcePos)
    {
        base.OnDamaged(damage, sourcePos);

        var dirX = Rigid.position.x >= sourcePos.x ? 1f : -1f;
        Rigid.linearVelocity = new Vector2(dirX * _knockbackX, _knockbackY);

        if (_invincibleCoroutine != null) StopCoroutine(_invincibleCoroutine);
        _invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;

        var blink = SpriteRenderer.DOFade(0f, _blinkInterval)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetLink(gameObject);

        yield return new WaitForSecondsRealtime(_invincibleDuration);

        blink.Kill();
        var color = SpriteRenderer.color;
        color.a = 1f;
        SpriteRenderer.color = color;

        IsInvincible = false;
        _invincibleCoroutine = null;
    }

    private void OnPropelledHit(EnemyBehaviour enemy)
    {
        enemy.TakeDamage(enemy.MaxHp, enemy.Rigid.position);
        _combo.Add();
        SlingBehaviour.AddCharge();
        StartCoroutine(HitStopRoutine(_enemyHitPauseDuration));
        // velocity 보존 — 관통
    }

    private void OnPointerPress(Vector2 _)
    {
        if (!CanAim) return;
        _isAimSlowing = true;
        Time.timeScale = SlingBehaviour.Config.aimTimeScale;
    }

    private void OnPointerRelease(Vector2 _)
    {
        _isAimSlowing = false;
        Time.timeScale = 1f;
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = _isAimSlowing ? SlingBehaviour.Config.aimTimeScale : 1f;
    }

    public void PauseAndLaunch(float duration, Vector2 velocity, Action onResume)
    {
        StartCoroutine(HitPauseRoutine(duration, velocity, onResume));
    }

    private IEnumerator HitPauseRoutine(float duration, Vector2 velocity, Action onResume)
    {
        Rigid.linearVelocity = Vector2.zero;
        Rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        yield return new WaitForSeconds(duration);
        Rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        Rigid.linearVelocity = velocity;
        onResume?.Invoke();
    }

    private void OnDriftingHit(EnemyBehaviour enemy)
    {
        if (IsInvincible) return;
        _combo.Reset();
        ChangeState<PlayerStunState>();
    }

    public void OnSlingLaunched()
    {
        _propelGraceTimer = _propelGraceDuration;
    }

    // ========= ... =========

    public void ApplyGravity(float deltaTime)
    {
        var vel = Rigid.linearVelocity;
        vel.y -= SlingBehaviour.Config.slingGravity * deltaTime;
        Rigid.linearVelocity = vel;
    }

    private void UpdatePropelState(float deltaTime)
    {
        _propelGraceTimer -= deltaTime;

        if (_propelGraceTimer > 0f)
        {
            _isPropelled = true;
        }
        else
        {
            var sqr = Rigid.linearVelocity.sqrMagnitude;
            if (sqr > _propelHighThresholdSqr) _isPropelled = true;
            if (sqr < _propelLowThresholdSqr) _isPropelled = false;
        }

#if UNITY_EDITOR
        SpriteRenderer.color = _isPropelled ? Color.red : Color.gray;
#endif
    }

    // ========= ... =========

    public new void SetFacingDir(bool facingRight)
    {
        base.SetFacingDir(facingRight);

        _headbandTrail?.SetFacingDir(facingRight);
    }

    public void PlayPlayerAnimation(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.Idle: PlayAnimation(IDLE); break;
            case PlayerAnimationState.Roll: PlayAnimation(ROLL); break;
            case PlayerAnimationState.Wall: PlayAnimation(WALL); break;
        }
    }

    public void PlayHitEffect()
    {
        _hitColorSequence?.Kill();
        Material.SetFloat("_Amount", HIT_COLOR_MAX);
        var outColor = DOVirtual.Float(HIT_COLOR_MAX, HIT_COLOR_MIN, HIT_DURATION, v => Material.SetFloat("_Amount", v));
        _hitColorSequence = DOTween.Sequence().Append(outColor).OnComplete(() => _hitColorSequence = null);
    }

    // ========= ... =========

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead) return;

        if (_fsm.CurrState is PlayerAirState airState)
            airState.OnCollisionEnter(this, collision);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (IsDead) return;
        if (!collider.TryGetComponent<EnemyBehaviour>(out var enemy) || enemy.IsDead) return;

        if (_isPropelled) OnPropelledHit(enemy);
        else OnDriftingHit(enemy);
    }
}
