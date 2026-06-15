using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour, IGameStateListener<InGameState>
{
    // ======== 일반 카메라 ========

    [SerializeField] private Camera _uiCamera;
    public Camera UICamera => _uiCamera;

    private Camera _mainCamera;
    public Camera MainCamera => _mainCamera;

    // ======== ... ========

    private CinemachineBrain _brain;
    [SerializeField] private CinemachineImpulseSource _impulse;

    // ======== 카메라 크기 ========

    public float CameraWidth => CameraHeight * CameraAspect;
    public float CameraHeight => _mainCamera.orthographicSize * 2f;
    public float CameraAspect => (float)Screen.width / (float)Screen.height;

    // ======== Y축 추적 ========

    [SerializeField] private float _verticalOffset = 2f; // 양수일수록 플레이어가 화면 아래에 위치
    [SerializeField] private float _riseSpeed = 8f;  // 플레이어 상승 시 카메라 추적 속도
    [SerializeField] private float _fallSpeed = 3f;  // 플레이어 하강 시 카메라 추적 속도

    private Transform _followTarget;
    private float _fixedX;

    private void LateUpdate()
    {
        if (_followTarget == null) return;

        var targetY = _followTarget.position.y + _verticalOffset;
        var pos = transform.position;
        pos.x = _fixedX;
        float speed = targetY > pos.y ? _riseSpeed : _fallSpeed;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * speed);
        transform.position = pos;
    }

    public void Initialize()
    {
        _mainCamera = GetComponent<Camera>();
        _brain = _mainCamera.GetComponent<CinemachineBrain>();
        _fixedX = transform.position.x;
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        
    }

    // ======== ... ========

    public void SetOrthographicSize(float size)
    {
        // var lens = _idleCinemachine.Lens;
        // lens.OrthographicSize = size;
        // _idleCinemachine.Lens = lens;
        // _uiCamera.orthographicSize = size;
    }

    // ======== ... ========

    public void ActivateFollow(Transform target)
    {
        _followTarget = target;
    }

    public void DeactivateFollow()
    {
        _followTarget = null;
    }

    // ======== ... ========
    
    public void Shake(float force = 0.5f) => _impulse?.GenerateImpulse(force);
}
