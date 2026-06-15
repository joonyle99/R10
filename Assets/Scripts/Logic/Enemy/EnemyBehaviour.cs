using System;
using UnityEngine;
using JoonyleGameDevKit;
using System.Collections;

public abstract class EnemyBehaviour : CombatEntity
{
    [SerializeField] private float _hitInvincibleDuration = 0.1f;

    private PlayerBehaviour _player;
    public PlayerBehaviour Player => _player;

    private EyeTracker _eyeTreacker;

    public void Initialize(PlayerBehaviour player, Action<int> onDamaged, Action onDead)
    {
        InitCombatEntity(onDamaged, onDead);

        _player = player;

        _eyeTreacker = GetComponentInChildren<EyeTracker>();
        _eyeTreacker.Initialize(player.transform);

        OnInitialize();
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead) return;

        base.Tick(deltaTime);

        _eyeTreacker?.Tick(deltaTime);
    }

    // ============ ... ============

    protected abstract void OnInitialize();

    protected override void OnDamaged(int damage, Vector2 sourcePos)
    {
        base.OnDamaged(damage, sourcePos);
        StartCoroutine(HitInvincibleRoutine());
    }

    private IEnumerator HitInvincibleRoutine()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(_hitInvincibleDuration);
        IsInvincible = false;
    }
}
