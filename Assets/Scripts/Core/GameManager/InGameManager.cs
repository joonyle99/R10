using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InGameManager : MonoBehaviour
{
    private PointerInput _pointerInput;

    private GameStateController<InGameState> _gameStateController;
    private CameraController _cameraController;
    private UIController _uiController;
    private LevelManager _levelManager;

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        _gameStateController.OnStateChanged -= OnStateChanged;
        _gameStateController.OnStateChanged -= _uiController.OnStateChanged;
        _gameStateController.OnStateChanged -= _cameraController.OnStateChanged;
        if (SoundManager.Instance != null) _gameStateController.OnStateChanged -= SoundManager.Instance.OnStateChanged;

        _pointerInput.Dispose();
    }

    private void FixedUpdate()
    {
        var fixedDeltaTime = Time.fixedDeltaTime;
        _levelManager?.FixedTick(fixedDeltaTime);
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        _pointerInput?.Tick(deltaTime);
        _levelManager?.Tick(deltaTime);
    }

    private void Initialize()
    {
        _gameStateController = new GameStateController<InGameState>();
        _cameraController = FindFirstObjectByType<CameraController>();
        _uiController = FindFirstObjectByType<UIController>();
        _levelManager = FindFirstObjectByType<LevelManager>();
        _cameraController.Initialize();
        _pointerInput = new PointerInput(_cameraController.MainCamera);
        _uiController.Initialize();
        _levelManager.Initialize(_cameraController, _pointerInput, Failure, Success);

        if (SoundManager.Instance != null) _gameStateController.OnStateChanged += SoundManager.Instance.OnStateChanged;
        _gameStateController.OnStateChanged += _cameraController.OnStateChanged;
        _gameStateController.OnStateChanged += _uiController.OnStateChanged;
        _gameStateController.OnStateChanged += OnStateChanged;

        _gameStateController.ChangeState(InGameState.Play);
    }

    // ========== 상태 전환 ==========

    private void OnStateChanged(InGameState prev, InGameState curr)
    {

    }

    private void Failure()
    {
        _gameStateController.ChangeState(InGameState.Failure);
    }
    
    private void Success()
    {
        _gameStateController.ChangeState(InGameState.Success);
    }
}
