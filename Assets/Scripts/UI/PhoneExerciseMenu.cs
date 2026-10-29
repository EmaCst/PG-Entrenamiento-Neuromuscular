using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Praxen menu with cached graphics and portrait navigation.</summary>
public partial class PhoneExerciseMenu : MonoBehaviour
{
    private enum MenuPage { Welcome, Home, Tutorial, Individual, Circuit, Account, Results, History, Audio }
    private enum ExerciseChoice { Hands, Feet, Running }
    private static readonly string[] DifficultyNames = { "Inicial", "Básico", "Intermedio", "Avanzado", "Intensivo" };
    private static readonly string[] ExerciseNames = { "Manos", "Pies", "Piques y giros" };
    private const float Width = 1600f;
    private const float Height = 720f;
    private static readonly Color Navy = new Color(0.012f, 0.10f, 0.23f);
    private static readonly Color Cyan = new Color(0.10f, 0.84f, 0.95f);
    private static readonly Color Orange = new Color(1f, 0.36f, 0.13f);
    private static readonly Color Muted = new Color(0.73f, 0.83f, 0.94f);

    private MenuPage page = MenuPage.Welcome;
    private MenuPage accountReturnPage;
    private int tutorialStep;
    private bool confirmLogout;
    private bool historyLoading;
    private bool historyShowSessions;
    private string historyError;
    private readonly List<SessionResultData> poseHistory = new List<SessionResultData>();
    private int historyOffset, historyRowPage;
    private bool viewingHistory;
    private PraxenApiSessionsResponse sessionsHistory;
    private int historyAccount;
    private int historyRevision;
    private static readonly string[] TutorialTitles = { "Antes de empezar", "Manos", "Pies", "Piques y giros" };
    private static readonly string[] TutorialInstructions =
    {
        "Apoya el teléfono en vertical, con la pantalla y la cámara de selfies hacia ti. Deja espacio para que se vean cabeza, manos y pies. Una persona por teléfono.\n\nInicia sesión para asociar tus resultados. Configura tus sets, duración, descanso y dificultad máxima. Quédate quieto para calibrar: después tendrás 5 segundos para prepararte. Si mueves el teléfono, pulsa Recalibrar.",
        "Manos: apoya el teléfono con la pantalla hacia ti. Aléjate hasta ver cabeza, manos y pies; mantente de frente para calibrar.\n\nSolo aparece la esfera activa. Alcanza la azul con la mano izquierda y la roja con la derecha. Si pierdes el encuadre, el tiempo se pausa.",
        "Apoya el teléfono con la cámara de selfies hacia ti. Encuadra el cuerpo completo y ambos pies, con espacio a los lados. Quédate quieto para calibrar.\n\nMantén los piques en el lugar: alcanza el objetivo azul con el pie izquierdo y el rojo con el derecho. Retira el pie y vuelve al centro antes del siguiente intento. Se mide el contacto visual del pie con el objetivo.",
        "Apoya el teléfono con la cámara de selfies hacia ti y encuadra todo el cuerpo. Ponte de frente para calibrar. Haz piques en tu sitio y gira hombros y cadera al lado indicado: izquierda azul, derecha roja.\n\nVuelve de frente después de cada giro. La siguiente indicación llega tras una espera aleatoria de 3, 4 o 5 segundos. Si se pierde el seguimiento, el tiempo se pausa. Mantén el teléfono fijo durante todo el ejercicio."
    };
    private ExerciseChoice exerciseChoice;
    private int difficulty = 2;
    private string individualSeconds = "60";
    private string individualSets = "4";
    private string individualRest = "30";
    private string repetitions = "2";
    private string handsSeconds = "60";
    private string feetSeconds = "60";
    private string runningSeconds = "60";
    private string restSeconds = "20";
    private string validationMessage;
    private SessionResultData completedSession;
    private List<PraxenResultCard> completedCards = new List<PraxenResultCard>();
    private Texture2D logo, background, panelTexture, buttonTexture, hoverTexture, cyanTexture, orangeTexture;
    private readonly List<Texture2D> ownedTextures = new List<Texture2D>();
    private GUIStyle titleStyle, brandStyle, subtitleStyle, smallStyle, labelStyle, buttonStyle, selectedStyle;
    private GUIStyle orangeStyle, tileStyle, inputStyle, panelStyle;
    private GUIStyle accountInputStyle, accountFieldLabelStyle, accountHintStyle, accountErrorStyle, accountNameStyle, accountAvatarStyle, accountSyncStyle;
    private GUIStyle resultNumberStyle, resultSubtitleStyle;

    private void Awake()
    {
        PraxenCloud.EnsureInstance();
        if (PraxenCompletedTraining.TryTake(out completedSession))
        {
            completedCards = PraxenCompletedTraining.Cards(completedSession);
            page = MenuPage.Results;
        }
        NatacionPortrait.Configure();
        logo = Resources.Load<Texture2D>("PraxenMenuLogo");
        if (logo == null) Debug.LogWarning("Praxen menu logo missing: Resources/PraxenMenuLogo.png");
    }

