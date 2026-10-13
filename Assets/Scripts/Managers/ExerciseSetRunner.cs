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
    private bool hasStereoHud;
    public static ExerciseSetRunner ActiveRunner { get; private set; }
    public bool IsExercising { get; private set; }
    public string HudText
    {
        get
        {
            int seconds = Mathf.CeilToInt(remaining);
            return IsExercising ? $"Set {currentSet}/{totalSets} · {seconds / 60:00}:{seconds % 60:00}" : string.Empty;
        }
    }
    public bool Completed { get; private set; }
    public bool IsRunning { get; private set; }

    public IEnumerator Run(MonoBehaviour owner, int sets, float seconds, float rest)
    {
        controller = owner; exercise = owner as INeuromuscularExercise;
        if (exercise == null || sets < 1 || sets > TrainingPlan.MaximumSets ||
            !TrainingPlan.ValidSeconds(seconds) || !TrainingPlan.ValidRest(rest)) yield break;
        Completed = false; IsRunning = true; totalSets = sets; ActiveRunner = this;
        hasStereoHud = Object.FindFirstObjectByType<PhoneStereoOverlay>() != null;
        exercise.PauseExercise();
        try
        {
            yield return PraxenVoice.Instance.Say(this, PraxenVoice.ExerciseKey(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            for (int set = 1; set <= sets && IsRunning; set++)
            {
                currentSet = set;
                if (set == sets) yield return PraxenVoice.Instance.Say(this, "last_set");
                yield return ExerciseStartCountdown.Wait(this, $"Set {set}/{sets}");
                if (!IsRunning || controller == null) yield break;
                if (set == 1) exercise.StartExercise(); else exercise.ResumeExercise();
                PraxenVoice.Instance.Play("start");
                bool warned = false;
                IsExercising = true;
                float deadline = Time.realtimeSinceStartup + seconds;
                while (IsRunning && controller != null && Time.realtimeSinceStartup < deadline)
                {
                    remaining = Mathf.Max(0f, deadline - Time.realtimeSinceStartup);
                    if (!warned && seconds > 10f && remaining <= 10f) { warned = true; PraxenVoice.Instance.Play("ten_seconds"); }
                    yield return null;
                }
                if (!IsRunning || controller == null) yield break;
                IsExercising = false;
                exercise.PauseExercise();
                PraxenVoice.Instance.Stop();
                if (set < sets && rest > 0f)
                    yield return TrainingTransitionScreen.Wait(this, rest, "Descanso", $"Siguiente: set {set + 1}/{sets}");
            }
            Completed = IsRunning;
            if (Completed) yield return PraxenVoice.Instance.Say(this, "finished");
        }
        finally
        {
            if (controller != null) exercise.StopExercise(); IsRunning = false; IsExercising = false;
            if (ActiveRunner == this) ActiveRunner = null;
        }
    }
    public void Cancel()
    {
        IsRunning = false; IsExercising = false;
        if (ActiveRunner == this) ActiveRunner = null;
        TrainingTransitionScreen.Cancel(this);
        PraxenVoice.Instance.Stop();
        if (controller != null && exercise != null) exercise.StopExercise();
    }
    private void OnDisable() => Cancel();
    private void OnGUI()
    {
        if (!IsExercising || hasStereoHud || TrainingTransitionScreen.IsVisible) return;
        if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Screen.height / 35, 16, 30) };
        string text = HudText;
        for (int eye = 0; eye < 2; eye++)
            GUI.Box(new Rect(Screen.width * (eye * 0.5f + 0.10f), Screen.height * 0.17f,
                Screen.width * 0.30f, Screen.height * 0.09f), text, style);
    }
}
