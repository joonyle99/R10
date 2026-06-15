using UnityEngine;

public class PursuerBehaviour : MonoBehaviour
{
    [SerializeField] private float _riseSpeed = 3f;
    [SerializeField] private float _killOffsetY = 0f;

    private PlayerBehaviour _player;

    public void Initialize(PlayerBehaviour player)
    {
        _player = player;
    }

    public void Tick(float deltaTime)
    {
        if (_player.IsDead) return;

        transform.position += Vector3.up * (_riseSpeed * deltaTime);

        if (_player.transform.position.y <= transform.position.y + _killOffsetY)
            _player.TakeDamage(_player.MaxHp, transform.position);
    }
}