    private void Start()
    {
        if (Camera.main != null) return;
        GameObject cameraObject = new GameObject("Menu Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
    }

    private void OnGUI()
    {
        PraxenKeyboard.BeginGUI();
        try {EnsureStyles();DrawPortraitMenu();}
        finally {PraxenKeyboard.EndGUI();}
    }

    private void DrawHeader()
    {
        if (page != MenuPage.Account && page != MenuPage.Results && page != MenuPage.History &&
            GUI.Button(new Rect(1110f, 32f, 426f, 58f), PraxenCloud.EnsureInstance().AthleteId > 0 ? "Mi cuenta" : "Iniciar sesión", buttonStyle))
        {
            confirmLogout = false;
            accountReturnPage = page;
            PraxenKeyboard.Dismiss(); validationMessage = null; page = MenuPage.Account;
        }
        float left = page == MenuPage.Welcome ? 58f : 550f;
        if (logo != null) GUI.DrawTexture(new Rect(left, 8f, 135f, 112f), logo, ScaleMode.ScaleToFit, true);
        GUI.Label(new Rect(left + 130f, 20f, 310f, 60f), "Praxen", brandStyle);
        GUI.Label(new Rect(left + 133f, 79f, 430f, 30f), "Natación · Cámara de selfies", smallStyle);
        if (page != MenuPage.Welcome && page != MenuPage.Results && page != MenuPage.History &&
            !PraxenCloud.EnsureInstance().IsAuthenticating &&
            GUI.Button(new Rect(62f, 32f, 160f, 58f), "<  Volver", buttonStyle))
        {
            if (page == MenuPage.Account) PraxenCloud.EnsureInstance().HideAccountSecrets();
            confirmLogout = false;
            PraxenKeyboard.Dismiss();
            validationMessage = null;
            page = page == MenuPage.Account ? accountReturnPage :
                page == MenuPage.Home || page == MenuPage.Tutorial ? MenuPage.Welcome : MenuPage.Home;
        }
    }

    private void DrawResults()
    {
        if (viewingHistory && (completedSession == null || completedSession.athleteId != PraxenCloud.EnsureInstance().AthleteId))
        { completedSession=null;completedCards.Clear();page=MenuPage.Account;return; }
        GUI.Label(new Rect(65f, 135f, 1080f, 68f), viewingHistory ? "Resultados de tu sesión" : "¡Entrenamiento completado!", titleStyle);
        GUI.Label(new Rect(67f, 207f, 1050f, 48f),
            viewingHistory ? "Natación · " + DateLabel(completedSession.startedAt) : "Estos son tus resultados. ¡Buen trabajo!", subtitleStyle);

        GUI.Box(new Rect(1160f, 143f, 376f, 96f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(1188f, 153f, 325f, 32f), "DURACIÓN DE LA SESIÓN", smallStyle);
        GUI.Label(new Rect(1188f, 184f, 326f, 42f),
            PraxenCompletedTraining.Duration(completedSession), labelStyle);

        int count = completedCards.Count;
        if (count == 0)
        {
            GUI.Box(new Rect(320f, 300f, 960f, 250f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(380f, 370f, 840f, 96f),
                "No hay métricas disponibles para esta sesión.", subtitleStyle);
        }
        else
        {
            const float gap = 26f;
            float cardWidth = Mathf.Min(800f, (1470f - gap * (count - 1)) / count);
            float totalWidth = cardWidth * count + gap * (count - 1);
            float left = (Width - totalWidth) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                DrawResultCard(new Rect(left + i * (cardWidth + gap), 278f, cardWidth, 300f),
                    completedCards[i]);
            }
        }

        bool linkedAccount = completedSession != null &&
            completedSession.athleteId > 0 &&
            completedSession.athleteId == PraxenCloud.EnsureInstance().AthleteId;
        GUI.Label(new Rect(65f, 595f, 1470f, 34f),
            viewingHistory ? "Resultados sincronizados de tu cuenta · Mediciones de Pose" : linkedAccount
                ? "Resultado guardado en el teléfono · Sincronización automática con Praxen"
                : "Resultado local de invitado · No está asociado a una cuenta",
            smallStyle);
        if (GUI.Button(new Rect(1000f, 638f, 535f, 62f), viewingHistory ? "<  Volver al historial" : "Volver al inicio  >", selectedStyle))
        {
            completedSession = null;
            completedCards.Clear();
            page = viewingHistory ? MenuPage.History : MenuPage.Home;
            viewingHistory = false;
        }
    }

    private void DrawResultCard(Rect area, PraxenResultCard card)
    {
        GUI.Box(area, GUIContent.none, panelStyle);
        GUI.DrawTexture(new Rect(area.x + 28f, area.y + 25f, 6f, 43f), cyanTexture);
        GUI.Label(new Rect(area.x + 49f, area.y + 24f, area.width - 73f, 44f),
            card.Title, labelStyle);
        GUI.Label(new Rect(area.x + 28f, area.y + 82f, area.width - 56f, 86f),
            card.PrimaryValue, resultNumberStyle);
        GUI.Label(new Rect(area.x + 29f, area.y + 166f, area.width - 58f, 46f),
            card.PrimaryLabel, resultSubtitleStyle);
        GUI.DrawTexture(new Rect(area.x + 28f, area.y + 214f, area.width - 56f, 2f),
            buttonTexture);
        GUI.Label(new Rect(area.x + 28f, area.y + 220f, area.width - 56f, 38f),
            card.DetailOne, accountHintStyle);
        GUI.Label(new Rect(area.x + 28f, area.y + 258f, area.width - 56f, 40f),
            card.DetailTwo, resultSubtitleStyle);
    }

    private void OpenHistory(int offset = 0)
    {
        PraxenCloud cloud = PraxenCloud.EnsureInstance();
        if (cloud.AthleteId <= 0) return;
        page = MenuPage.History; historyShowSessions = false; historyRowPage = 0;
        historyError = null; sessionsHistory = null; poseHistory.Clear();
        historyOffset = Mathf.Max(0, offset); historyAccount = cloud.AthleteId;
        historyRevision++;
        StartCoroutine(LoadHistory(historyRevision));
    }

    private System.Collections.IEnumerator LoadHistory(int revision)
    {
        PraxenCloud cloud = PraxenCloud.EnsureInstance(); historyLoading = true;
        // The current API does not filter measurementMode in its aggregates.
        // Read an explicit page and filter its raw results locally, never combine methods.
        yield return cloud.ResultsApi.GetSessions((value, error) =>
        {
            if (revision != historyRevision || historyAccount != cloud.AthleteId) return;
            historyError = error; sessionsHistory = value; poseHistory.Clear();
            if (string.IsNullOrEmpty(error) && value != null)
                poseHistory.AddRange(PraxenPoseHistory.Page(value.sesiones, historyAccount, PraxenTrainingMode.UseGlasses));
        }, offset: historyOffset);
        if (revision == historyRevision) historyLoading = false;
    }

    private void DrawHistory()
    {
        PraxenCloud cloud = PraxenCloud.EnsureInstance();
        if (historyAccount <= 0 || historyAccount != cloud.AthleteId)
        {
            historyRevision++; poseHistory.Clear(); sessionsHistory = null;
            page = MenuPage.Account; return;
        }
        GUI.Label(new Rect(65f, 133f, 1200f, 65f), "Tu historial de entrenamientos", titleStyle);
        GUI.Label(new Rect(65f, 204f, 1470f, 48f),
            "Natación · Resultados de Pose. Las mediciones anteriores se conservan por separado.", smallStyle);
        if (historyShowSessions) { DrawSessionsHistory(); return; }
        int hands=0, feet=0, turns=0;
        foreach(var session in poseHistory)
        { hands+=session.exercises.Count; feet+=session.footExercises.Count; turns+=session.runningExercises.Count; }
        GUI.Box(new Rect(65f,268f,470f,255f),GUIContent.none,panelStyle);
        GUI.Label(new Rect(97f,290f,408f,43f),"SESIONES EN ESTA PÁGINA",accountFieldLabelStyle);
        GUI.Label(new Rect(96f,359f,410f,89f),historyLoading?"…":poseHistory.Count.ToString(),resultNumberStyle);
        GUI.Label(new Rect(96f,465f,412f,42f),"Con mediciones de Pose",resultSubtitleStyle);
        GUI.Box(new Rect(560f,268f,470f,255f),GUIContent.none,panelStyle);
        GUI.Label(new Rect(592f,290f,406f,43f),"MÁS RECIENTE EN LA PÁGINA",accountFieldLabelStyle);
        GUI.Label(new Rect(592f,370f,405f,77f),poseHistory.Count>0?DateLabel(poseHistory[0].startedAt):"Sin sesiones",labelStyle);
        GUI.Label(new Rect(592f,465f,400f,42f),"Solo resultados sincronizados",smallStyle);
        GUI.Box(new Rect(1055f,268f,480f,255f),GUIContent.none,panelStyle);
        GUI.Label(new Rect(1087f,290f,415f,43f),"RESULTADOS CON POSE",accountFieldLabelStyle);
        GUI.Label(new Rect(1087f,359f,414f,89f),(hands+feet+turns).ToString(),resultNumberStyle);
        GUI.Label(new Rect(1087f,465f,420f,42f),$"Manos {hands} · Pies {feet} · Giros {turns}",resultSubtitleStyle);
        GUI.Label(new Rect(65f,546f,1470f,54f),HistoryStatus(),smallStyle);
        if(GUI.Button(new Rect(65f,625f,456f,62f),"<  Volver al perfil",buttonStyle))
        { historyRevision++; page=MenuPage.Account; }
        bool enabled=GUI.enabled; GUI.enabled=enabled && !historyLoading;
        if(GUI.Button(new Rect(548f,625f,456f,62f),"Actualizar",buttonStyle)) OpenHistory(historyOffset);
        GUI.enabled=enabled;
        if(GUI.Button(new Rect(1030f,625f,505f,62f),"Ver sesiones  >",selectedStyle)) historyShowSessions=true;
    }

    private string HistoryStatus()
    {
        if(historyLoading) return "Consultando tus resultados…";
        if(!string.IsNullOrEmpty(historyError)) return historyError;
        int shown=sessionsHistory?.sesiones?.Length ?? 0;
        return $"Se revisan {shown} sesiones (desde {historyOffset+1}) de {sessionsHistory?.total ?? 0}. " +
            "Solo se incluyen mediciones identificadas del modo seleccionado.";
    }

    private void DrawSessionsHistory()
    {
        const int rows=4;
        int start=historyRowPage*rows;
        if(poseHistory.Count==0)
            GUI.Label(new Rect(65f,300f,1470f,110f),historyLoading?"Cargando sesiones…":
                "No hay resultados de Pose en esta página. Puedes consultar otra página o realizar un entrenamiento.",subtitleStyle);
        for(int i=start;i<Mathf.Min(start+rows,poseHistory.Count);i++)
        {
            SessionResultData session=poseHistory[i]; float y=274f+65f*(i-start);
            GUI.Box(new Rect(65f,y,1470f,57f),GUIContent.none,panelStyle);
            GUI.Label(new Rect(85f,y+11f,300f,40f),DateLabel(session.startedAt),resultSubtitleStyle);
            GUI.Label(new Rect(390f,y+11f,400f,40f),session.mode=="circuit"?"Circuito · Pose":"Individual · Pose",resultSubtitleStyle);
            GUI.Label(new Rect(805f,y+11f,290f,40f),PraxenCompletedTraining.Duration(session),resultSubtitleStyle);
            if(GUI.Button(new Rect(1120f,y+5f,390f,47f),"Ver resultados  >",buttonStyle))
            { completedSession=session;completedCards=PraxenCompletedTraining.Cards(session);viewingHistory=true;page=MenuPage.Results; }
        }
        GUI.Label(new Rect(65f,540f,1470f,52f),HistoryStatus(),accountHintStyle);
        if(GUI.Button(new Rect(65f,625f,400f,62f),"<  Volver al resumen",buttonStyle)) historyShowSessions=false;
        bool enabled=GUI.enabled; GUI.enabled=enabled && !historyLoading && string.IsNullOrEmpty(historyError);
        if(historyRowPage>0)
        { if(GUI.Button(new Rect(495f,625f,490f,62f),"<  Sesiones anteriores de la lista",buttonStyle)) historyRowPage--; }
        else if(historyOffset>0 && GUI.Button(new Rect(495f,625f,490f,62f),"<  Página más reciente",buttonStyle))
        { OpenHistory(Mathf.Max(0,historyOffset-20));historyShowSessions=true; }
        if(start+rows<poseHistory.Count)
        { if(GUI.Button(new Rect(1015f,625f,520f,62f),"Más sesiones de la lista  >",selectedStyle)) historyRowPage++; }
        else if(historyOffset+(sessionsHistory?.sesiones?.Length ?? 0)<(sessionsHistory?.total ?? 0) &&
            GUI.Button(new Rect(1015f,625f,520f,62f),"Página más antigua  >",selectedStyle))
        { OpenHistory(historyOffset+20);historyShowSessions=true; }
        GUI.enabled=enabled;
    }

    private static string DateLabel(string iso)
    {
        if (System.DateTimeOffset.TryParse(iso, out System.DateTimeOffset date))
            return date.ToLocalTime().ToString("dd/MM/yyyy");
        return "Sin sesiones";
    }

    private void DrawAccount()
    {
        PraxenCloud cloud = PraxenCloud.EnsureInstance();
        if (cloud.AthleteId > 0)
        {
            DrawProfile(cloud);
            return;
        }
        GUI.Label(new Rect(65f, 160f, 550f, 130f), cloud.IsRegistering ? "Tu entrenamiento,\ntu progreso" : "Bienvenido\na Praxen", titleStyle);
        GUI.Label(new Rect(65f, 310f, 530f, 100f), "Conecta tu cuenta para guardar los resultados de tus próximas sesiones.", subtitleStyle);
        GUI.DrawTexture(new Rect(65f, 438f, 100f, 4f), cyanTexture);
        GUI.Label(new Rect(65f, 467f, 530f, 85f), "Inicia sesión antes de entrenar para vincular tus resultados a tu perfil.", smallStyle);
        GUI.Label(new Rect(65f, 570f, 530f, 72f), "¿Solo quieres probar? Puedes continuar como invitado.", accountHintStyle);
        bool canLeave = GUI.enabled;
        GUI.enabled = canLeave && !cloud.IsAuthenticating;
        if (GUI.Button(new Rect(65f, 645f, 530f, 55f), "Continuar como invitado", buttonStyle))
        {
            cloud.HideAccountSecrets(); PraxenKeyboard.Dismiss();
            page = MenuPage.Home;
        }
        GUI.enabled = canLeave;
        GUI.Box(new Rect(650f, 128f, 885f, 580f), GUIContent.none, panelStyle);
        cloud.DrawAccount(new Rect(678f, 142f, 829f, 556f),
            accountFieldLabelStyle, accountInputStyle, selectedStyle, buttonStyle, accountHintStyle, accountErrorStyle);
    }

    private void DrawProfile(PraxenCloud cloud)
    {
        GUI.Label(new Rect(65f, 141f, 1470f, 65f), "Tu perfil", titleStyle);
        GUI.Label(new Rect(65f, 199f, 1470f, 32f), "Tu información y tus resultados, en un solo lugar.", smallStyle);

        GUI.Box(new Rect(65f, 250f, 795f, 365f), GUIContent.none, panelStyle);
        string name = string.IsNullOrWhiteSpace(cloud.DisplayName) ? cloud.Username : cloud.DisplayName;
        string initial = string.IsNullOrWhiteSpace(name) ? "P" : name.Trim().Substring(0, 1).ToUpperInvariant();
        GUI.Box(new Rect(100f, 285f, 105f, 105f), GUIContent.none, selectedStyle);
        GUI.Label(new Rect(100f, 285f, 105f, 105f), initial, accountAvatarStyle);
        GUI.Label(new Rect(235f, 286f, 590f, 70f), name, accountNameStyle);
        GUI.Label(new Rect(238f, 359f, 585f, 35f), "@" + cloud.Username, smallStyle);
        GUI.DrawTexture(new Rect(100f, 424f, 725f, 2f), buttonTexture);
        GUI.Label(new Rect(100f, 451f, 725f, 35f), "FEDERACIÓN", smallStyle);
        GUI.Label(new Rect(100f, 491f, 725f, 66f),
            string.IsNullOrWhiteSpace(cloud.Federation) ? "Sin federación" : cloud.Federation, labelStyle);
        GUI.Label(new Rect(100f, 565f, 725f, 35f), "Sesión iniciada", smallStyle);

        if (GUI.Button(new Rect(100f, 632f, 760f, 64f), "Ver mis entrenamientos  >", selectedStyle))
            OpenHistory();
        GUI.Box(new Rect(890f, 250f, 645f, 292f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(922f, 273f, 581f, 42f), "Resultados", labelStyle);
        string state = cloud.IsUploading ? "Sincronizando…" : cloud.BlockedCount > 0 ? "Requiere revisión" : cloud.PendingCount == 0 ? "Todo al día" : $"{cloud.PendingCount} pendientes";
        accountSyncStyle.normal.textColor = !cloud.IsUploading && cloud.PendingCount > 0 ? Orange : Cyan;
        GUI.Label(new Rect(922f, 319f, 581f, 52f), state, accountSyncStyle);
        GUI.Label(new Rect(922f, 378f, 581f, 90f), cloud.Status, accountHintStyle);
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && !cloud.IsBusy;
        if (cloud.PendingCount > cloud.BlockedCount)
        {
            if (GUI.Button(new Rect(922f, 480f, 581f, 48f), "Sincronizar ahora", selectedStyle)) cloud.SyncNow();
        }
        else GUI.Label(new Rect(922f, 480f, 581f, 48f), cloud.BlockedCount > 0
            ? "Hay resultados que requieren revisión." : "Sincronización automática activa", smallStyle);
        if (!confirmLogout)
        {
            if (GUI.Button(new Rect(890f, 560f, 645f, 62f), "Cerrar sesión", buttonStyle)) confirmLogout = true;
        }
        else
        {
            GUI.Label(new Rect(890f, 552f, 645f, 64f), cloud.PendingCount > 0
                ? "Quedan resultados pendientes. Se conservarán para esta cuenta. ¿Cerrar sesión?"
                : "¿Quieres cerrar tu sesión en este teléfono?", accountHintStyle);
            if (GUI.Button(new Rect(890f, 625f, 310f, 58f), "Cancelar", buttonStyle)) confirmLogout = false;
            if (GUI.Button(new Rect(1220f, 625f, 315f, 58f), "Sí, cerrar sesión", orangeStyle))
            { cloud.Logout(); confirmLogout = false; }
        }
        GUI.enabled = previousEnabled;
    }

    private void DrawWelcome()
    {
        GUI.Label(new Rect(65f, 155f, 1470f, 65f), "Tu próxima sesión empieza aquí", titleStyle);
        GUI.Label(new Rect(65f, 222f, 1470f, 45f), "Entrena tu coordinación, precisión y cambios de dirección.", subtitleStyle);
        GUI.Box(new Rect(65f, 300f, 900f, 330f), GUIContent.none, panelStyle);
        DrawExerciseIcon(new Rect(100f, 350f, 150f, 150f), ExerciseChoice.Running, Cyan);
        GUI.Label(new Rect(280f, 337f, 640f, 60f), "Comenzar entrenamiento", labelStyle);
        GUI.Label(new Rect(280f, 405f, 620f, 85f), "Entrena frente a la cámara de selfies, con el teléfono apoyado.", subtitleStyle);
        if (GUI.Button(new Rect(280f, 525f, 630f, 68f), "Comenzar entrenamiento  >", selectedStyle)) page = MenuPage.Home;
        GUI.Box(new Rect(995f, 300f, 540f, 330f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(1030f, 337f, 470f, 60f), "Conoce Praxen", labelStyle);
        GUI.Label(new Rect(1030f, 405f, 470f, 85f), "Aprende a preparar y realizar cada ejercicio.", subtitleStyle);
        if (GUI.Button(new Rect(1030f, 525f, 470f, 68f), "Ver tutorial  >", buttonStyle))
        { tutorialStep = 0; page = MenuPage.Tutorial; }
    }

    private void DrawTutorial()
    {
        GUI.Label(new Rect(65f, 141f, 1470f, 65f), "Tutorial · Conoce tu entrenamiento", titleStyle);
        for (int i = 0; i < TutorialTitles.Length; i++)
            if (GUI.Button(new Rect(65f + i * 374f, 225f, 348f, 58f), TutorialTitles[i],
                tutorialStep == i ? selectedStyle : buttonStyle)) tutorialStep = i;
        GUI.Box(new Rect(65f, 310f, 1470f, 290f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(100f, 335f, 1400f, 45f), TutorialTitles[tutorialStep], labelStyle);
        GUI.Label(new Rect(100f, 392f, 1400f, 185f), TutorialInstructions[tutorialStep], smallStyle);
        if (tutorialStep > 0 && GUI.Button(new Rect(65f, 625f, 348f, 60f), "<  Anterior", buttonStyle)) tutorialStep--;
        if (tutorialStep < TutorialTitles.Length - 1)
        {
            if (GUI.Button(new Rect(1187f, 625f, 348f, 60f), "Siguiente  >", selectedStyle)) tutorialStep++;
        }
        else if (GUI.Button(new Rect(1035f, 625f, 500f, 60f), "Configurar entrenamiento  >", selectedStyle)) page = MenuPage.Home;
    }

    private void DrawHome()
    {
        GUI.Label(new Rect(64f, 138f, 1450f, 65f), "¿Qué quieres realizar?", titleStyle);
        DrawHomeCard(new Rect(64f, 226f, 722f, 392f), false);
        DrawHomeCard(new Rect(814f, 226f, 722f, 392f), true);
        GUI.Label(new Rect(65f, 641f, 980f, 45f), "Cámara de selfies · Una persona por teléfono", smallStyle);
        if (GUI.Button(new Rect(1110f, 641f, 426f, 54f), "Cómo colocar el teléfono", buttonStyle))
        { tutorialStep = 0; page = MenuPage.Tutorial; }
    }

    private void DrawHomeCard(Rect rect, bool circuit)
    {
        GUI.Box(rect, GUIContent.none, panelStyle);
        Color accent = circuit ? Orange : Cyan;
        DrawExerciseIcon(new Rect(rect.x + 34f, rect.y + 55f, 196f, 190f), circuit ? ExerciseChoice.Running : ExerciseChoice.Hands, accent);
        GUI.Label(new Rect(rect.x + 246f, rect.y + 67f, 440f, 65f), circuit ? "Circuito" : "Un ejercicio", titleStyle);
        GUI.Label(new Rect(rect.x + 246f, rect.y + 142f, 435f, 96f),
            circuit ? "Combina manos, pies y giros" : "Elige manos, pies o giros", subtitleStyle);
        if (GUI.Button(new Rect(rect.x + 246f, rect.y + 260f, 430f, 82f),
            circuit ? "Configurar circuito  >" : "Elegir ejercicio  >", circuit ? orangeStyle : selectedStyle))
        {
            page = circuit ? MenuPage.Circuit : MenuPage.Individual;
            validationMessage = null;
        }
    }

    private void DrawIndividual()
    {
        GUI.Label(new Rect(64f, 141f, 1100f, 65f), "Configura tu ejercicio", titleStyle);
        GUI.Label(new Rect(66f, 227f, 650f, 40f), "Tipo de ejercicio", labelStyle);
        for (int i = 0; i < ExerciseNames.Length; i++)
        {
            Rect tile = new Rect(65f + i * 229f, 281f, 211f, 222f);
            bool selected = (int)exerciseChoice == i;
            if (GUI.Button(tile, GUIContent.none, selected ? selectedStyle : tileStyle)) exerciseChoice = (ExerciseChoice)i;
            DrawExerciseIcon(new Rect(tile.x + 66f, tile.y + 25f, 80f, 107f), (ExerciseChoice)i, selected ? Navy : Color.white);
            GUI.Label(new Rect(tile.x, tile.y + 151f, tile.width, 64f), ExerciseNames[i], selected ? DarkCenteredLabel() : centeredLabel);
        }
        GUI.Label(new Rect(808f, 225f, 690f, 42f), "Dificultad máxima", labelStyle);
        GUI.Label(new Rect(808f, 266f, 700f, 42f), "Empieza en Inicial y progresa hasta este tope", smallStyle);
        DrawDifficulty(808f, 316f, 728f);
        GUI.Label(new Rect(808f, 409f, 170f, 52f), "Duración", labelStyle);
        individualSeconds = DrawStepper(new Rect(983f, 407f, 364f, 64f), individualSeconds, 5f, 5f);
        GUI.Label(new Rect(1367f, 414f, 170f, 45f), "segundos", smallStyle);
        GUI.Label(new Rect(808f, 482f, 330f, 40f), "Cantidad de sets", labelStyle);
        individualSets = DrawStepper(new Rect(808f, 525f, 330f, 58f), individualSets, 1f, 1f);
        GUI.Label(new Rect(1205f, 482f, 330f, 40f), "Descanso (s)", labelStyle);
        individualRest = DrawStepper(new Rect(1205f, 525f, 330f, 58f), individualRest, 0f, 5f);
        if (GUI.Button(new Rect(65f, 600f, 1471f, 64f), "Iniciar ejercicio  >", selectedStyle)) StartIndividual();
        if (string.IsNullOrEmpty(validationMessage))
            GUI.Label(new Rect(65f, 677f, 1470f, 30f), "Tendrás 5 segundos para prepararte.", smallStyle);
    }

    private void DrawCircuit()
    {
        GUI.Label(new Rect(65f, 129f, 1000f, 65f), "Configura tu circuito", titleStyle);
        GUI.Label(new Rect(65f, 210f, 330f, 42f), "Dificultad máxima", labelStyle);
        DrawDifficulty(400f, 202f, 1135f);
        GUI.Label(new Rect(65f, 266f, 1470f, 30f), "Cada ejercicio empieza en Inicial y se adapta sin superar este tope.", smallStyle);
        GUI.Label(new Rect(65f, 309f, 320f, 40f), "Sets / vueltas", labelStyle);
        repetitions = DrawStepper(new Rect(386f, 297f, 330f, 62f), repetitions, 1f, 1f);
        GUI.Label(new Rect(810f, 309f, 365f, 40f), "Descanso (segundos)", labelStyle);
        restSeconds = DrawStepper(new Rect(1205f, 297f, 330f, 62f), restSeconds, 0f, 5f);
        handsSeconds = DrawCircuitDuration(65f, "Manos", handsSeconds);
        feetSeconds = DrawCircuitDuration(565f, "Pies", feetSeconds);
        runningSeconds = DrawCircuitDuration(1065f, "Piques y giros", runningSeconds);
        if (GUI.Button(new Rect(65f, 552f, 1470f, 82f), "Iniciar circuito  >", orangeStyle)) StartCircuit();
        if (string.IsNullOrEmpty(validationMessage))
            GUI.Label(new Rect(65f, 645f, 1470f, 38f), "0 segundos desactiva un ejercicio. Preparación de 5 s antes de cada set.", smallStyle);
    }

    private string DrawCircuitDuration(float x, string exercise, string value)
    {
        GUI.Label(new Rect(x, 404f, 450f, 40f), exercise + " (segundos)", labelStyle);
        return DrawStepper(new Rect(x, 452f, 470f, 62f), value, 0f, 5f);
    }

    private void DrawDifficulty(float x, float y, float width)
    {
        float gap = 12f;
        float itemWidth = (width - gap * 4f) / 5f;
        for (int i = 0; i < DifficultyNames.Length; i++)
            if (GUI.Button(new Rect(x + i * (itemWidth + gap), y, itemWidth, 62f), DifficultyNames[i],
                difficulty == i ? selectedStyle : buttonStyle)) difficulty = i;
    }

    private string DrawStepper(Rect rect, string value, float minimum, float step)
    {
        const float buttonWidth = 64f;
        if (GUI.Button(new Rect(rect.x, rect.y, buttonWidth, rect.height), "−", buttonStyle))
            value = ChangeValue(value, -step, minimum);
        Rect field=new Rect(rect.x + buttonWidth + 8f, rect.y, rect.width - 2f * buttonWidth - 16f, rect.height);
        PraxenKeyboard.Field(field);
        value = GUI.TextField(field, value, 8, inputStyle);
        if (GUI.Button(new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height), "+", buttonStyle))
            value = ChangeValue(value, step, minimum);
        return value;
    }

    private static string ChangeValue(string text, float step, float minimum)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value)) value = minimum;
        return Mathf.Max(minimum, value + step).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void StartIndividual()
    {
        if (!TrySeconds(individualSeconds, out float duration))
        {
            validationMessage = "La duración del set debe estar entre 5 y 3600 segundos.";
            return;
        }
        if (!int.TryParse(individualSets, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sets) || sets < 1 || sets > TrainingPlan.MaximumSets ||
            !TrySeconds(individualRest, out float rest, true))
        { validationMessage = "Sets entre 1 y 100; descanso entre 0 y 3600 segundos."; return; }
        PhoneTrainingOptions.ConfigureIndividual(SceneForChoice(exerciseChoice), difficulty, duration, sets, rest);
        SceneManager.LoadScene(PraxenTrainingMode.Resolve(SceneForChoice(exerciseChoice)), LoadSceneMode.Single);
    }

    private void StartCircuit()
    {
        if (!int.TryParse(repetitions, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cycles) || cycles < 1 || cycles > TrainingPlan.MaximumSets ||
            !TryCircuitSeconds(handsSeconds, out float hands) || !TryCircuitSeconds(feetSeconds, out float feet) ||
            !TryCircuitSeconds(runningSeconds, out float running) || !TrySeconds(restSeconds, out float rest, true))
        {
            validationMessage = "Sets 1–100; duraciones 0 (desactivado) o 5–3600 s; descanso 0–3600 s.";
            return;
        }
        if (hands + feet + running == 0f)
        { validationMessage = "Activa al menos un ejercicio del circuito."; return; }
        PhoneTrainingOptions.ConfigureCircuit(difficulty, cycles, hands, feet, running, rest);
        SceneManager.LoadScene("SesionCombinada", LoadSceneMode.Single);
    }

    private static string SceneForChoice(ExerciseChoice choice)
    {
        return choice == ExerciseChoice.Hands ? "TelefonoManos" : choice == ExerciseChoice.Feet ? "TelefonoPies" : "TelefonoCorrer";
    }

    private static bool TrySeconds(string text, out float value, bool allowZero = false)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            (allowZero ? TrainingPlan.ValidRest(value) : TrainingPlan.ValidSeconds(value));
    }

    private static bool TryCircuitSeconds(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && TrainingPlan.ValidSeconds(value, true);
    }

    private GUIStyle darkCenteredLabel, centeredLabel;
    private GUIStyle DarkCenteredLabel()
    {
        if (darkCenteredLabel == null)
        {
            darkCenteredLabel = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 27 };
            darkCenteredLabel.normal.textColor = Navy;
        }
        return darkCenteredLabel;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        background = MakeBackground();
        panelTexture = MakeRounded(new Color(0.027f, 0.18f, 0.36f));
        buttonTexture = MakeRounded(new Color(0.055f, 0.24f, 0.43f));
        hoverTexture = MakeRounded(new Color(0.08f, 0.34f, 0.53f));
        cyanTexture = MakeRounded(Cyan);
        orangeTexture = MakeRounded(Orange);
        titleStyle = TextStyle(40, Color.white, FontStyle.Bold);
        brandStyle = TextStyle(52, Color.white, FontStyle.BoldAndItalic);
        subtitleStyle = TextStyle(30, Muted);
        smallStyle = TextStyle(25, Muted);
        labelStyle = TextStyle(30, Color.white, FontStyle.Bold);
        centeredLabel = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 27 };
        buttonStyle = ButtonStyle(buttonTexture, Color.white, 25);
        selectedStyle = ButtonStyle(cyanTexture, Navy, 27);
        orangeStyle = ButtonStyle(orangeTexture, Navy, 27);
        tileStyle = ButtonStyle(panelTexture, Color.white, 27);
        panelStyle = ButtonStyle(panelTexture, Color.white, 25);
        panelStyle.padding=new RectOffset(22,22,20,20);
        inputStyle = ButtonStyle(buttonTexture, Color.white, 34);
        inputStyle.fontStyle = FontStyle.Bold;
        accountInputStyle = new GUIStyle(inputStyle)
        { fontSize = 25, fontStyle = FontStyle.Normal, wordWrap = false,
            alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 16, 5, 5) };
        accountInputStyle.focused.background = hoverTexture;
        accountFieldLabelStyle = TextStyle(23, Color.white, FontStyle.Bold);
        accountHintStyle = TextStyle(22, Muted);
        accountErrorStyle = TextStyle(23, new Color(1f, 0.65f, 0.54f));
        accountNameStyle = TextStyle(38, Color.white, FontStyle.Bold);
        accountSyncStyle = TextStyle(36, Cyan, FontStyle.Bold);
        accountAvatarStyle = new GUIStyle(labelStyle)
        { fontSize = 52, alignment = TextAnchor.MiddleCenter };
        accountAvatarStyle.normal.textColor = Navy;
        resultNumberStyle = TextStyle(73, Cyan, FontStyle.Bold);
        resultSubtitleStyle = TextStyle(22, Muted);

    }

    private GUIStyle TextStyle(int size, Color color, FontStyle fontStyle = FontStyle.Normal)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, wordWrap = true, richText = false };
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle ButtonStyle(Texture2D texture, Color textColor, int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = size, alignment = TextAnchor.MiddleCenter, wordWrap = true,
            border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(8, 8, 6, 6)
        };
        style.normal.background = style.active.background = style.focused.background = texture;
        style.hover.background = texture == buttonTexture || texture == panelTexture ? hoverTexture : texture;
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = textColor;
        return style;
    }

    private Texture2D MakeRounded(Color color)
    {
        const int size = 64;
        const float radius = 12f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius));
                float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius));
                float distance = new Vector2(Mathf.Max(0f, dx), Mathf.Max(0f, dy)).magnitude;
                Color pixel = color;
                pixel.a = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = pixel;
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        ownedTextures.Add(texture);
        return texture;
    }

    private Texture2D MakeBackground()
    {
        Texture2D texture = new Texture2D(1, 64, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++) texture.SetPixel(0, y, Color.Lerp(Navy, new Color(0.015f, 0.18f, 0.37f), y / 63f));
        texture.Apply(false, true);
        ownedTextures.Add(texture);
        return texture;
    }

    private static void DrawExerciseIcon(Rect area, ExerciseChoice exercise, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        float unit = Mathf.Min(area.width, area.height) / 100f;
        Vector2 origin = new Vector2(area.x + (area.width - 100f * unit) * 0.5f, area.y + (area.height - 100f * unit) * 0.5f);
        if (exercise == ExerciseChoice.Hands)
        {
            GUI.DrawTexture(new Rect(origin.x + 27f * unit, origin.y + 45f * unit, 50f * unit, 45f * unit), Texture2D.whiteTexture);
            for (int i = 0; i < 4; i++)
                GUI.DrawTexture(new Rect(origin.x + (28f + i * 13f) * unit, origin.y + (i == 1 ? 10f : 18f) * unit, 10f * unit, 42f * unit), Texture2D.whiteTexture);
            IconLine(origin + new Vector2(29f, 67f) * unit, origin + new Vector2(13f, 43f) * unit, 10f * unit);
        }
        else if (exercise == ExerciseChoice.Feet)
        {
            IconLine(origin + new Vector2(17f, 76f) * unit, origin + new Vector2(87f, 76f) * unit, 9f * unit);
            IconLine(origin + new Vector2(20f, 66f) * unit, origin + new Vector2(66f, 29f) * unit, 18f * unit);
            IconLine(origin + new Vector2(66f, 29f) * unit, origin + new Vector2(84f, 60f) * unit, 18f * unit);
            IconLine(origin + new Vector2(84f, 60f) * unit, origin + new Vector2(20f, 66f) * unit, 18f * unit);
        }
        else
        {
            GUI.DrawTexture(new Rect(origin.x + 58f * unit, origin.y + 7f * unit, 15f * unit, 15f * unit), Texture2D.whiteTexture);
            IconLine(origin + new Vector2(59f, 34f) * unit, origin + new Vector2(44f, 58f) * unit, 10f * unit);
            IconLine(origin + new Vector2(59f, 34f) * unit, origin + new Vector2(78f, 47f) * unit, 8f * unit);
            IconLine(origin + new Vector2(58f, 34f) * unit, origin + new Vector2(31f, 35f) * unit, 8f * unit);
            IconLine(origin + new Vector2(44f, 58f) * unit, origin + new Vector2(20f, 86f) * unit, 9f * unit);
            IconLine(origin + new Vector2(44f, 58f) * unit, origin + new Vector2(68f, 69f) * unit, 9f * unit);
            IconLine(origin + new Vector2(68f, 69f) * unit, origin + new Vector2(58f, 94f) * unit, 9f * unit);
        }
        GUI.color = previous;
    }

    private static void IconLine(Vector2 start, Vector2 end, float thickness)
    {
        Matrix4x4 previous = GUI.matrix;
        Vector2 delta = end - start;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }

    private void OnDestroy()
    {
        foreach (Texture2D texture in ownedTextures) if (texture != null) Destroy(texture);
    }
}






