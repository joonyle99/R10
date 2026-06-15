using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Headband : MonoBehaviour
{
    [Header("Cloth")]
    [SerializeField] private int _segmentCount = 30;
    [SerializeField] private float _segmentSpacing = 0.3f;
    [SerializeField] private int _solverIter = 5; // 높을수록 뻣뻣함 / 낮을수록 늘어남
    [SerializeField] private float _damping = 0.8f; // 관성 길이 (높을수록 오래 출렁임)
    [SerializeField] private Vector2 _gravity = new(0f, -2f);
    [SerializeField] private Vector2 _wind = new(-3f, 1.5f);
    [SerializeField] private float _swayAmplitude = 0.5f; // 흔들림 세기
    [SerializeField] private float _swayPhaseStep = 0.4f; // 세그먼트당 위상 간격 (클수록 파형이 촘촘)
    [SerializeField] private float _turbulenceAmp = 0.2f; // 고주파 난류 세기
    [SerializeField] private float _tipAmplifyExp = 1.5f; // 끝쪽으로 갈수록 진폭 증가 지수

    private Vector2 _currWind;
    private LineRenderer _line;
    private Vector2[] _pos, _prevPos;

    private void LateUpdate()
    {
        Simulate();

        for (int i = 0; i < _segmentCount; i++)
            _line.SetPosition(i, _pos[i]);
    }

    public void Initialize()
    {
        _currWind = _wind;
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = _segmentCount;

        _pos = new Vector2[_segmentCount];
        _prevPos = new Vector2[_segmentCount];

        var start = (Vector2)transform.position;
        for (int i = 0; i < _segmentCount; i++)
            _pos[i] = _prevPos[i] = start + Vector2.down * (_segmentSpacing * i);
    }

    private void Simulate()
    {
        var dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 60f); // 스파이크 방어
        var dt2 = dt * dt;
        var baseForce = (_gravity + _currWind) * dt2;
        var t = Time.unscaledTime;

        // 1. 적분 — (pos - prevPos)가 관성(속도) 역할
        for (int i = 1; i < _segmentCount; i++)
        {
            var vel = (_pos[i] - _prevPos[i]) * _damping;
            _prevPos[i] = _pos[i];
            _pos[i] += vel + baseForce;
        }

        // 2. 앵커 고정
        _pos[0] = transform.position;

        // 3. 길이 제약 반복
        for (int k = 0; k < _solverIter; k++)
        {
            for (int i = 0; i < _segmentCount - 1; i++)
            {
                var delta = _pos[i + 1] - _pos[i];
                var dist = delta.magnitude;
                if (dist < 1e-5f) continue;
                var diff = (dist - _segmentSpacing) / dist;
                if (i != 0) _pos[i] += delta * (0.5f * diff);
                _pos[i + 1]         -= delta * (0.5f * diff);
            }
        }

        // // 4. 바람 흔들림 — 직접 위치 오프셋 (댐핑 영향 없이 항상 보임)
        // for (int i = 1; i < _segmentCount; i++)
        // {
        //     var tip = Mathf.Pow((float)i / (_segmentCount - 1), _tipAmplifyExp);
        //     var phase = i * _swayPhaseStep;
        //     var sway = Mathf.Sin(t * 2.1f + phase) * _swayAmplitude
        //              + Mathf.Sin(t * 4.7f + phase * 1.3f) * _turbulenceAmp;
        //     _pos[i].x += sway * tip;
        // }
    }

    public void SetFacingDir(bool facingRight)
    {
        _currWind = new Vector2(Mathf.Abs(_wind.x) * (facingRight ? -1f : 1f), _wind.y);
    }
}
