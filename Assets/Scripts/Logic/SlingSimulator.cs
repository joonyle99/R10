using UnityEngine;

public enum SlingEvent
{
    None,
    Bounce,
}

public struct SlingState
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Remaining;
    public float Total; // 누적 이동 거리
    public int Bounces;

    public static SlingState Create(Vector2 origin, Vector2 dir, SlingConfig config)
    {
        return new SlingState
        {
            Position = origin,
            Velocity = dir * config.slingSpeed,
            Remaining = config.slingTime,
        };
    }
}

public static class SlingSimulator
{
    public static SlingEvent Tick(ref SlingState state, SlingConfig config, LayerMask groundLayer, float deltaTime)
    {
        state.Velocity.y -= config.slingGravity * deltaTime;
        state.Remaining -= deltaTime;

        var step = state.Velocity * deltaTime;
        var stepDist = step.magnitude;

        var allHits = Physics2D.CircleCastAll(state.Position, config.circleCastRadius, step.normalized, stepDist, groundLayer);
        foreach (var h in allHits)
        {
            if (h.distance <= 0f) continue;

            state.Total += h.distance;
            state.Position = h.centroid;

            if (!IsWall(h.normal, config))
            {
                continue;
            }

            if (CanBounce(in state, config))
            {
                Bounce(ref state, h.normal, config);
                return SlingEvent.Bounce;
            }

            return SlingEvent.None;
        }

        state.Position += step;
        state.Total += stepDist;
        return SlingEvent.None;
    }

    // 법선이 수평에 가까우면 벽 (위쪽 법선만 바닥으로 분류 — 천장은 벽 취급)
    public static bool IsWall(Vector2 normal, SlingConfig config)
        => Vector2.Angle(normal, Vector2.up) > config.wallAngleThreshold;

    public static bool CanBounce(in SlingState state, SlingConfig config)
        => state.Total >= config.minDistance && state.Remaining > 0f && state.Bounces < config.maxBounces;

    public static void Bounce(ref SlingState state, Vector2 normal, SlingConfig config)
    {
        state.Bounces++;
        state.Remaining += config.bonusTime;
        state.Velocity = Kick(normal, config.kick);
    }

    // 땅샷: 조준 고도각(수평 기준, 아래쪽은 음수)이 임계값 이하면 포물선 대신 직선 발사로 취급
    public static bool IsGroundShot(Vector2 dir, SlingConfig config)
        => Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg <= config.groundShotMaxAngle;

    public static Vector2 Kick(Vector2 normal, SlingConfig.KickConfig kick)
    {
        var rad = kick.angle * Mathf.Deg2Rad;
        var away = Mathf.Sign(normal.x);
        return new Vector2(Mathf.Cos(rad) * away, Mathf.Sin(rad)) * kick.speed;
    }
}
