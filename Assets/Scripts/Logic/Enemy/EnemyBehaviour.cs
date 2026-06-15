using System;
using UnityEngine;
using JoonyleGameDevKit;

public abstract class EnemyBehaviour : CombatEntity
{
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
}
