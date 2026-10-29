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
    public string DisplayName { get; private set; } = "";
    public string Username { get; private set; } = "";
    private const string RegistrationFederation = "Natación";
    public string Federation { get; private set; } = "";
    public int PendingCount { get; private set; }
    public int BlockedCount { get; private set; }
    public bool IsBusy => busy || uploading;
    public bool IsUploading => uploading;
    public bool IsRegistering => registering;
    public bool RequiresPasswordChange { get; private set; }
    public string Status { get; private set; } = "Inicia sesión para sincronizar resultados";
    public PraxenResultsApi ResultsApi { get; private set; }
    private int identityRevision;

    public bool TryGetAthleteCredentials(out string server, out string jwt, out int athleteId,
        out int revision)
    {
        server = baseUrl; jwt = token; athleteId = AthleteId; revision = identityRevision;
        return athleteId > 0 && !string.IsNullOrEmpty(jwt) &&
            !string.IsNullOrEmpty(server) && server.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
    public bool MatchesAthleteCredentials(string server, int athleteId, int revision)
    {
        return identityRevision == revision && AthleteId == athleteId &&
            string.Equals(baseUrl, server, StringComparison.Ordinal);
    }
    private string baseUrl = "", token = "", username = "", password = "", nombre = "";
    private bool registering, busy, uploading;
    private bool showPassword, showRecoveryHelp, authSuccess;
    private string confirmPassword = "", authMessage = "";
    private string newPassword = "", confirmNewPassword = "";
    private float authenticationStarted;
    public bool IsAuthenticating => busy;

    public void HideAccountSecrets()
    {
        showPassword = false;
        if (!busy) password = confirmPassword = newPassword = confirmNewPassword = "";
    }

    private void AuthError(string message)
    {
        authSuccess = false;
        authMessage = message;
        Status = message;
    }
    private float nextRetry;
    private int retrySeconds = 15;
    private string QueuePath => Path.Combine(Application.persistentDataPath, "praxen-results");
    [Serializable] private class Credentials { public string username, password, nombre, federacion; }
    [Serializable] private class Athlete { public int id; public string nombre, federacion; }
    [Serializable] private class User { public string nombre, username; public bool mustChangePassword; }
    [Serializable] private class PasswordChangeRequest { public string currentPassword, newPassword; }
    [Serializable] private class AuthResponse { public string token; public Athlete atleta; public User user; }
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
        ResultsApi = new PraxenResultsApi(this);
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
        RefreshPendingCount();
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
        { AuthError("No se pudo conectar con el servidor configurado."); yield break; }
        baseUrl = uri.AbsoluteUri.TrimEnd('/');
        PlayerPrefs.SetString("Praxen.ApiUrl", baseUrl); PlayerPrefs.Save();
        if (IsBusy) yield break;
        busy = true;
        authMessage = "";
        authenticationStarted = Time.unscaledTime;
        string submittedPassword=password;
        try {
            using (var request = Post(baseUrl + "/api/auth/" + (registering ? "register" : "login"),
                JsonUtility.ToJson(new Credentials { username = username, password = password, nombre = nombre, federacion = RegistrationFederation })))
            {
                request.timeout=60;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    AuthError(request.responseCode == 0 ? "No pudimos conectar. Revisa tu conexión e inténtalo de nuevo." :
                        request.responseCode == 401 ? "Usuario o contraseña incorrectos. Revisa tus datos." :
                        request.responseCode == 429 ? "Demasiados intentos. Espera un momento antes de reintentar." :
                        request.responseCode >= 500 ? "El servicio no está disponible ahora. Inténtalo más tarde." : ErrorMessage(request));
                    yield break;
                }
                AuthResponse response = null;
                try { response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text); }
                catch (Exception) { AuthError("No pudimos interpretar la respuesta. Inténtalo de nuevo."); }

                if(response!=null && !string.IsNullOrEmpty(response.token) && response.atleta==null)
                { AuthError("Esta cuenta es de administración. Usa el panel web; aquí ingresa con una cuenta de deportista.");yield break; }
                if (response == null || string.IsNullOrEmpty(response.token) || response.atleta == null || response.atleta.id <= 0)
                { AuthError("No se pudo completar el acceso. Inténtalo de nuevo."); yield break; }
                password = confirmPassword = ""; showPassword = false;
                identityRevision++;
                token = response.token; AthleteId = response.atleta.id;
                DisplayName = response.user?.nombre ?? response.atleta.nombre ?? "";
                Username = response.user?.username ?? username.Trim().ToLowerInvariant();
                Federation = response.atleta.federacion ?? "";
                RequiresPasswordChange = response.user != null && response.user.mustChangePassword;
                if (RequiresPasswordChange)
                {
                    AthleteId = 0;
                    password=submittedPassword;
                    authMessage = "Contraseña temporal. Elige una contraseña nueva antes de entrenar con tu cuenta.";
                    Status = authMessage;
                    yield break;
                }
                submittedPassword="";
                RefreshPendingCount();
                Status = "Sesión iniciada. Resultados vinculados a tu cuenta"; nextRetry = 0; retrySeconds = 15;
            }
        } finally { busy = false; }
    }
    private IEnumerator ChangeTemporaryPassword()
    {
        if (IsBusy || !RequiresPasswordChange) yield break;
        string validation=PraxenPasswordPolicy.Error(password,newPassword,confirmNewPassword);
        if(validation!=null){AuthError(validation);yield break;}
        busy = true; authMessage = ""; authenticationStarted = Time.unscaledTime;
        try
        {
            using (var request = Post(baseUrl + "/api/auth/change-password",
                JsonUtility.ToJson(new PasswordChangeRequest { currentPassword = password, newPassword = newPassword })))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if(request.responseCode==401)
                    { string saved=Username;ClearIdentity();username=saved;registering=false;AuthError("Tu acceso temporal caducó. Inicia sesión otra vez para cambiar la contraseña."); }
                    else AuthError(request.responseCode==403?"La contraseña temporal es incorrecta. Revísala e inténtalo otra vez.":ErrorMessage(request));
                    yield break;
                }
                string savedUsername = Username;
                ClearIdentity();
                username = savedUsername; registering = false;
                authSuccess=true;authMessage="Contraseña actualizada. Inicia sesión con tu contraseña nueva.";Status=authMessage;
            }
        }
        finally { showPassword=false;busy = false; }
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
                        ClearIdentity(); AuthError("Tu sesión caducó. Inicia sesión para enviar los resultados pendientes."); yield break;
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
        } finally { uploading = false; RefreshPendingCount(); }
    }
    private void ClearIdentity()
    {
        identityRevision++;
        token = ""; AthleteId = 0; RequiresPasswordChange = false;
        password = confirmPassword = newPassword = confirmNewPassword = "";
        showPassword = false; showRecoveryHelp=false;authSuccess=false;authMessage = "";
        DisplayName = Username = Federation = "";
        PendingCount = BlockedCount = 0;
    }
    public void Logout()
    {
        if (IsBusy) return;
        ClearIdentity(); username = nombre = ""; registering = false;
        Status = "Sesión cerrada";
    }
    public void SyncNow()
    {
        if (IsBusy || AthleteId <= 0) return;
        RefreshPendingCount(); nextRetry = 0;
    }
    private void RefreshPendingCount()
    {
        PendingCount = BlockedCount = 0;
        if (AthleteId <= 0) return;
        try
        {
            foreach (string file in Directory.GetFiles(QueuePath, "*.json"))
            {
                Pending pending;
                try { pending = JsonUtility.FromJson<Pending>(File.ReadAllText(file)); }
                catch (Exception) { continue; }
                if (pending == null || pending.result == null || pending.server != baseUrl || pending.athleteId != AthleteId) continue;
                PendingCount++;
                if (pending.blocked) BlockedCount++;
            }
        }
        catch (Exception) { }
    }

    // Drawn by the phone menu; all coordinates use its safe-area canvas.
    public void DrawAccount(Rect area, GUIStyle label, GUIStyle input, GUIStyle primary,
        GUIStyle secondary, GUIStyle hint, GUIStyle error)
    {
        if (RequiresPasswordChange)
        { DrawTemporaryPasswordForm(area,label,input,primary,secondary,hint,error); return; }
        if (area.width < 700)
        { DrawPortraitAccount(area,label,input,primary,secondary,hint,error);return; }
        GUI.BeginGroup(area);
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && !IsBusy;
        float width = area.width, gap = 24f, column = (width - gap) * 0.5f;
        if (GUI.Button(new Rect(0, 0, column, 54), "Iniciar sesión", !registering ? primary : secondary)) SetAuthMode(false);
        if (GUI.Button(new Rect(column + gap, 0, column, 54), "Crear cuenta", registering ? primary : secondary)) SetAuthMode(true);

        if (registering)
        {
            nombre = AccountField(new Rect(0, 72, width, 88), "Nombre completo", "auth.name", nombre, 120, label, input);
            username = AccountField(new Rect(0, 170, width, 88), "Nombre de usuario", "auth.username", username, 32, label, input);
            password = PasswordField(new Rect(0, 268, column, 88), "Contraseña", "auth.password", password, label, input);
            confirmPassword = PasswordField(new Rect(column + gap, 268, column, 88), "Repite la contraseña", "auth.confirm", confirmPassword, label, input);
        }
        else
        {
            username = AccountField(new Rect(0, 90, width, 88), "Nombre de usuario", "auth.username", username, 32, label, input);
            password = PasswordField(new Rect(0, 196, width, 88), "Contraseña", "auth.password", password, label, input);
        }
        if(!registering)GUI.Label(new Rect(0,555,width,90),"¿Olvidaste tu contraseña? Pide a tu entrenador una temporal e ingresa con ella para crear una nueva.",hint);
        float helpY = registering ? 366 : 306;
        GUI.Label(new Rect(0, helpY, width - 195, 52), registering
            ? "Usuario: 3–32 caracteres (a–z, 0–9, . _ -).\nContraseña: mínimo 8 caracteres."
            : "Usa tu nombre de usuario, no tu correo.", hint);
        if (GUI.Button(new Rect(width - 185, helpY, 185, 48), showPassword ? "Ocultar clave" : "Mostrar clave", secondary))
            showPassword = !showPassword;

        GUI.enabled = enabled;
        string message = busy ? (Time.unscaledTime - authenticationStarted > 6f
            ? "Conectando… el servicio puede tardar en responder." : registering ? "Creando tu cuenta…" : "Iniciando sesión…") : authMessage;
        GUI.Label(new Rect(0, 424, width, 60), message, busy || authSuccess ? hint : error);
        GUI.enabled = enabled && !IsBusy;
        if (GUI.Button(new Rect(0, 490, width, 62), busy ? "Conectando…" : registering ? "Crear mi cuenta" : "Entrar a Praxen", primary))
            SubmitAccount();
        GUI.enabled = enabled;
        GUI.EndGroup();
    }

    private void DrawTemporaryPasswordForm(Rect area, GUIStyle label, GUIStyle input, GUIStyle primary,
        GUIStyle secondary, GUIStyle hint, GUIStyle error)
    {
        GUI.BeginGroup(area);
        bool enabled=GUI.enabled;GUI.enabled=enabled && !IsBusy;
        float width=area.width;
        GUI.Label(new Rect(0,0,width,60),"Protege tu cuenta",label);
        GUI.Label(new Rect(0,64,width,70),"Tu entrenador restableció tu acceso. Elige una contraseña personal para continuar.",hint);
        password=PasswordField(new Rect(0,145,width,88),"Contraseña temporal","auth.temp",password,label,input);
        newPassword=PasswordField(new Rect(0,245,width,88),"Nueva contraseña","auth.new",newPassword,label,input);
        confirmNewPassword=PasswordField(new Rect(0,345,width,88),"Repite la nueva contraseña","auth.newConfirm",confirmNewPassword,label,input);
        GUI.Label(new Rect(0,441,width,60),"Al menos 8 caracteres. Debe ser distinta de la temporal.",hint);
        if(GUI.Button(new Rect(0,510,width,54),showPassword?"Ocultar contraseñas":"Mostrar contraseñas",secondary))showPassword=!showPassword;
        GUI.Label(new Rect(0,576,width,88),busy?"Guardando tu contraseña…":authMessage,busy?hint:error);
        if(GUI.Button(new Rect(0,670,width,66),busy?"Guardando…":"Guardar y volver a iniciar sesión",primary))
        {GUI.FocusControl(null);StartCoroutine(ChangeTemporaryPassword());}
        if(GUI.Button(new Rect(0,750,width,54),"Volver al inicio de sesión",secondary))
        {string saved=Username;ClearIdentity();username=saved;registering=false;Status="Inicia sesión para continuar.";}
        GUI.enabled=enabled;GUI.EndGroup();
    }

    private void DrawPortraitAccount(Rect area, GUIStyle label, GUIStyle input, GUIStyle primary,
        GUIStyle secondary, GUIStyle hint, GUIStyle error)
    {
        GUI.BeginGroup(area);
        bool enabled=GUI.enabled;GUI.enabled=enabled && !IsBusy;
        float width=area.width, half=(width-16)*.5f;
        if(GUI.Button(new Rect(0,0,half,58),"Iniciar sesión",!registering?primary:secondary))SetAuthMode(false);
        if(GUI.Button(new Rect(half+16,0,half,58),"Crear cuenta",registering?primary:secondary))SetAuthMode(true);
        float y=80;
        if(registering)
        {
            nombre=AccountField(new Rect(0,y,width,88),"Nombre completo","auth.name",nombre,120,label,input);y+=104;
        }
        username=AccountField(new Rect(0,y,width,88),"Nombre de usuario","auth.username",username,32,label,input);y+=104;
        password=PasswordField(new Rect(0,y,width,88),"Contraseña","auth.password",password,label,input);y+=104;
        if(registering)
        {confirmPassword=PasswordField(new Rect(0,y,width,88),"Repite la contraseña","auth.confirm",confirmPassword,label,input);y+=104;}
        GUI.Label(new Rect(0,y,width,68),registering?"Usuario: 3–32 caracteres (a–z, 0–9, . _ -).\nContraseña: mínimo 8 caracteres.":"Usa tu nombre de usuario, no tu correo.",hint);y+=76;
        if(GUI.Button(new Rect(0,y,width,54),showPassword?"Ocultar contraseña":"Mostrar contraseña",secondary))showPassword=!showPassword;
        y+=66;GUI.enabled=enabled;
        string message=busy?(Time.unscaledTime-authenticationStarted>6?"Conectando… el servicio puede tardar en responder.":"Conectando…"):authMessage;
        GUI.Label(new Rect(0,y,width,72),message,busy || authSuccess?hint:error);y+=80;
        GUI.enabled=enabled && !IsBusy;
        if(GUI.Button(new Rect(0,y,width,66),busy?"Conectando…":registering?"Crear mi cuenta":"Entrar a Praxen",primary))SubmitAccount();
        y+=80;
        if(!registering)
        {
            if(GUI.Button(new Rect(0,y,width,50),showRecoveryHelp?"Ocultar ayuda":"Olvidé mi contraseña",secondary))showRecoveryHelp=!showRecoveryHelp;
            if(showRecoveryHelp)GUI.Label(new Rect(0,y+60,width,120),"Pide a tu entrenador o administrador que restablezca tu contraseña. Te dará una temporal; ingresa con ella y aquí podrás elegir una nueva.",hint);
        }
        GUI.enabled=enabled;GUI.EndGroup();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) showPassword = false;
    }

    private void SetAuthMode(bool create)
    {
        if (registering == create) return;
        registering = create; authMessage = "";
        password = confirmPassword = ""; showPassword = false;
        GUI.FocusControl(null);
    }

    private static string AccountField(Rect area, string title, string control, string value,
        int limit, GUIStyle label, GUIStyle input)
    {
        GUI.Label(new Rect(area.x, area.y, area.width, 30), title, label);
        GUI.SetNextControlName(control);
        return GUI.TextField(new Rect(area.x, area.y + 34, area.width, 54), value, limit, input);
    }

    private string PasswordField(Rect area, string title, string control, string value, GUIStyle label, GUIStyle input)
    {
        GUI.Label(new Rect(area.x, area.y, area.width, 30), title, label);
        GUI.SetNextControlName(control);
        Rect field = new Rect(area.x, area.y + 34, area.width, 54);
        return showPassword ? GUI.TextField(field, value, 128, input) : GUI.PasswordField(field, value, '●', 128, input);
    }

    private bool InvalidAccount(string message, string control)
    {
        authMessage = message;
        GUI.FocusControl(control);
        return false;
    }

    private bool ValidateAccount()
    {
        username = username.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(username)) return InvalidAccount("Escribe tu nombre de usuario.", "auth.username");
        if (string.IsNullOrEmpty(password)) return InvalidAccount("Escribe tu contraseña.", "auth.password");
        if (!registering) return true;
        nombre = nombre.Trim();
        if (nombre.Length<2 || nombre.Length>120) return InvalidAccount("Escribe tu nombre completo (2–120 caracteres).", "auth.name");
        if (username.Length < 3 || !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-z0-9][a-z0-9._-]{2,31}$"))
            return InvalidAccount("El usuario debe tener 3–32 letras, números, puntos, guiones o guiones bajos.", "auth.username");
        if (password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72)
            return InvalidAccount("La contraseña debe tener al menos 8 caracteres y como máximo 72 bytes.", "auth.password");
        if (password != confirmPassword) return InvalidAccount("Las contraseñas no coinciden. Revisa la confirmación.", "auth.confirm");
        return true;
    }

    private void SubmitAccount()
    {
        if (IsBusy || !ValidateAccount()) return;
        GUI.FocusControl(null);
        showPassword = false;
        StartCoroutine(Authenticate());
    }
}


