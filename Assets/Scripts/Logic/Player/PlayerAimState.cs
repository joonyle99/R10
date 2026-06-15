using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerAimState : StateBase<PlayerBehaviour>
{
    private Vector2 _lastDragOffset;

    public override void Enter(PlayerBehaviour owner)
    {
        owner.PlayerDisplay.BeginAimPreview();
        if (owner.FSM.PrevState is PlayerGroundState)
            owner.SquashStretch.HoldSoftSquash();
    }

    public override void Exit(PlayerBehaviour owner)
    {
        owner.SlingBehaviour.HideTrajectory();
        owner.PlayerDisplay.EndAimPreview();
        if (owner.FSM.PrevState is PlayerGroundState)
            owner.SquashStretch.Restore();
    }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        var pointerInput = owner.PointerInput;
        var cam = owner.CameraController.MainCamera;
        var worldUnitsPerPixel = cam.orthographicSize * 2f / Screen.height;

        if (pointerInput.IsDragging)
        {
            _lastDragOffset = pointerInput.GetScreenDragDelta * worldUnitsPerPixel;
            if (!Mathf.Approximately(_lastDragOffset.x, 0f))
                owner.SetFacingDir(_lastDragOffset.x < 0f); // 드래그 반대 방향 = 발사 방향
        }

        if (pointerInput.JustReleased)
        {
            if (!pointerInput.JustTapped)
            {
                owner.SlingBehaviour.ShootSling(_lastDragOffset, consumeCharge: true);
                owner.OnSlingLaunched();
                owner.ChangeState<PlayerAirState>();
            }
            else
            {
                CancelAim(owner);
            }

            return;
        }
        else if (!pointerInput.IsDragging)
        {
            CancelAim(owner);
            owner.SquashStretch.Restore();
            return;
        }

        owner.SlingBehaviour.ShowTrajectory(_lastDragOffset);
    }

    private void CancelAim(PlayerBehaviour owner)
    {
        if (owner.FSM.PrevState is PlayerAirState)
        {
            owner.ChangeState<PlayerAirState>();
        }
        else
        {
            owner.ChangeState<PlayerGroundState>();
        }
    }
}
