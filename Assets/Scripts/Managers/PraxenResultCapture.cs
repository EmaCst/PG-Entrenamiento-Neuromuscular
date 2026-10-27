using UnityEngine;

public static class PraxenResultCapture
{
    // Capturar antes de cambiar de escena, una vez detenidos los objetivos.
    public static void Add(SessionResultData session, string scene, int sets, float seconds, float rest)
    {
        scene = PraxenSceneRoutes.Canonical(scene);
        if (scene == "TelefonoManos")
        {
            var stats = Object.FindFirstObjectByType<AimLabStats>();
            if (stats != null)
            {
                var entry = stats.BuildResult(sets, seconds, rest);
                if (Object.FindFirstObjectByType<NatacionHandsExercise>() != null) entry.measurementMode = NatacionMeasurement.Hands;
                else entry.measurementMode = "mediapipe_hand_screen_contact_v1";
                session.exercises.Add(entry);
            }
        }
        else if (scene == "TelefonoPies")
        {
            var stats = Object.FindFirstObjectByType<FootExerciseStats>();
            if (stats != null)
            {
                var entry = stats.BuildResult(sets, seconds, rest);
                if (Object.FindFirstObjectByType<NatacionFeetExercise>() != null) entry.measurementMode = NatacionMeasurement.Feet;
                else entry.measurementMode = "reference_mat_foot_contact_v1";
                session.footExercises.Add(entry);
            }
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


