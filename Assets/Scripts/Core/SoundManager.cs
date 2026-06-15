using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections.Generic;

public enum BgmType
{
    
}

public enum SfxType
{
    Temp_0 = 0,
    Temp_10 = 10,
    Temp_20 = 20,
    Temp_30 = 30,
    Temp_40 = 40,
}

[System.Serializable]
public struct BgmEntry
{
    public BgmType type;
    public AudioClip clip;
}

[System.Serializable]
public struct SfxEntry
{
    public SfxType type;
    public AudioClip clip;
}

public class SoundManager : Singleton<SoundManager>, IManager, IGameStateListener<OutGameState>, IGameStateListener<InGameState>
{
    public int Priority => 10;

    [SerializeField] private BgmEntry[] _bgmEntries;
    [SerializeField] private SfxEntry[] _sfxEntries;

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Space]
    
    [SerializeField] private float _bgmFadeDuration = 0.35f;

    private Dictionary<BgmType, BgmEntry> _bgmMap;
    private Dictionary<SfxType, SfxEntry> _sfxMap;

    private float _bgmVolume;
    private Tween _bgmFadeTween;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        _bgmFadeTween?.Kill();
    }

    public void Initialize()
    {
        _bgmMap = new Dictionary<BgmType, BgmEntry>();
        foreach (var entry in _bgmEntries)
            _bgmMap.TryAdd(entry.type, entry);

        _sfxMap = new Dictionary<SfxType, SfxEntry>();
        foreach (var entry in _sfxEntries)
            _sfxMap.TryAdd(entry.type, entry);

        _bgmVolume = _bgmSource.volume;
    }

    public void OnStateChanged(OutGameState prevState, OutGameState currState)
    {
        
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        
    }

    public void PlayBgm(BgmType type, float volume = -1f)
    {
        if (!_bgmMap.TryGetValue(type, out var entry) || entry.clip == null) return;
        if (_bgmSource.clip == entry.clip) return;

        if (volume >= 0f) _bgmVolume = volume;

        FadeBgmTo(0f);
        _bgmFadeTween.OnComplete(() =>
        {
            _bgmSource.clip = entry.clip;
            _bgmSource.Play();
            FadeBgmTo(_bgmVolume);
        });
    }

    public void StopBgm()
    {
        FadeBgmTo(0f);
        _bgmFadeTween.OnComplete(() =>
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        });
    }

    public void PlaySfx(SfxType type, float volume = 0.5f)
    {
        if (_sfxMap.TryGetValue(type, out var entry) && entry.clip != null)
        {
            _sfxSource.PlayOneShot(entry.clip, volume);
        }
    }

    private void FadeBgmTo(float targetVolume)
    {
        _bgmFadeTween?.Kill();
        _bgmFadeTween = _bgmSource.DOFade(targetVolume, _bgmFadeDuration).SetUpdate(true);
    }

    private void SetSfxPaused(bool paused)
    {
        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in sources)
        {
            if (source == _bgmSource) continue;
            if (paused) source.Pause();
            else source.UnPause();
        }
    }
}
