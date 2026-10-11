using System.Collections;
using UnityEngine;

/// <summary>Preparation time shared by individual exercises and circuit steps.</summary>
public sealed class ExerciseStartCountdown : MonoBehaviour
{
    public const float PreparationSeconds = 5f;
    private MonoBehaviour owner;
    private float deadline;
    private GUIStyle labelStyle;

    public static IEnumerator Wait(MonoBehaviour owner)
    {
        GameObject overlay = new GameObject("Exercise preparation countdown");
        overlay.transform.SetParent(owner.transform, false);
        ExerciseStartCountdown countdown = overlay.AddComponent<ExerciseStartCountdown>();
        countdown.owner = owner;
        countdown.deadline = Time.realtimeSinceStartup + PreparationSeconds;
        try
        {
            yield return new WaitForSecondsRealtime(PreparationSeconds);
        }
        finally
        {
            if (overlay != null) Object.Destroy(overlay);
        }
    }

    private void OnGUI()
    {
        if (owner == null || !owner.isActiveAndEnabled) return;
        int seconds = Mathf.CeilToInt(deadline - Time.realtimeSinceStartup);
        if (seconds <= 0) return;
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.wordWrap = true;
        }

        labelStyle.fontSize = Mathf.Clamp(Screen.height / 10, 28, 72);
        PhoneStereoRig rig = Camera.main != null ? Camera.main.GetComponent<PhoneStereoRig>() : null;
        string message = "Preparate\n" + seconds;
        if (rig != null && rig.IsReady)
        {
            Draw(message, Screen.width * 0.25f, Screen.width * 0.5f);
            Draw(message, Screen.width * 0.75f, Screen.width * 0.5f);
        }
        else Draw(message, Screen.width * 0.5f, Screen.width);
    }

    private void Draw(string message, float centerX, float viewportWidth)
    {
        float width = Mathf.Min(360f, viewportWidth * 0.8f);
        float height = Mathf.Min(220f, Screen.height * 0.45f);
        GUI.Box(new Rect(centerX - width * 0.5f, (Screen.height - height) * 0.5f, width, height),
            message, labelStyle);
    }
}
