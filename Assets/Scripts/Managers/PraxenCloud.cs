using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// El JWT vive solamente en memoria. La cola conserva el propietario y servidor originales.
public class PraxenCloud : MonoBehaviour
{
    public const string DefaultApiUrl = "https://api-praxen.onrender.com";
    public static PraxenCloud Instance { get; private set; }
    public int AthleteId { get; private set; }
    public string Status { get; private set; } = "Inicia sesión para sincronizar resultados";
    private string baseUrl = "", token = "", username = "", password = "", nombre = "", federacion = "";
    private bool registering, busy, uploading;
    private float nextRetry;
    private int retrySeconds = 15;
    private string QueuePath => Path.Combine(Application.persistentDataPath, "praxen-results");
    [Serializable] private class Credentials { public string username, password, nombre, federacion; }
    [Serializable] private class Athlete { public int id; }
    [Serializable] private class AuthResponse { public string token; public Athlete atleta; }
    [Serializable] private class SavedResponse { public SessionResultData session; }
    [Serializable] private class Message { public string message; }
    [Serializable] private class Pending { public string server; public int athleteId; public SessionResultData result; public bool blocked; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }
    public static PraxenCloud EnsureInstance()
    {
        if (Instance == null) new GameObject("PraxenCloud").AddComponent<PraxenCloud>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        baseUrl = DefaultApiUrl;
        Directory.CreateDirectory(QueuePath);
    }
    private void Update()
    {
        if (!busy && !uploading && AthleteId > 0 && Time.unscaledTime >= nextRetry)
            StartCoroutine(Flush());
    }
    public SessionResultData Begin(string mode)
    {
        return new SessionResultData { sessionId = Guid.NewGuid().ToString(), athleteId = AthleteId,
            mode = mode, startedAt = DateTime.UtcNow.ToString("o") };
    }
    public void Submit(SessionResultData result)
    {
        result.endedAt = DateTime.UtcNow.ToString("o");
        if (result.exercises.Count + result.footExercises.Count + result.runningExercises.Count == 0) return;
        // Nunca asignar resultados de un invitado o de otra cuenta a la cuenta actual.
        var pending = new Pending { server = baseUrl, athleteId = result.athleteId, result = result };
        try { Save(Path.Combine(QueuePath, result.sessionId + ".json"), pending); }
        catch (Exception) { Status = "No se pudo guardar el resultado en el teléfono"; return; }
        Status = result.athleteId > 0 ? "Resultado guardado; pendiente de sincronizar" : "Resultado local de invitado. Inicia sesión antes del próximo ejercicio";
        nextRetry = 0;
    }
    private static void Save(string path, Pending pending)
    {
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(pending), Encoding.UTF8);
        // Reemplazo atómico para conservar la copia anterior ante un cierre inesperado.
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }
    private static UnityWebRequest Post(string url, string json)
    {
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 20;
        return request;
    }
    private IEnumerator Authenticate()
    {
        if (!Uri.TryCreate(baseUrl.Trim().TrimEnd('/'), UriKind.Absolute, out Uri uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        { Status = "Indica la URL HTTPS de la API, sin /api ni parámetros"; yield break; }
        baseUrl = uri.AbsoluteUri.TrimEnd('/');
        PlayerPrefs.SetString("Praxen.ApiUrl", baseUrl); PlayerPrefs.Save();
        busy = true;
        try {
            using (var request = Post(baseUrl + "/api/auth/" + (registering ? "register" : "login"),
                JsonUtility.ToJson(new Credentials { username = username, password = password, nombre = nombre, federacion = federacion })))
            {
                yield return request.SendWebRequest(); password = "";
                if (request.result != UnityWebRequest.Result.Success) { Status = ErrorMessage(request); yield break; }
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (response == null || string.IsNullOrEmpty(response.token) || response.atleta == null || response.atleta.id <= 0)
                { Status = "Respuesta de autenticación incompleta"; yield break; }
                token = response.token; AthleteId = response.atleta.id;
                Status = "Sesión iniciada. Resultados vinculados a tu cuenta"; nextRetry = 0; retrySeconds = 15;
            }
        } finally { busy = false; }
    }
    private static string ErrorMessage(UnityWebRequest request)
    {
        if (request.responseCode == 0) return "No se pudo conectar con la API";
        try { var message = JsonUtility.FromJson<Message>(request.downloadHandler.text); if (!string.IsNullOrEmpty(message?.message)) return message.message; }
        catch (Exception) { }
        return "La API respondió con error " + request.responseCode;
    }
    private IEnumerator Flush()
    {
        uploading = true;
        nextRetry = Time.unscaledTime + retrySeconds;
        try {
            foreach (string file in Directory.GetFiles(QueuePath, "*.json"))
            {
                Pending pending;
                try { pending = JsonUtility.FromJson<Pending>(File.ReadAllText(file)); }
                catch (Exception) { continue; }
                if (pending == null || pending.result == null || pending.blocked || pending.server != baseUrl || pending.athleteId != AthleteId) continue;
                using (var request = Post(pending.server + "/api/sesiones", JsonUtility.ToJson(pending.result)))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                    yield return request.SendWebRequest();
                    // Solo los códigos documentados confirman persistencia; no eliminar en errores.
                    if (request.result == UnityWebRequest.Result.Success && (request.responseCode == 200 || request.responseCode == 201))
                    {
                        SavedResponse saved = null;
                        try { saved = JsonUtility.FromJson<SavedResponse>(request.downloadHandler.text); }
                        catch (Exception) { }
                        if (saved?.session == null || saved.session.sessionId != pending.result.sessionId || saved.session.athleteId != pending.athleteId)
                        { Status = "Respuesta inesperada de la API. El resultado sigue guardado"; yield break; }
                        File.Delete(file); Status = "Resultados sincronizados"; retrySeconds = 15;
                    }
                    else if (request.responseCode == 401)
                    {
                        token = ""; AthleteId = 0; Status = "Tu sesión caducó. Inicia sesión para enviar los resultados pendientes"; yield break;
                    }
                    else if (request.responseCode >= 400 && request.responseCode < 500 && request.responseCode != 408 && request.responseCode != 429)
                    {
                        pending.blocked = true; Save(file, pending);
                        Status = ErrorMessage(request) + " (resultado conservado en el teléfono)";
                    }
                    else
                    {
                        Status = "Sin conexión con la nube. Resultados conservados para reintentar";
                        retrySeconds = Math.Min(120, retrySeconds * 2); nextRetry = Time.unscaledTime + retrySeconds; yield break;
                    }
                }
            }
        } finally { uploading = false; }
    }
    // Drawn by PhoneExerciseMenu in its own page and coordinate system.
    public void DrawAccount(Rect area, GUIStyle label, GUIStyle input, GUIStyle button, GUIStyle status, GUIStyle toggle)
    {
        GUILayout.BeginArea(area);
        GUILayout.Label(Status, status, GUILayout.Height(48f));
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && !busy && !uploading;
        if (AthleteId > 0)
        {
            GUILayout.Label("Sesión iniciada · Atleta " + AthleteId, label);
            if (GUILayout.Button("Sincronizar pendientes", button, GUILayout.Height(52f))) nextRetry = 0;
            if (GUILayout.Button("Cerrar sesión", button, GUILayout.Height(52f)))
            { token = ""; AthleteId = 0; password = ""; Status = "Sesión cerrada"; }
        }
        else
        {
            GUILayout.Label("Nombre de usuario", label, GUILayout.Height(28f));
            username = GUILayout.TextField(username, 32, input, GUILayout.Height(38f));
            GUILayout.Label("Contraseña", label, GUILayout.Height(28f));
            password = GUILayout.PasswordField(password, '●', 128, input, GUILayout.Height(38f));
            registering = GUILayout.Toggle(registering, "Crear una cuenta", toggle, GUILayout.Height(34f));
            if (registering)
            {
                GUILayout.Label("Nombre completo", label, GUILayout.Height(28f));
                nombre = GUILayout.TextField(nombre, 120, input, GUILayout.Height(38f));
                GUILayout.Label("Federación", label, GUILayout.Height(28f));
                federacion = GUILayout.TextField(federacion, 120, input, GUILayout.Height(38f));
            }
            if (GUILayout.Button(registering ? "Registrarme" : "Iniciar sesión", button, GUILayout.Height(52f)))
                StartCoroutine(Authenticate());
        }
        GUI.enabled = previousEnabled;
        GUILayout.EndArea();
    }
}
