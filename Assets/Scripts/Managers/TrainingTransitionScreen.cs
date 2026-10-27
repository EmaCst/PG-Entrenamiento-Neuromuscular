using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Additive preparation/rest UI keeps the live camera and Pose running.
public sealed class TrainingTransitionScreen : MonoBehaviour
{
    public const string SceneName = "CuentaRegresiva";
    public static bool IsVisible => instance != null && instance.canvas != null && instance.canvas.enabled;
    private static TrainingTransitionScreen instance;
    private static bool busy;
    private static MonoBehaviour waiter;
    private static int generation;
    private Canvas canvas;
    private Image backdrop;
    private RectTransform safeRoot;
    private bool singleView;
    private GUIStyle exitButton;
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
        scaler.referenceResolution = NatacionPoseSource.SingleView ? new Vector2(720f, 1280f) : new Vector2(1600f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        var background = new GameObject("Transparent background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        backdrop = background.GetComponent<Image>();
        backdrop.color = Color.clear;
        backdrop.raycastTarget = false;
        singleView = NatacionPoseSource.SingleView;
        var safeObject = new GameObject("Safe area", typeof(RectTransform));
        safeObject.transform.SetParent(root.transform, false);
        safeRoot = safeObject.GetComponent<RectTransform>();
        UpdateSafeArea();
        for (int eye = 0; eye < (singleView ? 1 : 2); eye++)
        {
            float spacing = PhoneViewerSettings.Enabled ? PhoneViewerSettings.LensSpacing : 0f;
            float vertical = PhoneViewerSettings.Enabled ? PhoneViewerSettings.VerticalOffset : 0f;
            Vector2 center = new Vector2((eye == 0 ? 0.25f : 0.75f) + (eye == 0 ? -spacing : spacing) * 0.25f, 0.5f + vertical * 0.5f);
            if (singleView) center = new Vector2(0.5f, 0.5f);
            var panel = new GameObject(eye == 0 ? "Left eye" : "Right eye", typeof(RectTransform), typeof(Image));
            panel.GetComponent<Image>().color = new Color(0.012f, 0.10f, 0.23f, 0.45f);
            panel.GetComponent<Image>().raycastTarget = false;
            panel.transform.SetParent(singleView ? safeRoot : root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = center; rect.sizeDelta = new Vector2(640f, 360f); rect.anchoredPosition = Vector2.zero;
            if (singleView)
            {
                rect.anchorMin = new Vector2(.06f,.24f);rect.anchorMax = new Vector2(.94f,.76f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = new Color(.012f,.10f,.23f,.86f);
            }
            headings[eye] = Label(panel.transform, "Phase", 38f, new Vector2(0f, 130f), new Vector2(640f, 80f));
            counters[eye] = Label(panel.transform, "Seconds", 140f, Vector2.zero, new Vector2(640f, 160f));
            counters[eye].color = new Color(0.10f, 0.84f, 0.95f);
            details[eye] = Label(panel.transform, "Set", 28f, new Vector2(0f, -125f), new Vector2(640f, 80f));
            if (singleView)
            {
                FitLabel(headings[eye], .73f, .98f, 48);
                FitLabel(counters[eye], .22f, .76f, 160);
                FitLabel(details[eye], .01f, .25f, 32);
            }
        }
        canvas.enabled = false;
    }
    private void Update() { if(singleView) UpdateSafeArea(); }
    private void UpdateSafeArea()
    {
        if(safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
    }
    private static void FitLabel(TMP_Text text, float bottom, float top, float maxSize)
    {
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(.04f,bottom);rect.anchorMax = new Vector2(.96f,top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        text.enableAutoSizing = true;text.fontSizeMax = maxSize;text.fontSizeMin = maxSize*.6f;
    }
    private void OnGUI()
    {
        if(!singleView || !IsVisible) return;
        Rect safe = NatacionTrainingHud.SafeRect;
        if(exitButton == null) exitButton = new GUIStyle(GUI.skin.button);
        exitButton.fontSize = Mathf.Clamp(Screen.height/32,16,28);
        if(GUI.Button(new Rect(safe.x+12,safe.yMax-60,190,48),"Salir al menú",exitButton))
        {
            var owner = waiter;
            if(owner != null) Cancel(owner);
            ExerciseSetRunner.ActiveRunner?.Cancel();
            if(CombinedExerciseSession.Instance != null) Destroy(CombinedExerciseSession.Instance.gameObject);
            SceneManager.LoadScene("MenuTelefono",LoadSceneMode.Single);
        }
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
    public static IEnumerator Wait(MonoBehaviour owner, float seconds, string phase, string detail = "", System.Func<bool> ready = null)
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
            instance.backdrop.color = Color.clear;
            var clock = new TrainingPreparationClock(seconds);
            int lastRemaining = -1;
            bool restEndingPlayed = false;
            if (phase == "Descanso") PraxenVoice.Instance.Play("set_done");
            while (generation == ticket && instance != null && owner != null && owner.isActiveAndEnabled && clock.Remaining > 0)
            {
                bool canAdvance = ready == null || ready();
                clock.Step(Time.unscaledDeltaTime, canAdvance);
                // Return to the controller's framing UI so Recalibrar remains accessible.
                // The set runner waits for readiness and starts a fresh five-second count.
                if (!canAdvance || clock.Remaining <= 0) break;
                int remaining = Mathf.CeilToInt((float)clock.Remaining);
                if (remaining != lastRemaining)
                for (int eye = 0; eye < 2; eye++)
                {
                    if (instance.headings[eye] == null) continue;
                    instance.headings[eye].text = phase;
                    instance.counters[eye].text = phase == "Descanso" ? $"{remaining / 60:00}:{remaining % 60:00}" : remaining.ToString();
                    instance.details[eye].text = detail;
                }
                if (remaining != lastRemaining && phase == "Prepárate" && remaining >= 1 && remaining <= 5)
                    PraxenVoice.Instance.Play(remaining.ToString());
                if (phase == "Descanso" && !restEndingPlayed && seconds >= 15f && remaining <= 8)
                { restEndingPlayed = true; PraxenVoice.Instance.Play("rest_ending"); }
                lastRemaining = remaining;
                yield return null;
            }
        }
        finally
        {
            if (generation == ticket)
            {
                if (instance != null)
                {
                    instance.canvas.enabled = false;
                    instance.backdrop.color = Color.clear;
                }
                busy = false;
                waiter = null;
                PraxenVoice.Instance.Stop();
            }
        }
    }
    public static void Cancel(MonoBehaviour owner)
    {
        if (waiter != owner) return;
        generation++; busy = false; waiter = null;
        if (instance != null)
        {
            instance.canvas.enabled = false;
            instance.backdrop.color = Color.clear;
        }
    }
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null; generation++; busy = false; waiter = null;
    }
}

