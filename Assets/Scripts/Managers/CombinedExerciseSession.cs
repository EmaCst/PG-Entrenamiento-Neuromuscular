using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CombinedExerciseSession : MonoBehaviour
{
    public static CombinedExerciseSession Instance { get; private set; }

    [SerializeField, Min(1)] private int repetitions = 2;
    [SerializeField, Min(5f)] private float handSeconds = 60f;
    [SerializeField, Min(5f)] private float feetSeconds = 60f;
    [SerializeField, Min(5f)] private float runningSeconds = 60f;
    [SerializeField, Min(0f)] private float restSeconds = 20f;
    [SerializeField] private string[] exerciseScenes =
    {
        "TelefonoManos", "TelefonoPies", "TelefonoCorrer"
    };

    private string repetitionsText;
    private string handTimeText;
    private string feetTimeText;
    private string runningTimeText;
    private string restText;
    private string status = "Configura la sesion";
    private float remaining;
    private bool running;
    private bool finished;

    public bool IsRunning => running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        repetitionsText = repetitions.ToString(CultureInfo.InvariantCulture);
        handTimeText = handSeconds.ToString(CultureInfo.InvariantCulture);
        feetTimeText = feetSeconds.ToString(CultureInfo.InvariantCulture);
        runningTimeText = runningSeconds.ToString(CultureInfo.InvariantCulture);
        restText = restSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private void OnGUI()
    {
        const float width = 430f;
        GUILayout.BeginArea(new Rect(20f, 20f, width, Screen.height - 40f), GUI.skin.box);
        GUILayout.Label("Sesion neuromuscular combinada");

        if (!running && !finished)
        {
            DrawField("Repeticiones del ciclo", ref repetitionsText);
            DrawField("Segundos de manos", ref handTimeText);
            DrawField("Segundos de pies", ref feetTimeText);
            DrawField("Segundos de correr", ref runningTimeText);
            DrawField("Segundos de descanso", ref restText);

            if (GUILayout.Button("Iniciar manos - pies - correr", GUILayout.Height(42f)))
            {
                ApplyConfiguration();
                StartCoroutine(RunSession());
            }
        }
        else
        {
            GUILayout.Label(status);
            GUILayout.Label($"Tiempo restante: {Mathf.CeilToInt(remaining)} s");

            if (finished && GUILayout.Button("Configurar otra sesion"))
            {
                finished = false;
                status = "Configura la sesion";
            }
        }

        GUILayout.EndArea();
    }

    private static void DrawField(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(220f));
        value = GUILayout.TextField(value, GUILayout.Width(160f));
        GUILayout.EndHorizontal();
    }

    private void ApplyConfiguration()
    {
        if (int.TryParse(repetitionsText, out int parsedRepetitions))
            repetitions = Mathf.Max(1, parsedRepetitions);
        if (float.TryParse(handTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedHands))
            handSeconds = Mathf.Max(5f, parsedHands);
        if (float.TryParse(feetTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFeet))
            feetSeconds = Mathf.Max(5f, parsedFeet);
        if (float.TryParse(runningTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedRunning))
            runningSeconds = Mathf.Max(5f, parsedRunning);
        if (float.TryParse(restText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedRest))
            restSeconds = Mathf.Max(0f, parsedRest);
    }

    private IEnumerator RunSession()
    {
        running = true;
        finished = false;

        for (int repetition = 1; repetition <= repetitions; repetition++)
        {
            for (int index = 0; index < exerciseScenes.Length; index++)
            {
                string sceneName = exerciseScenes[index];
                status = $"Ciclo {repetition}/{repetitions}: cargando {ReadableName(sceneName)}";
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                yield return null;

                MonoBehaviour[] controllers = FindExerciseControllers();
                InvokeLifecycle(controllers, "StartExercise");
                yield return Countdown(GetExerciseDuration(index),
                    $"Ciclo {repetition}/{repetitions}: {ReadableName(sceneName)}");
                InvokeLifecycle(controllers, "StopExercise");

                bool isLast = repetition == repetitions && index == exerciseScenes.Length - 1;
                if (!isLast && restSeconds > 0f)
                    yield return Countdown(restSeconds, "Descanso");
            }
        }

        running = false;
        finished = true;
        remaining = 0f;
        status = "Sesion completada";
    }

    private IEnumerator Countdown(float seconds, string label)
    {
        remaining = seconds;
        status = label;
        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static MonoBehaviour[] FindExerciseControllers()
    {
        return Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(component => component != null &&
                component.GetType().GetMethod("StartExercise", BindingFlags.Instance | BindingFlags.Public) != null &&
                component.GetType().GetMethod("StopExercise", BindingFlags.Instance | BindingFlags.Public) != null)
            .ToArray();
    }

    private static void InvokeLifecycle(MonoBehaviour[] controllers, string method)
    {
        foreach (MonoBehaviour controller in controllers)
            controller.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public)?.Invoke(controller, null);
    }

    private static string ReadableName(string sceneName)
    {
        if (sceneName.Contains("Manos")) return "Ejercicio de manos";
        if (sceneName.Contains("Pies")) return "Ejercicio de pies";
        if (sceneName.Contains("Correr")) return "Ejercicio de correr";
        return sceneName;
    }

    private float GetExerciseDuration(int index)
    {
        if (index == 0) return handSeconds;
        if (index == 1) return feetSeconds;
        return runningSeconds;
    }
}
