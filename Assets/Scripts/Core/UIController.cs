using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIController : MonoBehaviour, IGameStateListener<InGameState>
{
    private void OnDestroy()
    {
        
    }

    public void Initialize()
    {
        
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {

    }
}
