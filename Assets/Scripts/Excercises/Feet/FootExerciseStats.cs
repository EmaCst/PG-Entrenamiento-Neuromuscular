using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FootAttemptData
{
    public int sequence;
    public int targetIndex;
    public string requiredFoot;
    public bool hit;
    public float reactionSeconds;
    public float confidence;
}

[Serializable]
public class FootMetricsData
{
    public int hits;
    public int misses;
    public float averageReactionSeconds;
    public float bestReactionSeconds;
    public List<FootAttemptData> attempts = new List<FootAttemptData>();
}

[Serializable]
public class FootExerciseResult
{
    public string exerciseType = "feet_targets";
    public int sets;
    public float setDurationSeconds;
    public float restDurationSeconds;
    public FootMetricsData metrics;
}

public class FootExerciseStats : MonoBehaviour
{
    private readonly List<FootAttemptData> attempts = new List<FootAttemptData>();
    private float reactionTotal;
    private float bestReaction = float.MaxValue;

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public void ResetStats()
    {
        attempts.Clear();
        Hits = 0;
        Misses = 0;
        reactionTotal = 0f;
        bestReaction = float.MaxValue;
    }

    public void Register(int target, FootSide side, bool hit, float reaction, float confidence)
    {
        attempts.Add(new FootAttemptData
        {
            sequence = attempts.Count + 1,
            targetIndex = target,
            requiredFoot = side.ToString().ToLower(),
            hit = hit,
            reactionSeconds = reaction,
            confidence = confidence
        });

        if (hit)
        {
            Hits++;
            reactionTotal += reaction;
            bestReaction = Mathf.Min(bestReaction, reaction);
        }
        else Misses++;
    }

    public FootExerciseResult BuildResult(int sets, float setDuration, float restDuration)
    {
        return new FootExerciseResult
        {
            sets = sets,
            setDurationSeconds = setDuration,
            restDurationSeconds = restDuration,
            metrics = new FootMetricsData
            {
                hits = Hits,
                misses = Misses,
                averageReactionSeconds = Hits == 0 ? 0f : reactionTotal / Hits,
                bestReactionSeconds = Hits == 0 ? 0f : bestReaction,
                attempts = new List<FootAttemptData>(attempts)
            }
        };
    }
}
