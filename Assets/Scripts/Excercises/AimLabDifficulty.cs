using UnityEngine;
using TMPro;

public enum AimLabLevel
{
    Inicial,
    Basico,
    Intermedio,
    Avanzado,
    Intensivo
}

public class AimLabDifficulty : MonoBehaviour
{
    public AimLabLevel currentLevel = AimLabLevel.Inicial;

    [Header("UI")]
    public TMP_Text difficultyText;
    public AimLabStats stats;

    private int consecutiveHits = 0;
    private int consecutiveMisses = 0;

    private const int hitsToLevelUp = 5;
    private const int missesToLevelDown = 3;

    public float TargetLifetime
    {
        get
        {
            switch (currentLevel)
            {
                case AimLabLevel.Inicial:
                    return 2.5f;

                case AimLabLevel.Basico:
                    return 2.0f;

                case AimLabLevel.Intermedio:
                    return 1.5f;

                case AimLabLevel.Avanzado:
                    return 1.0f;

                case AimLabLevel.Intensivo:
                    return 0.5f;

                default:
                    return 2.5f;
            }
        }
    }

    public float RespawnDelay
    {
        get
        {
            switch (currentLevel)
            {
                case AimLabLevel.Inicial:
                    return 1.0f;

                case AimLabLevel.Basico:
                    return 0.75f;

                case AimLabLevel.Intermedio:
                    return 0.5f;

                case AimLabLevel.Avanzado:
                    return 0.25f;

                case AimLabLevel.Intensivo:
                    return 0f;

                default:
                    return 1.0f;
            }
        }
    }

    void Start()
    {
        UpdateDifficultyUI();
    }

    public void RegisterHit()
    {
        consecutiveHits++;
        consecutiveMisses = 0;

        if (consecutiveHits >= hitsToLevelUp)
        {
            LevelUp();

            consecutiveHits = 0;
            consecutiveMisses = 0;
        }
    }

    public void RegisterMiss()
    {
        consecutiveMisses++;
        consecutiveHits = 0;

        if (consecutiveMisses >= missesToLevelDown)
        {
            LevelDown();

            consecutiveHits = 0;
            consecutiveMisses = 0;
        }
    }

    private void LevelUp()
    {
        if (currentLevel < AimLabLevel.Intensivo)
        {
            currentLevel++;

            Debug.Log(
                "SUBE DE NIVEL → " +
                currentLevel
            );

            if (stats != null)
            {
                stats.RegisterDifficultyChange(currentLevel);
            }

            UpdateDifficultyUI();
        }
    }

    private void LevelDown()
    {
        if (currentLevel > AimLabLevel.Inicial)
        {
            currentLevel--;

            Debug.Log(
                "BAJA DE NIVEL → " +
                currentLevel
            );

            if (stats != null)
            {
                stats.RegisterDifficultyChange(currentLevel);
            }

            UpdateDifficultyUI();
        }
    }

    public void ResetStreaks()
    {
        consecutiveHits = 0;
        consecutiveMisses = 0;
    }

    void UpdateDifficultyUI()
    {
        if (difficultyText != null)
        {
            difficultyText.text =
                "Nivel: " + currentLevel;
        }
    }
}