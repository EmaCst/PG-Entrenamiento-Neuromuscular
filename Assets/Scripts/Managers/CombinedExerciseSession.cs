using System.Collections;
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

    private string status = "Configura la sesion";
    private float remaining;
    private bool running;
    private bool finished;
    private bool configuredFromMenu;

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
        if (PhoneTrainingOptions.HasConfiguration && PhoneTrainingOptions.IsCircuit)
        {
            configuredFromMenu = true;
            repetitions = PhoneTrainingOptions.Repetitions;
            handSeconds = PhoneTrainingOptions.HandSeconds;
            feetSeconds = PhoneTrainingOptions.FeetSeconds;
            runningSeconds = PhoneTrainingOptions.RunningSeconds;
            restSeconds = PhoneTrainingOptions.RestSeconds;
        }
    }

    private static void EnsureMenuCamera()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.isActiveAndEnabled && camera.targetDisplay == 0) return;
        }

        GameObject cameraObject = new GameObject("CombinedSessionMenuCamera");
        cameraObject.tag = "MainCamera";
        Camera menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
    }

    private void OnGUI()
    {
        const float width = 520f;
        GUILayout.BeginArea(new Rect(16f, 16f, width, Screen.height - 32f), GUI.skin.box);
        GUILayout.Label("CIRCUITO NEUROMUSCULAR");
        if (running || finished)
        {
            GUILayout.Label(status);
            GUILayout.Label($"Tiempo restante: {Mathf.CeilToInt(remaining)} s");

            if (finished && GUILayout.Button("Volver al menu", GUILayout.Height(48f)))
                ReturnToMenu();
        }
        else if (configuredFromMenu)
            GUILayout.Label("Iniciando circuito con la configuracion elegida...");
        else if (GUILayout.Button("Iniciar circuito predeterminado", GUILayout.Height(48f)))
            StartCoroutine(RunSession());

        GUILayout.EndArea();
    }

    private void Start()
    {
        EnsureMenuCamera();
        if (configuredFromMenu)
            StartCoroutine(RunSession());
    }

    private void ReturnToMenu()
    {
        Instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
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
                if (!string.Equals(sceneName, "TelefonoManos", System.StringComparison.Ordinal))
                {
                    yield return WaitForSceneExerciseReady(sceneName, controllers);
                    if (finished) yield break;
                }

                status = $"Preparate: {ReadableName(sceneName)}";
                remaining = 0f;
                yield return ExerciseStartCountdown.Wait(this);
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

    private IEnumerator WaitForSceneExerciseReady(string sceneName, MonoBehaviour[] controllers)
    {
        const float timeoutSeconds = 120f;
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        status = string.Equals(sceneName, "TelefonoCorrer", System.StringComparison.Ordinal)
            ? "Delimita el area: mueve el telefono para detectar el suelo y toca sus cuatro esquinas."
            : $"Esperando camara y seguimiento: {ReadableName(sceneName)}";

        while (Time.realtimeSinceStartup < deadline)
        {
            bool hasReadinessProperty = false;
            bool allReady = true;
            foreach (MonoBehaviour controller in controllers)
            {
                var property = controller.GetType().GetProperty("IsReady", BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.PropertyType != typeof(bool)) continue;
                hasReadinessProperty = true;
                if (!(bool)property.GetValue(controller)) allReady = false;
            }

            if (!hasReadinessProperty || allReady) yield break;
            yield return null;
        }

        running = false;
        finished = true;
        status = $"No se pudo iniciar {ReadableName(sceneName)}: camara o seguimiento sin respuesta.";
        Debug.LogError(status);
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
