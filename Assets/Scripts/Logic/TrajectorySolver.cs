using System;
using UnityEngine;
using System.Collections.Generic;

public struct SlingResult
{
    public List<Vector2> Points; // 선 렌더러용 전체 점
    public List<Vector2> BouncePoints;
    public int BounceCount;
    public float TotalDistance;
}

public class TrajectorySolver
{
    private readonly SlingConfig _config;
    private readonly LayerMask _groundLayer;

    public TrajectorySolver(SlingConfig config, LayerMask groundLayer)
    {
        _config = config;
        _groundLayer = groundLayer;
    }

    public SlingResult Solve(Vector2 origin, Vector2 dir)
    {
        var points = new List<Vector2> { origin };
        var bouncePoints = new List<Vector2>();
        var simulation = SlingState.Create(origin, dir, _config);

        while (simulation.Remaining > 0f)
        {
            var evt = SlingSimulator.Tick(ref simulation, _config, _groundLayer, Time.fixedDeltaTime);
            points.Add(simulation.Position);

            if (evt == SlingEvent.Bounce)
                bouncePoints.Add(simulation.Position);
        }

        return new SlingResult
        {
            Points = points,
            BouncePoints = bouncePoints,
            BounceCount = bouncePoints.Count,
            TotalDistance = simulation.Total,
        };
    }
}
