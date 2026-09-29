using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunningAttemptData
{
    public int sequence;
    public string direction;
    public float requestedDistanceMeters;
    public float elapsedSeconds;
    public float travelledDistanceMeters;
    public Vector3 targetPosition;
}

[Serializable]
public class RunningMetricsData
{
    public int targetsReached;
    public float averageResponseSeconds;
    public float bestResponseSeconds;
    public float totalTravelledMeters;
    public float longestRouteMeters;
    public List<RunningAttemptData> attempts = new List<RunningAttemptData>();
}

[Serializable]
public class RunningExerciseResult
{
    public string exerciseType = "running_direction_change";
    public int sets;
    public float setDurationSeconds;
    public float restDurationSeconds;
    public RunningAreaData area;
    public RunningMetricsData metrics;
}

public class RunningExerciseStats : MonoBehaviour
{
    private readonly List<RunningAttemptData> attempts = new List<RunningAttemptData>();
    private float totalResponseTime;
    private float bestResponseTime = float.MaxValue;
    private float totalTravelledDistance;
    private float longestRoute;

    public int TargetsReached => attempts.Count;

    public void ResetStats()
    {
        attempts.Clear();
        totalResponseTime = 0f;
        bestResponseTime = float.MaxValue;
        totalTravelledDistance = 0f;
        longestRoute = 0f;
    }

    public void RegisterReachedTarget(string direction, float requestedDistance, float elapsed, float travelled, Vector3 target)
    {
        attempts.Add(new RunningAttemptData
        {
            sequence = attempts.Count + 1,
            direction = direction,
            requestedDistanceMeters = requestedDistance,
            elapsedSeconds = elapsed,
            travelledDistanceMeters = travelled,
            targetPosition = target
        });

        totalResponseTime += elapsed;
        bestResponseTime = Mathf.Min(bestResponseTime, elapsed);
        totalTravelledDistance += travelled;
        longestRoute = Mathf.Max(longestRoute, requestedDistance);
    }

    public RunningExerciseResult BuildResult(int sets, float setDuration, float restDuration, RunningAreaData area)
    {
        return new RunningExerciseResult
        {
            sets = sets,
            setDurationSeconds = setDuration,
            restDurationSeconds = restDuration,
            area = area,
            metrics = new RunningMetricsData
            {
                targetsReached = attempts.Count,
                averageResponseSeconds = attempts.Count == 0 ? 0f : totalResponseTime / attempts.Count,
                bestResponseSeconds = attempts.Count == 0 ? 0f : bestResponseTime,
                totalTravelledMeters = totalTravelledDistance,
                longestRouteMeters = longestRoute,
                attempts = new List<RunningAttemptData>(attempts)
            }
        };
    }
}
