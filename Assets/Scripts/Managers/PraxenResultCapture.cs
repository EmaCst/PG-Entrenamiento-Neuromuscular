using UnityEngine;

public static class PraxenResultCapture
{
    // Capturar antes de cambiar de escena, una vez detenidos los objetivos.
    public static void Add(SessionResultData session, string scene, int sets, float seconds, float rest)
    {
        if (scene == "TelefonoManos")
        {
            var stats = Object.FindFirstObjectByType<AimLabStats>();
            if (stats != null) session.exercises.Add(stats.BuildResult(sets, seconds, rest));
        }
        else if (scene == "TelefonoPies")
        {
            var stats = Object.FindFirstObjectByType<FootExerciseStats>();
            if (stats != null) session.footExercises.Add(stats.BuildResult(sets, seconds, rest));
        }
        else if (scene == "TelefonoCorrer")
        {
            var stats = Object.FindFirstObjectByType<RunningExerciseStats>();
            var manager = Object.FindFirstObjectByType<RunningExerciseManager>();
            if (stats != null && (manager != null || Object.FindFirstObjectByType<NatacionTurnsExercise>() != null))
                session.runningExercises.Add(stats.BuildResult(sets, seconds, rest));
        }
    }
}

