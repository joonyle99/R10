using UnityEngine;

public class EyeTracker : MonoBehaviour
{
    [SerializeField] private Transform _eye;
    [SerializeField] private PolygonCollider2D _boundary;
    [SerializeField] private float _sqrRange = 20f;
    [SerializeField] private float _speed = 12f;
    
    private Vector3 _originEyePos;

    private Transform _target;
    private float _lastParentFlipSign;

    public void Initialize(Transform target)
    {
        _target = target;

        _originEyePos = _eye.localPosition;
        _lastParentFlipSign = Mathf.Sign(_eye.parent.lossyScale.x);
    }

    public void Tick(float deltaTime)
    {
        if (_target == null || _boundary == null) return;

        // flip 감지: parent scale.x 부호 변화 시 localPos.x 즉시 미러링
        var parentFlipSign = Mathf.Sign(_eye.parent.lossyScale.x);
        if (parentFlipSign != _lastParentFlipSign)
        {
            var pos = _eye.localPosition;
            pos.x = -pos.x;
            _eye.localPosition = pos;
            _lastParentFlipSign = parentFlipSign;
        }

        var distVector = _target.position - transform.position;
        var ratio = Mathf.Clamp01(distVector.sqrMagnitude / _sqrRange);

        var originWorld = (Vector2)_eye.parent.TransformPoint(_originEyePos);
        var dir2D = new Vector2(distVector.x, distVector.y).normalized;
        var boundaryPoint = _boundary.ClosestPoint(originWorld + dir2D * 1000f);

        var targetWorld = Vector2.Lerp(originWorld, boundaryPoint, ratio);
        var targetLocal = _eye.parent.InverseTransformPoint(targetWorld);

        _eye.localPosition = Vector3.Lerp(_eye.localPosition, targetLocal, _speed * deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, Mathf.Sqrt(_sqrRange));

        if (_boundary == null) return;

        Gizmos.color = Color.cyan;
        var points = _boundary.points;
        for (int i = 0; i < points.Length; i++)
        {
            var a = _boundary.transform.TransformPoint(points[i]);
            var b = _boundary.transform.TransformPoint(points[(i + 1) % points.Length]);
            Gizmos.DrawLine(a, b);
        }
    }
}
