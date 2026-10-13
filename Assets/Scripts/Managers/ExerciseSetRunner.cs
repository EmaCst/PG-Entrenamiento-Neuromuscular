using System.Collections;
using UnityEngine;

// Keeps the same exercise instance and cumulative statistics across individual sets.
public sealed class ExerciseSetRunner : MonoBehaviour
{
    private INeuromuscularExercise exercise;
    private MonoBehaviour controller;
    private int currentSet, totalSets;
    private float remaining;
    private GUIStyle style;
    public bool Completed { get; private set; }
    public bool IsRunning { get; private set; }

    public IEnumerator Run(MonoBehaviour owner, int sets, float seconds, float rest)
    {
        controller = owner; exercise = owner as INeuromuscularExercise;
        if (exercise == null || sets < 1 || sets > TrainingPlan.MaximumSets ||
            !TrainingPlan.ValidSeconds(seconds) || !TrainingPlan.ValidRest(rest)) yield break;
        Completed = false; IsRunning = true; totalSets = sets;
        exercise.PauseExercise();
        try
        {
            for (int set = 1; set <= sets && IsRunning; set++)
            {
                currentSet = set;
                yield return ExerciseStartCountdown.Wait(this, $"Set {set}/{sets}");
                if (!IsRunning || controller == null) yield break;
                if (set == 1) exercise.StartExercise(); else exercise.ResumeExercise();
                float deadline = Time.realtimeSinceStartup + seconds;
                while (IsRunning && controller != null && Time.realtimeSinceStartup < deadline)
                {
                    remaining = Mathf.Max(0f, deadline - Time.realtimeSinceStartup); yield return null;
                }
                if (!IsRunning || controller == null) yield break;
                exercise.PauseExercise();
                if (set < sets && rest > 0f)
                    yield return TrainingTransitionScreen.Wait(this, rest, "Descanso", $"Siguiente: set {set + 1}/{sets}");
            }
            Completed = IsRunning;
        }
        finally { if (controller != null) exercise.StopExercise(); IsRunning = false; }
    }
    public void Cancel()
    {
        IsRunning = false;
        TrainingTransitionScreen.Cancel(this);
        if (controller != null && exercise != null) exercise.StopExercise();
    }
    private void OnDisable() => Cancel();
    private void OnGUI()
    {
        if (!IsRunning || TrainingTransitionScreen.IsVisible) return;
        if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Screen.height / 35, 16, 30) };
        string text = $"Set {currentSet}/{totalSets} · {Mathf.CeilToInt(remaining)} s";
        for (int eye = 0; eye < 2; eye++)
            GUI.Box(new Rect(Screen.width * (eye * 0.5f + 0.10f), Screen.height * 0.78f,
                Screen.width * 0.30f, Screen.height * 0.09f), text, style);
    }
}
