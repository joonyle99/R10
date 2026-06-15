using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointerInput : IPointerInput, IDisposable
{
    private readonly InputAction _pressAction;
    private readonly InputAction _positionAction;

    private bool _isDragStarted;
    private bool _dragStartFired;
    private Vector2 _pressStartPos;
    private Vector2 _pressStartWorldPos;
    private Vector2 _prevScreenPos;
    private Vector2 _prevWorldPos;

    private readonly Camera _camera;

    public PointerInput(Camera camera)
    {
        _camera = camera;

        _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        _positionAction = new InputAction("Position", InputActionType.Value, "<Pointer>/position");

        _pressAction.Enable();
        _positionAction.Enable();
    }

    public Vector2 GetScreenPos => _positionAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 GetScreenPosDelta { get; private set; }
    public Vector2 GetScreenDragDelta => IsDragging ? GetScreenPos - _pressStartPos : Vector2.zero;
    public Vector2 GetWorldPos => ScreenToWorldPos(GetScreenPos);
    public Vector2 GetWorldPosDelta { get; private set; }
    public Vector2 GetPressStartWorldPos => _pressStartWorldPos;
    public float DragThresholdScreenRadius { get; } = 70f;

    public bool JustPressed { get; private set; }
    public bool JustReleased { get; private set; }
    public bool JustTapped { get; private set; }
    public bool IsPressed { get; private set; }
    public bool IsDragging { get; private set; }

    public event Action<Vector2> OnPress;
    public event Action<Vector2> OnRelease;
    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    public void Tick(float deltaTime)
    {
        var currScreenPos = _positionAction.ReadValue<Vector2>();
        var currWorldPos = ScreenToWorldPos(currScreenPos);

        JustPressed = _pressAction.WasPressedThisFrame();
        JustReleased = _pressAction.WasReleasedThisFrame();

        if (JustPressed)
        {
            _prevWorldPos = currWorldPos;
            _prevScreenPos = currScreenPos;
            _pressStartPos = currScreenPos;
            _pressStartWorldPos = currWorldPos;
            _dragStartFired = false;
            _isDragStarted = false;
            OnPress?.Invoke(currWorldPos);
        }

        IsPressed = _pressAction.IsPressed();

        // 누른 채로 매 프레임 거리 재검사 — 범위 안으로 돌아오면 드래그 취소
        if (IsPressed)
        {
            var sqrDistance = (currScreenPos - _pressStartPos).sqrMagnitude;
            var sqrThreshold = DragThresholdScreenRadius * DragThresholdScreenRadius;
            _isDragStarted = sqrDistance > sqrThreshold;
        }

        IsDragging = IsPressed && _isDragStarted;

        GetScreenPosDelta = IsDragging ? currScreenPos - _prevScreenPos : Vector2.zero;
        GetWorldPosDelta = IsDragging ? currWorldPos - _prevWorldPos : Vector2.zero;

        // 드래그 시작 이벤트 (최초 1회)
        if (IsDragging && !_dragStartFired)
        {
            _dragStartFired = true;

            OnDragStart?.Invoke(currWorldPos);
        }

        // 드래그 중 매 프레임 델타 발행
        if (IsDragging)
        {
            OnDrag?.Invoke(GetWorldPosDelta);
        }

        // 릴리즈 처리
        if (JustReleased)
        {
            JustTapped = !_isDragStarted;
            
            OnRelease?.Invoke(currWorldPos);

            if (JustTapped)
            {
                OnTap?.Invoke(currWorldPos);
            }
            else
            {
                OnDragEnd?.Invoke(currWorldPos);
            }
        }
        else
        {
            JustTapped = false;
        }

        _prevScreenPos = currScreenPos;
        _prevWorldPos = currWorldPos;

// #if UNITY_EDITOR
//         if (JustPressed)  Debug.Log($"[Input] JustPressed  | screen: {currScreenPos}");
//         if (JustTapped)   Debug.Log($"[Input] JustTapped   | world: {currWorldPos}");
//         if (JustReleased && !JustTapped) Debug.Log($"[Input] JustReleased (drag end) | world: {currWorldPos}");
//         if (IsDragging && _dragStartFired && GetWorldPosDelta != Vector2.zero)
//             Debug.Log($"[Input] Dragging | worldDelta: {GetWorldPosDelta}  screenDelta: {GetScreenPosDelta}");
// #endif
    }

    private Vector2 ScreenToWorldPos(Vector2 screenPos)
    {
        if (_camera == null) return Vector2.zero;
        return _camera.ScreenToWorldPoint(screenPos);
    }

    public void Dispose()
    {
        _pressAction.Disable();
        _positionAction.Disable();

        _pressAction.Dispose();
        _positionAction.Dispose();
    }
}
