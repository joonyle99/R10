using System;
using UnityEngine;

public abstract class EffectBase : MonoBehaviour
{
    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;

    private Action _onRelease;

    public void Play(Vector3 worldPosition, bool flipX = false)
    {
        if (_isPlaying) return;
        _isPlaying = true;

        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;
        var scale = transform.localScale;
        transform.localScale = new Vector3(flipX ? -1f * scale.x : scale.x, scale.y, scale.z);
        OnPlay();
    }

    public void Stop()
    {
        if (!_isPlaying) return;
        _isPlaying = false;

        OnStop();
        _onRelease?.Invoke();
    }

    protected abstract void OnPlay();
    protected abstract void OnStop();

    public void SetReleaseAction(Action onRelease) => _onRelease = onRelease;

    public void OnComplete() => Stop(); // VFX 완료 시점에 호출 (Animation Event, OnParticleSystemStopped 등)
}
