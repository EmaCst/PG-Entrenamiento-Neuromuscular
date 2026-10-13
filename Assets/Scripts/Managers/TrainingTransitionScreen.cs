using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This scene contains only the preparation/rest UI. It loads additively to retain AR tracking.
public sealed class TrainingTransitionScreen : MonoBehaviour
{
    public const string SceneName = "CuentaRegresiva";
    public static bool IsVisible => instance != null && instance.canvas != null && instance.canvas.enabled;
    private static TrainingTransitionScreen instance;
    private static bool busy;
    private static MonoBehaviour waiter;
    private static int generation;
    private Canvas canvas;
    private readonly TMP_Text[] headings = new TMP_Text[2];
    private readonly TMP_Text[] counters = new TMP_Text[2];
    private readonly TMP_Text[] details = new TMP_Text[2];

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        var root = new GameObject("Countdown canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        var background = new GameObject("Transparent background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = Color.clear;
        background.GetComponent<Image>().raycastTarget = false;
        for (int eye = 0; eye < 2; eye++)
        {
            float spacing = PhoneViewerSettings.Enabled ? PhoneViewerSettings.LensSpacing : 0f;
            float vertical = PhoneViewerSettings.Enabled ? PhoneViewerSettings.VerticalOffset : 0f;
            Vector2 center = new Vector2((eye == 0 ? 0.25f : 0.75f) + (eye == 0 ? -spacing : spacing) * 0.25f, 0.5f + vertical * 0.5f);
            var panel = new GameObject(eye == 0 ? "Left eye" : "Right eye", typeof(RectTransform), typeof(Image));
            panel.GetComponent<Image>().color = new Color(0.012f, 0.10f, 0.23f, 0.45f);
            panel.GetComponent<Image>().raycastTarget = false;
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = center; rect.sizeDelta = new Vector2(640f, 360f); rect.anchoredPosition = Vector2.zero;
            headings[eye] = Label(panel.transform, "Phase", 38f, new Vector2(0f, 130f), new Vector2(640f, 80f));
            counters[eye] = Label(panel.transform, "Seconds", 140f, Vector2.zero, new Vector2(640f, 160f));
            counters[eye].color = new Color(0.10f, 0.84f, 0.95f);
            details[eye] = Label(panel.transform, "Set", 28f, new Vector2(0f, -125f), new Vector2(640f, 80f));
        }
        canvas.enabled = false;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static TMP_Text Label(Transform parent, string name, float size, Vector2 position, Vector2 dimensions)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions; rect.anchoredPosition = position;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        return text;
    }
    public static IEnumerator Wait(MonoBehaviour owner, float seconds, string phase, string detail = "")
    {
        if (seconds <= 0f || owner == null) yield break;
        while (busy && owner != null && owner.isActiveAndEnabled) yield return null;
        if (owner == null || !owner.isActiveAndEnabled) yield break;
        busy = true; waiter = owner;
        int ticket = ++generation;
        try
        {
            if (instance == null)
            {
                if (Application.CanStreamedLevelBeLoaded(SceneName))
                    yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
                else
                {
                    Debug.LogWarning("CuentaRegresiva no está en el Build Profile. Se crea la UI temporalmente.");
                    new GameObject(SceneName).AddComponent<TrainingTransitionScreen>();
                }
            }
            if (instance == null || owner == null || generation != ticket) yield break;
            instance.canvas.enabled = true;
            float deadline = Time.realtimeSinceStartup + seconds;
            int lastRemaining = -1;
            while (generation == ticket && instance != null && owner != null && owner.isActiveAndEnabled && Time.realtimeSinceStartup < deadline)
            {
                int remaining = Mathf.CeilToInt(Mathf.Max(0f, deadline - Time.realtimeSinceStartup));
                if (remaining != lastRemaining)
                for (int eye = 0; eye < 2; eye++)
                {
                    instance.headings[eye].text = phase;
                    instance.counters[eye].text = phase == "Descanso" ? $"{remaining / 60:00}:{remaining % 60:00}" : remaining.ToString();
                    instance.details[eye].text = detail;
                }
                lastRemaining = remaining;
                yield return null;
            }
        }
        finally
        {
            if (generation == ticket)
            { if (instance != null) instance.canvas.enabled = false; busy = false; waiter = null; }
        }
    }
    public static void Cancel(MonoBehaviour owner)
    {
        if (waiter != owner) return;
        generation++; busy = false; waiter = null;
        if (instance != null) instance.canvas.enabled = false;
    }
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null; generation++; busy = false; waiter = null;
    }
}
