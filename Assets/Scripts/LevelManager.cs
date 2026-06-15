using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private CameraController _cameraController;
    private IPointerInput _pointerInput;
    private Action _onFailure;
    private Action _onSuccess;

    [SerializeField] private LevelData[] _levels;

    private int _index;
    private LevelBehaviour _currLv;
    public PlayerBehaviour Player => _currLv?.Player;

    public void Initialize(CameraController cameraController, IPointerInput pointerInput, Action onFailure, Action onSuccess)
    {
        _cameraController = cameraController;
        _pointerInput = pointerInput;
        _onFailure = onFailure;
        _onSuccess = onSuccess;

        LoadNext();
    }

    public void LoadNext()
    {
        if (_levels == null || _levels.Length == 0) return;
        if (_currLv != null) Destroy(_currLv.gameObject);
        var levelData = _levels[_index % _levels.Length];
        if (levelData == null) return;

        _currLv = Instantiate(levelData.levelPrefab, transform);
        _currLv.Initialize(_cameraController, _pointerInput, _onFailure, _onSuccess);

        _index++;
    }

    public void FixedTick(float deltaTime)
    {
        _currLv?.FixedTick(deltaTime);
    }

    public void Tick(float deltaTime)
    {
        _currLv?.Tick(deltaTime);
    }
}
