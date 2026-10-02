using UnityEngine;

public static class PhoneTrainingOptions
{
    public static bool HasConfiguration { get; private set; }
    public static bool IsCircuit { get; private set; }
    public static string SelectedScene { get; private set; }
    public static int DifficultyLevel { get; private set; } = 2;
    public static int Repetitions { get; private set; } = 2;
    public static float HandSeconds { get; private set; } = 60f;
    public static float FeetSeconds { get; private set; } = 60f;
    public static float RunningSeconds { get; private set; } = 60f;
    public static float RestSeconds { get; private set; } = 20f;
    public static float IndividualSeconds { get; private set; } = 60f;

    public static void ConfigureIndividual(string scene, int difficulty, float duration)
    {
        HasConfiguration = true;
        IsCircuit = false;
        SelectedScene = scene;
        DifficultyLevel = Mathf.Clamp(difficulty, 0, 4);
        IndividualSeconds = Mathf.Max(5f, duration);
    }

    public static void ConfigureCircuit(int difficulty, int repetitions,
        float handSeconds, float feetSeconds, float runningSeconds, float restSeconds)
    {
        HasConfiguration = true;
        IsCircuit = true;
        SelectedScene = null;
        DifficultyLevel = Mathf.Clamp(difficulty, 0, 4);
        Repetitions = Mathf.Max(1, repetitions);
        HandSeconds = Mathf.Max(5f, handSeconds);
        FeetSeconds = Mathf.Max(5f, feetSeconds);
        RunningSeconds = Mathf.Max(5f, runningSeconds);
        RestSeconds = Mathf.Max(0f, restSeconds);
    }

    public static bool AppliesTo(string scene)
    {
        return HasConfiguration && (IsCircuit || string.Equals(SelectedScene, scene, System.StringComparison.Ordinal));
    }

    public static float DurationFor(string scene)
    {
        if (!IsCircuit) return IndividualSeconds;
        if (scene == "TelefonoManos") return HandSeconds;
        if (scene == "TelefonoPies") return FeetSeconds;
        return RunningSeconds;
    }

    public static float FootTargetLifetime(int level)
    {
        return LevelValue(level, 3.5f, 3f, 2.5f, 2f, 1.5f);
    }

    public static float RunningInitialDistance(int level)
    {
        return LevelValue(level, 1f, 1.25f, 1.5f, 1.8f, 2.1f);
    }

    public static float RunningMaximumDistance(int level)
    {
        return LevelValue(level, 2f, 2.25f, 2.5f, 3f, 3f);
    }

    private static float LevelValue(int level, float initial, float basic,
        float intermediate, float advanced, float intensive)
    {
        switch (Mathf.Clamp(level, 0, 4))
        {
            case 0: return initial;
            case 1: return basic;
            case 2: return intermediate;
            case 3: return advanced;
            default: return intensive;
        }
    }
}
