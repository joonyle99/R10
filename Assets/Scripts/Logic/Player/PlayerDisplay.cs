using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class PlayerDisplay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _chargePrefab;
    [SerializeField] private SpriteRenderer _noChargeIcon;

    [SerializeField] private float _chargeSpacing = 0.3f;
    [SerializeField] private float _chargeYOffset = 0f;
    [SerializeField] private float _noChargeIconFadeDuration = 0.25f;

    private SlingBehaviour _slingBehaviour;
    private Tween _noChargeIconTween;
    private bool _isAimPreview;
    private List<SpriteRenderer> _charges = new();

    private void OnDestroy()
    {
        if (_slingBehaviour != null)
            _slingBehaviour.OnChargesChanged -= OnChargesChanged;
    }

    public void Initialize(SlingBehaviour slingBehaviour)
    {
        _slingBehaviour = slingBehaviour;
        _slingBehaviour.OnChargesChanged += OnChargesChanged;

        var color = _noChargeIcon.color;
        color.a = 0f;
        _noChargeIcon.color = color;

        SpawnChargeIcons(slingBehaviour.TotalCharges);
        RefreshDisplay();
    }

    private void SpawnChargeIcons(int total)
    {
        foreach (var icon in _charges)
            Destroy(icon.gameObject);
        _charges.Clear();

        for (int i = 0; i < total; i++)
            _charges.Add(Instantiate(_chargePrefab, transform));
    }

    // ====================================

    private void OnChargesChanged(int curr, int max)
    {
        if (max != _charges.Count)
            SpawnChargeIcons(max);
        RefreshDisplay();
    }

    public void BeginAimPreview()
    {
        _isAimPreview = true;
        RefreshDisplay();
    }

    public void EndAimPreview()
    {
        _isAimPreview = false;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        int curr = _slingBehaviour.CurrCharges;
        int displayCurr = _isAimPreview ? Mathf.Max(0, curr - 1) : curr;

        float offset = (displayCurr - 1) * _chargeSpacing * 0.5f;
        for (int i = 0; i < _charges.Count; i++)
        {
            bool visible = i < displayCurr;
            _charges[i].gameObject.SetActive(visible);
            if (visible)
                _charges[i].transform.localPosition = new Vector3(i * _chargeSpacing - offset, _chargeYOffset);
        }
    }

    public void ShowNoChargeEffect()
    {
        StartCoroutine(NoChargePauseRoutine());

        _noChargeIconTween?.Kill();
        var color = _noChargeIcon.color;
        color.a = 1f;
        _noChargeIcon.color = color;
        _noChargeIconTween = _noChargeIcon
            .DOFade(0f, _noChargeIconFadeDuration)
            .SetDelay(_slingBehaviour.Config.noChargePauseDuration)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private IEnumerator NoChargePauseRoutine()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(_slingBehaviour.Config.noChargePauseDuration);
        Time.timeScale = 1f;
    }
}
