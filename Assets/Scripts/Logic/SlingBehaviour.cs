using System;
using UnityEngine;
using System.Collections.Generic;

public class SlingBehaviour : MonoBehaviour
{
    [SerializeField] private SlingConfig _config;
    public SlingConfig Config => _config;

    [SerializeField] private SpriteRenderer _bounceMarkerPrefab;

    private Rigidbody2D _rigid;
    public Rigidbody2D Rigid => _rigid;
    private TrajectorySolver _solver;
    private LineRenderer _line;
    private SpriteRenderer[] _bounceMarkers;

    private bool _isActiveSling;
    public bool IsActiveSling => _isActiveSling;

    private bool _isPendingShot; // Shoot 직후 한 번만 true — AirState가 "발사 진입"인지 "그냥 낙하"인지 구분하는 용도
    public Vector2 LastShotDir { get; private set; }

    // 최대 차지는 런타임 상태 — config의 baseCharges로 시작, 업그레이드로 증가 (SO에는 다시 쓰지 않는다)
    public int TotalCharges { get; private set; }
    public int CurrCharges { get; private set; }
    public bool HasCharge => CurrCharges > 0;
    public event Action<int, int> OnChargesChanged; // (curr, max)

    // 바운스마다 LineRenderer를 분리해 miter join 왜곡을 방지
    private LineRenderer[] _segmentLines;

    public void Initialize(Rigidbody2D rigid, LayerMask groundLayer)
    {
        _rigid = rigid;

        _solver = new TrajectorySolver(_config, groundLayer);

        _line = GetComponentInChildren<LineRenderer>();
        _line.textureMode = LineTextureMode.Tile;
        _line.useWorldSpace = true;
        _line.enabled = false;

        // 세그먼트 LineRenderer 풀: 바운스 수만큼 추가 세그먼트 필요 (총 maxBounces + 1개)
        _segmentLines = new LineRenderer[_config.maxBounces + 1];
        _segmentLines[0] = _line;
        for (int i = 1; i < _segmentLines.Length; i++)
        {
            var go = new GameObject($"Aim Line{i}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            // 원본 LineRenderer 설정 복사
            lr.textureMode = _line.textureMode;
            lr.useWorldSpace = _line.useWorldSpace;
            lr.sharedMaterial = _line.sharedMaterial;
            lr.widthCurve = _line.widthCurve;
            lr.widthMultiplier = _line.widthMultiplier;
            lr.colorGradient = _line.colorGradient;
            lr.shadowCastingMode = _line.shadowCastingMode;
            lr.sortingLayerID = _line.sortingLayerID;
            lr.sortingOrder = _line.sortingOrder;
            lr.enabled = false;
            _segmentLines[i] = lr;
        }

        _bounceMarkers = new SpriteRenderer[_config.maxBounces];
        for (int i = 0; i < _bounceMarkers.Length; i++)
        {
            _bounceMarkers[i] = Instantiate(_bounceMarkerPrefab, transform);
            _bounceMarkers[i].enabled = false;
        }

        TotalCharges = _config.baseCharges;
        CurrCharges = TotalCharges;
    }

    public void SetActiveSling(bool active) => _isActiveSling = active;

    // ============ ... ============

    public void ShowTrajectory(Vector2 dragOffset)
    {
        var slingDir = (-1) * dragOffset.normalized;

        // 조준선 원점은 물리 위치(_rigid.position)가 아니라 보간된 렌더 위치를 쓴다.
        // 물리 위치는 물리 스텝에서만 갱신돼서, 슬로우(aimTimeScale) 중 낙하하며 조준하면 선이 한 박자 늦게 따라온다.
        var origin = (Vector2)_rigid.transform.position;

        // 잠시 주석처리
        // if (SlingSimulator.IsGroundShot(slingDir, _config))
        // {
        //     ShowGroundShotLine(slingDir);
        //     return;
        // }

        var slingResult = _solver.Solve(origin, slingDir);

        // 바운스 지점을 기준으로 Points를 구간별로 나눠 별도 LineRenderer에 할당
        // → 구간 간 miter join이 생기지 않아 꺾임 부분 텍스처 왜곡이 사라짐
        {
            var allPoints = slingResult.Points;
            var bounceSet = new HashSet<Vector2>(slingResult.BouncePoints);

            int segIdx = 0;
            int start = 0;

            for (int i = 0; i <= allPoints.Count; i++)
            {
                bool isBounce = i < allPoints.Count && bounceSet.Contains(allPoints[i]) && i != 0;
                bool isEnd = i == allPoints.Count;

                if ((isBounce || isEnd) && segIdx < _segmentLines.Length)
                {
                    int count = i - start + (isBounce ? 1 : 0); // 바운스 점은 이 세그먼트에 포함
                    var lr = _segmentLines[segIdx];
                    lr.positionCount = count;
                    for (int j = 0; j < count; j++)
                        lr.SetPosition(j, allPoints[start + j]);
                    lr.enabled = true;

                    if (isBounce)
                        start = i; // 다음 세그먼트는 바운스 점부터 시작 (공유)
                    segIdx++;
                }
            }

            // 남은 세그먼트 비활성화
            for (int i = segIdx; i < _segmentLines.Length; i++)
            {
                _segmentLines[i].positionCount = 0;
                _segmentLines[i].enabled = false;
            }
        }

        {
            for (int i = 0; i < _bounceMarkers.Length; i++)
            {
                if (i < slingResult.BouncePoints.Count)
                {
                    _bounceMarkers[i].transform.position = slingResult.BouncePoints[i];
                    _bounceMarkers[i].enabled = true;
                }
                else
                {
                    _bounceMarkers[i].enabled = false;
                }
            }
        }
    }

    // 땅샷 조준선: 포물선 대신 짧은 고정 길이 직선
    private void ShowGroundShotLine(Vector2 slingDir)
    {
        var origin = (Vector2)_rigid.transform.position;

        _line.positionCount = 2;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, origin + slingDir * _config.groundShotAimLineLength);
        _line.enabled = true;

        foreach (var marker in _bounceMarkers)
            marker.enabled = false;
    }

    public void HideTrajectory()
    {
        foreach (var lr in _segmentLines)
            lr.enabled = false;
        foreach (var marker in _bounceMarkers)
            marker.enabled = false;
    }

    public void ShootSling(Vector2 dragOffset, bool consumeCharge)
    {
        _isPendingShot = true;

        var shotDir = (-1) * dragOffset.normalized;
        LastShotDir = shotDir;

        if (consumeCharge)
        {
            CurrCharges = Mathf.Max(0, CurrCharges - 1);
            OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
        }
    }

    public void RestoreCharges()
    {
        if (CurrCharges == TotalCharges) return;

        CurrCharges = TotalCharges;
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    public void AddCharge(int amount = 1)
    {
        if (CurrCharges >= TotalCharges) return;

        CurrCharges = Mathf.Min(TotalCharges, CurrCharges + amount);
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    // 최대 차지 업그레이드: 늘어난 칸은 즉시 채워서 획득이 바로 체감되게 한다
    public void IncreaseTotalCharges(int amount = 1)
    {
        TotalCharges += amount;
        CurrCharges = Mathf.Min(CurrCharges + amount, TotalCharges);
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    public bool ConsumeSling()
    {
        if (!_isPendingShot) return false;
        _isPendingShot = false;
        return true;
    }
}
