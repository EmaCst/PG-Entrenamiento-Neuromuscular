using UnityEngine;
using System;

[Serializable]
public class HandStatsData
{
    public int hits;
    public int misses;
    public float averageReactionSeconds;
}

[Serializable]
public class TimeByDifficultyData
{
    public float Inicial;
    public float Basico;
    public float Intermedio;
    public float Avanzado;
    public float Intensivo;
}

[Serializable]
public class AimLabMetricsData
{
    public int score;

    public int hits;
    public int misses;
    public float averageReactionSeconds;

    public string initialDifficulty;
    public string maxDifficulty;
    public string finalDifficulty;

    public TimeByDifficultyData timeByDifficulty;

    public HandStatsData left;
    public HandStatsData right;
}

[Serializable]
public class AimLabResult
{
    public string exerciseType;

    public int sets;
    public float setDurationSeconds;
    public float restDurationSeconds;

    public AimLabMetricsData metrics;
}

public class AimLabStats : MonoBehaviour
{
    // =========================
    // GLOBALES
    // =========================

    private int totalHits = 0;
    private int totalMisses = 0;

    private float totalReactionTime = 0f;
    private int reactionSamples = 0;

    // =========================
    // MANO IZQUIERDA
    // =========================

    private int leftHits = 0;
    private int leftMisses = 0;

    private float leftReactionTime = 0f;
    private int leftReactionSamples = 0;

    // =========================
    // MANO DERECHA
    // =========================

    private int rightHits = 0;
    private int rightMisses = 0;

    private float rightReactionTime = 0f;
    private int rightReactionSamples = 0;

    // =========================
    // DIFICULTAD
    // =========================

    private AimLabLevel initialDifficulty;
    private AimLabLevel maxDifficulty;
    private AimLabLevel finalDifficulty;

    private float inicialTime = 0f;
    private float basicoTime = 0f;
    private float intermedioTime = 0f;
    private float avanzadoTime = 0f;
    private float intensivoTime = 0f;

    private AimLabLevel lastTrackedLevel;
    private float lastDifficultyTimestamp;

    private bool difficultyTrackingStarted = false;

    // =========================
    // INICIALIZACIÓN
    // =========================

    public void Initialize(AimLabLevel startingLevel)
    {
        initialDifficulty = startingLevel;
        maxDifficulty = startingLevel;
        finalDifficulty = startingLevel;

        lastTrackedLevel = startingLevel;
        lastDifficultyTimestamp = Time.time;

        difficultyTrackingStarted = true;
    }

    // =========================
    // ACIERTOS
    // =========================

    public void RegisterHit(
        RequiredHand hand,
        float reactionTime
    )
    {
        totalHits++;

        totalReactionTime += reactionTime;
        reactionSamples++;

        if (hand == RequiredHand.Left)
        {
            leftHits++;

            leftReactionTime += reactionTime;
            leftReactionSamples++;
        }
        else
        {
            rightHits++;

            rightReactionTime += reactionTime;
            rightReactionSamples++;
        }
    }

    // =========================
    // FALLOS
    // =========================

    public void RegisterMiss(
        RequiredHand hand
    )
    {
        totalMisses++;

        if (hand == RequiredHand.Left)
        {
            leftMisses++;
        }
        else
        {
            rightMisses++;
        }
    }

    // =========================
    // DIFICULTAD
    // =========================

    public void RegisterDifficultyChange(
        AimLabLevel newLevel
    )
    {
        if (!difficultyTrackingStarted)
        {
            Initialize(newLevel);
            return;
        }

        AddTimeToDifficulty(
            lastTrackedLevel,
            Time.time - lastDifficultyTimestamp
        );

        lastTrackedLevel = newLevel;
        lastDifficultyTimestamp = Time.time;

        finalDifficulty = newLevel;

        if (newLevel > maxDifficulty)
        {
            maxDifficulty = newLevel;
        }
    }

    public void StopDifficultyTracking()
    {
        if (!difficultyTrackingStarted)
        {
            return;
        }

        AddTimeToDifficulty(
            lastTrackedLevel,
            Time.time - lastDifficultyTimestamp
        );

        difficultyTrackingStarted = false;
    }

    private void AddTimeToDifficulty(
        AimLabLevel level,
        float seconds
    )
    {
        switch (level)
        {
            case AimLabLevel.Inicial:
                inicialTime += seconds;
                break;

            case AimLabLevel.Basico:
                basicoTime += seconds;
                break;

            case AimLabLevel.Intermedio:
                intermedioTime += seconds;
                break;

            case AimLabLevel.Avanzado:
                avanzadoTime += seconds;
                break;

            case AimLabLevel.Intensivo:
                intensivoTime += seconds;
                break;
        }
    }

    // =========================
    // PROMEDIOS
    // =========================

    private float GetGlobalAverageReaction()
    {
        if (reactionSamples == 0)
        {
            return 0f;
        }

        return totalReactionTime / reactionSamples;
    }

    private float GetLeftAverageReaction()
    {
        if (leftReactionSamples == 0)
        {
            return 0f;
        }

        return leftReactionTime / leftReactionSamples;
    }

    private float GetRightAverageReaction()
    {
        if (rightReactionSamples == 0)
        {
            return 0f;
        }

        return rightReactionTime / rightReactionSamples;
    }

    // =========================
    // CREAR RESULTADO
    // =========================

    public AimLabResult BuildResult(
        int sets,
        float setDuration,
        float restDuration
    )
    {
        StopDifficultyTracking();

        AimLabResult result = new AimLabResult();

        result.exerciseType = "aimlab";

        result.sets = sets;
        result.setDurationSeconds = setDuration;
        result.restDurationSeconds = restDuration;

        result.metrics = new AimLabMetricsData();

        result.metrics.score = totalHits;

        result.metrics.hits = totalHits;
        result.metrics.misses = totalMisses;
        result.metrics.averageReactionSeconds =
            GetGlobalAverageReaction();

        result.metrics.initialDifficulty =
            initialDifficulty.ToString();

        result.metrics.maxDifficulty =
            maxDifficulty.ToString();

        result.metrics.finalDifficulty =
            finalDifficulty.ToString();

        result.metrics.timeByDifficulty =
            new TimeByDifficultyData();

        result.metrics.timeByDifficulty.Inicial =
            inicialTime;

        result.metrics.timeByDifficulty.Basico =
            basicoTime;

        result.metrics.timeByDifficulty.Intermedio =
            intermedioTime;

        result.metrics.timeByDifficulty.Avanzado =
            avanzadoTime;

        result.metrics.timeByDifficulty.Intensivo =
            intensivoTime;

        result.metrics.left =
            new HandStatsData();

        result.metrics.left.hits =
            leftHits;

        result.metrics.left.misses =
            leftMisses;

        result.metrics.left.averageReactionSeconds =
            GetLeftAverageReaction();

        result.metrics.right =
            new HandStatsData();

        result.metrics.right.hits =
            rightHits;

        result.metrics.right.misses =
            rightMisses;

        result.metrics.right.averageReactionSeconds =
            GetRightAverageReaction();

        return result;
    }

    // =========================
    // JSON
    // =========================

    public string BuildJson(
        int sets,
        float setDuration,
        float restDuration
    )
    {
        AimLabResult result =
            BuildResult(
                sets,
                setDuration,
                restDuration
            );

        return JsonUtility.ToJson(
            result,
            true
        );
    }
}