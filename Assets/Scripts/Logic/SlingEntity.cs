using System;
using UnityEngine;

public abstract class SlingEntity : CombatEntity
{
    public SlingBehaviour SlingBehaviour { get; protected set; }

    protected void InitSlingEntity(Action<int> onDamaged, Action onDead, LayerMask groundLayer)
    {
        InitCombatEntity(onDamaged, onDead);

        SlingBehaviour = GetComponentInChildren<SlingBehaviour>();
        SlingBehaviour.Initialize(Rigid, groundLayer);
        SlingBehaviour.SetActiveSling(true);
    }
}
