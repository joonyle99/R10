using UnityEngine;

public class EyeTracker : MonoBehaviour
{
    [SerializeField] private Transform _pupil;
    [SerializeField] private float _minX = -0.12f;
    [SerializeField] private float _maxX = 0.12f;
    [SerializeField] private float _minY = -0.06f;
    [SerializeField] private float _maxY = 0.06f;
    [SerializeField] private float _range = 5f;
    [SerializeField] private float _followSpeed = 12f;

    private Transform _target;
    private Vector3 _home;

    public void Initialize(Transform target)
    {
        _target = target;

        _home = _pupil.localPosition;
    }

    public void Tick(float deltaTime)
    {
        if (_target == null) return;

        // world 기준 방향
        var dir = _target.position - transform.position;
        var t = Mathf.Clamp01(dir.magnitude / _range); // 멀수록 끝까지

        var n = dir.normalized;
        float tx = n.x * t;
        float ty = n.y * t;
        var worldOffset = new Vector2(
            tx >= 0f ? tx * _maxX : -tx * _minX,
            ty >= 0f ? ty * _maxY : -ty * _minY
        );

        // flip 보정: 부모가 뒤집혔으면 x 부호 되돌림
        var flipSign = Mathf.Sign(transform.lossyScale.x);
        var want = _home + new Vector3(worldOffset.x * flipSign, worldOffset.y, 0f);

        _pupil.localPosition = Vector3.Lerp(_pupil.localPosition, want, _followSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (_pupil == null) return;

        var center = _pupil.parent != null
            ? _pupil.parent.TransformPoint(_home)
            : _pupil.position;
        Gizmos.color = Color.cyan;

        var bl = center + new Vector3(_minX, _minY);
        var br = center + new Vector3(_maxX, _minY);
        var tr = center + new Vector3(_maxX, _maxY);
        var tl = center + new Vector3(_minX, _maxY);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}