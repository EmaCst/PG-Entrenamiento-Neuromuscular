using System;
using System.Collections.Generic;

// Pure scheduling rules, shared by the menu and runtime session.
public static class TrainingPlan
{
    public const int MaximumSets = 100;
    public const float MaximumSeconds = 3600f;
    public struct Stage
    {
        public string Scene;
        public int Set;
        public float Seconds;
        public Stage(string scene, int set, float seconds) { Scene = scene; Set = set; Seconds = seconds; }
    }
    public static bool ValidSeconds(float seconds, bool allowZero = false)
    {
        return !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds <= MaximumSeconds &&
            (seconds >= 5f || (allowZero && seconds == 0f));
    }
    public static bool ValidRest(float seconds)
    {
        return !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds >= 0f && seconds <= MaximumSeconds;
    }
    public static List<Stage> Circuit(int sets, float hands, float feet, float running)
    {
        if (sets < 1 || sets > MaximumSets || !ValidSeconds(hands, true) || !ValidSeconds(feet, true) ||
            !ValidSeconds(running, true) || hands + feet + running == 0f)
            throw new ArgumentException("Activa al menos un ejercicio; sets 1–100 y duraciones 0 o 5–3600 s.");
        var stages = new List<Stage>();
        for (int set = 1; set <= sets; set++)
        {
            if (hands > 0f) stages.Add(new Stage("TelefonoManos", set, hands));
            if (feet > 0f) stages.Add(new Stage("TelefonoPies", set, feet));
            if (running > 0f) stages.Add(new Stage("TelefonoCorrer", set, running));
        }
        return stages;
    }
}
