using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunningAttemptData
{
    public int sequence, setIndex;
    public string direction, outcome;
    public float requestedAngleDegrees, achievedAngleDegrees, toleranceDegrees;
    public float elapsedSeconds, movementOnsetSeconds;
}
[Serializable]
public class RunningMetricsData
{
    public int cuesIssued, completedTurns, misses, interruptedTurns;
    public float averageResponseSeconds, bestResponseSeconds;
    public List<RunningAttemptData> attempts = new List<RunningAttemptData>();
}
[Serializable]
public class RunningExerciseResult
{
    public string exerciseType = "running_direction_change";
    public string measurementMode = "gyroscope_turn_response";
    public int sets;
    public float setDurationSeconds, restDurationSeconds;
    public RunningMetricsData metrics;
}
public class RunningExerciseStats : MonoBehaviour
{
    private readonly List<RunningAttemptData> attempts = new List<RunningAttemptData>();
    private float totalTime, bestTime;
    public int CuesIssued { get; private set; }
    public int CompletedTurns { get; private set; }
    public int Misses { get; private set; }
    public int InterruptedTurns { get; private set; }
    public void ResetStats()
    { attempts.Clear(); totalTime = 0; bestTime = float.MaxValue; CuesIssued = CompletedTurns = Misses = InterruptedTurns = 0; }
    public void CueIssued() => CuesIssued++;
    public void Register(string direction, string outcome, int set, float angle, float tolerance, TurnResponseEvaluator evaluation)
    {
        attempts.Add(new RunningAttemptData { sequence = attempts.Count + 1, setIndex = set,
            direction = direction, outcome = outcome, requestedAngleDegrees = angle,
            achievedAngleDegrees = (float)evaluation.ProgressDegrees, toleranceDegrees = tolerance,
            elapsedSeconds = (float)evaluation.ElapsedSeconds, movementOnsetSeconds = (float)evaluation.MovementOnsetSeconds });
        if (outcome == "completed")
        { CompletedTurns++; totalTime += (float)evaluation.ElapsedSeconds; bestTime = Mathf.Min(bestTime, (float)evaluation.ElapsedSeconds); }
        else if (outcome == "timeout") Misses++;
        else InterruptedTurns++;
    }
    public RunningExerciseResult BuildResult(int sets, float seconds, float rest)
        => new RunningExerciseResult { sets = sets, setDurationSeconds = seconds, restDurationSeconds = rest,
            metrics = new RunningMetricsData { cuesIssued = CuesIssued, completedTurns = CompletedTurns, misses = Misses,
                interruptedTurns = InterruptedTurns, averageResponseSeconds = CompletedTurns == 0 ? 0 : totalTime / CompletedTurns,
                bestResponseSeconds = CompletedTurns == 0 ? 0 : bestTime, attempts = new List<RunningAttemptData>(attempts) } };
}
