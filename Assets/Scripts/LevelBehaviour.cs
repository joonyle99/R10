using System;
using UnityEngine;

public class LevelBehaviour : MonoBehaviour
{
    [SerializeField] private Transform _startPlatform;
    [SerializeField] private Transform _endPlatform;

    private PlayerBehaviour _player;
    public PlayerBehaviour Player => _player;
    private PursuerBehaviour _pursuer;
    private EnemyBehaviour[] _enemies;

    private Action _onSuccess;
    private bool _isPlayerReached;

    public void Initialize(CameraController cameraController, IPointerInput pointerInput, Action onFailure, Action onSuccess)
    {
        _onSuccess = onSuccess;
        _isPlayerReached = false;

        _player = GetComponentInChildren<PlayerBehaviour>();
        _player?.Initialize(cameraController, pointerInput, null, onFailure);
        _pursuer = GetComponentInChildren<PursuerBehaviour>();
        _pursuer?.Initialize(_player);
        _enemies = GetComponentsInChildren<EnemyBehaviour>();
        foreach (var enemy in _enemies)
            enemy?.Initialize(_player, null, null);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        _player?.FixedTick(fixedDeltaTime);
        foreach (var enemy in _enemies)
            enemy?.FixedTick(fixedDeltaTime);

        if (!_isPlayerReached
            && _player != null
            && _endPlatform != null
            && _player.PlatformerSensor.IsGrounded
            && _player.Rigid.position.y >= _endPlatform.position.y)
        {
            _isPlayerReached = true;
            _onSuccess?.Invoke();
        }
    }

    public void Tick(float deltaTime)
    {
        _player?.Tick(deltaTime);
        _pursuer?.Tick(deltaTime);
        foreach (var enemy in _enemies)
            enemy?.Tick(deltaTime);
    }
}
