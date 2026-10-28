using UnityEngine;

public partial class PhoneExerciseMenu
{
    private Vector2 portraitScroll;
    private MenuPage portraitPage = (MenuPage)(-1);
    private const float PortraitWidth = 720;
    private void DrawPortraitMenu()
    {
        Matrix4x4 matrix=GUI.matrix;
        Color color=GUI.color,bg=GUI.backgroundColor,content=GUI.contentColor;
        GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),background);
        Rect safe=Screen.safeArea;
        float scale=Mathf.Min(safe.width/PortraitWidth,safe.height/1000f);
        if(scale<=0) { GUI.color=color;GUI.backgroundColor=bg;GUI.contentColor=content;return; }
        float height=safe.height/scale;
        GUI.matrix=Matrix4x4.TRS(new Vector3(safe.center.x-PortraitWidth*scale/2,Screen.height-safe.yMax,0),Quaternion.identity,new Vector3(scale,scale,1));
        swipeScale=scale;
        portraitViewportHeight=height-223;
        swipeViewport=new Rect(safe.center.x-PortraitWidth*scale/2+24*scale,Screen.height-safe.yMax+203*scale,672*scale,portraitViewportHeight*scale);
        bool area=false,scroll=false;
        try
        {
            if(portraitPage!=page) { if(portraitPage==MenuPage.Audio){PraxenMusic.StopPreview();PraxenAudioSettings.Save();} portraitPage=page;portraitScroll=Vector2.zero;swipe.Reset();GUI.FocusControl(null);validationMessage=null; }
            if(logo!=null) GUI.DrawTexture(new Rect(22,10,100,88),logo,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(130,14,560,66),"Praxen",brandStyle);
            GUI.Label(new Rect(132,76,560,38),PraxenTrainingMode.Label,smallStyle);
            var cloud=PraxenCloud.EnsureInstance();
            bool enabled=GUI.enabled;GUI.enabled=enabled && !cloud.IsAuthenticating;
            if(page!=MenuPage.Welcome && GUI.Button(new Rect(24,125,190,56),"< Volver",buttonStyle)) PortraitBack();
            if(page!=MenuPage.Account && page!=MenuPage.Results && GUI.Button(new Rect(402,125,294,56),cloud.AthleteId>0?"Mi cuenta":"Iniciar sesión",buttonStyle))
            { accountReturnPage=page;page=MenuPage.Account;confirmLogout=false; }
            GUI.enabled=enabled;
            SuppressSwipeClick();
            GUILayout.BeginArea(new Rect(24,203,672,height-223));area=true;
            portraitScroll=GUILayout.BeginScrollView(portraitScroll,false,false,GUIStyle.none,GUIStyle.none);scroll=true;
            switch(page)
            {
                case MenuPage.Welcome: PortraitWelcome();break;
                case MenuPage.Home: PortraitHome();break;
                case MenuPage.Tutorial: PortraitTutorial();break;
                case MenuPage.Individual: PortraitIndividual();break;
                case MenuPage.Circuit: PortraitCircuit();break;
                case MenuPage.Account: PortraitAccount();break;
                case MenuPage.Results: PortraitResults();break;
                case MenuPage.History: PortraitHistory();break;
                case MenuPage.Audio: PortraitAudio();break;
            }
            if(!string.IsNullOrEmpty(validationMessage)) GUILayout.Label(validationMessage,accountErrorStyle);
            GUILayout.Space(30);
            if(Event.current.type==EventType.Repaint)portraitContentHeight=GUILayoutUtility.GetLastRect().yMax;
        }
        finally
        {
            if(scroll) GUILayout.EndScrollView();if(area) GUILayout.EndArea();
            GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=bg;GUI.contentColor=content;
        }
    }
    private void PortraitBack()
    {
        if(page==MenuPage.Audio)PraxenAudioSettings.Save();
        var cloud=PraxenCloud.EnsureInstance();cloud.HideAccountSecrets();GUI.FocusControl(null);confirmLogout=false;
        if(page==MenuPage.Results)
        {
            completedSession=null;completedCards.Clear();page=viewingHistory?MenuPage.History:MenuPage.Home;viewingHistory=false;return;
        }
        if(page==MenuPage.History) { historyRevision++;page=MenuPage.Account;return; }
        page=page==MenuPage.Account?accountReturnPage:page==MenuPage.Home||page==MenuPage.Tutorial?MenuPage.Welcome:MenuPage.Home;
    }
    private void PTitle(string text) { GUILayout.Label(text,titleStyle);GUILayout.Space(16); }
    private void PHint(string text) { GUILayout.Label(text,smallStyle);GUILayout.Space(16); }
    private bool PButton(string text,bool primary=false) => GUILayout.Button(text,primary?selectedStyle:buttonStyle,GUILayout.MinHeight(72));
    private void PortraitWelcome()
    {
        PTitle("Tu próxima sesión empieza aquí");
        PHint(PraxenTrainingMode.UseGlasses?"Configura tu entrenamiento y coloca el teléfono en las gafas al comenzar.":"Apoya el teléfono en vertical y entrena frente a la cámara de selfies.");
        GUILayout.BeginVertical(panelStyle);
        GUILayout.Label("Comenzar entrenamiento",labelStyle);PHint("Elige un ejercicio o combina los tres en un circuito.");
        if(PButton("Configurar entrenamiento  >",true))page=MenuPage.Home;
        GUILayout.EndVertical();GUILayout.Space(24);
        GUILayout.BeginVertical(panelStyle);GUILayout.Label("¿Es tu primera vez?",labelStyle);
        PHint("Aprende a colocar el teléfono y realizar los ejercicios.");
        if(PButton("Ver tutorial  >")) {tutorialStep=0;page=MenuPage.Tutorial;}
        GUILayout.EndVertical();GUILayout.Space(20);
        if(PButton("Sonido · Voces y música"))page=MenuPage.Audio;
        GUILayout.Space(20);
        PHint(PraxenCloud.EnsureInstance().AthleteId>0?"Tu sesión está vinculada a tu cuenta.":"Inicia sesión para guardar tu progreso en tu cuenta, o prueba como invitado.");
    }
    private void PortraitHome()
    {
        PTitle("¿Qué quieres realizar?");
        PHint(PraxenTrainingMode.UseGlasses?"Usa el teléfono en horizontal dentro de las gafas al empezar.":"Una persona por teléfono. Colócalo en vertical y deja espacio para ver todo tu cuerpo.");
        if(PButton("Un ejercicio  >",true))page=MenuPage.Individual;
        GUILayout.Space(24);if(PButton("Circuito de ejercicios  >"))page=MenuPage.Circuit;
        GUILayout.Space(24);if(PButton("Cómo colocar el teléfono")){tutorialStep=0;page=MenuPage.Tutorial;}
        GUILayout.Space(24);if(PButton("Sonido · Voces y música"))page=MenuPage.Audio;
    }
    private string PortraitTutorialTitle(int step) => PraxenTrainingMode.UseGlasses && step==3 ? "Cambios de dirección" : TutorialTitles[step];
    private void PortraitTutorial()
    {
        PTitle("Conoce tu entrenamiento");
        for(int i=0;i<TutorialTitles.Length;i++)
        {
            if(PButton($"{i+1}. {PortraitTutorialTitle(i)}",tutorialStep==i))tutorialStep=i;
            GUILayout.Space(8);
        }
        GUILayout.Space(16);GUILayout.BeginVertical(panelStyle);
        GUILayout.Label(PortraitTutorialTitle(tutorialStep),labelStyle);GUILayout.Label(PraxenTrainingMode.UseGlasses?GlassesTutorial[tutorialStep]:TutorialInstructions[tutorialStep],subtitleStyle);
        GUILayout.EndVertical();GUILayout.Space(24);
        if(tutorialStep<TutorialTitles.Length-1){if(PButton("Siguiente paso  >",true))tutorialStep++;}
        else if(PButton("Configurar entrenamiento  >",true))page=MenuPage.Home;
    }
    private static readonly string[] GlassesTutorial =
    {
        "Configura el entrenamiento con el teléfono en la mano. Al iniciar, gíralo en horizontal y colócalo en las gafas. Se usa la cámara trasera y una vista por ojo. Prepárate durante los cinco segundos de cuenta atrás.",
        "Alcanza las esferas con las manos: azul con izquierda y rojo con derecha. Mantén las manos visibles para la cámara de las gafas.",
        "Coloca la alfombrilla de referencia en el suelo y encuadra los pies y la alfombrilla con la cámara de las gafas. Espera a que se detecte antes de empezar.",
        "Sigue las flechas y cambia de dirección en un espacio despejado. Los giros se miden con el giroscopio del teléfono; no se necesita delimitar el suelo."
    };
    private void PortraitModeSelector()
    {
        GUILayout.Label("Modo de uso",labelStyle);
        Rect row=GUILayoutUtility.GetRect(0,90,GUILayout.ExpandWidth(true));
        if(GUI.Button(row,GUIContent.none,panelStyle))
        {
            PraxenTrainingMode.UseGlasses=!PraxenTrainingMode.UseGlasses;
            historyRevision++;poseHistory.Clear();sessionsHistory=null;
        }
        GUI.Label(new Rect(row.x+14,row.y+24,row.width*.3f,46),"Con gafas",centeredLabel);
        GUI.Label(new Rect(row.x+row.width*.66f,row.y+24,row.width*.32f,46),"Sin gafas",centeredLabel);
        Rect track=new Rect(row.center.x-76,row.y+18,152,54);
        GUI.DrawTexture(track,buttonTexture);
        GUI.DrawTexture(new Rect(PraxenTrainingMode.UseGlasses?track.x+4:track.xMax-54,track.y+4,50,46),cyanTexture);
        GUILayout.Space(12);PHint(PraxenTrainingMode.Label+" · La selección se guarda para el próximo entrenamiento.");
    }
    private void PDifficulty()
    {
        GUILayout.Label("Dificultad máxima",labelStyle);
        for(int i=0;i<DifficultyNames.Length;i++)
        { if(PButton(DifficultyNames[i],difficulty==i))difficulty=i;GUILayout.Space(6); }
        PHint("Empieza en Inicial y se adapta sin superar el máximo elegido.");
    }
    private string PStepper(string title,string value,float minimum,float step)
    {
        GUILayout.Label(title,labelStyle);Rect rect=GUILayoutUtility.GetRect(0,66,GUILayout.ExpandWidth(true));
        string result=DrawStepper(rect,value,minimum,step);GUILayout.Space(20);return result;
    }
    private void PortraitIndividual()
    {
        PTitle("Configura tu ejercicio");PortraitModeSelector();
        for(int i=0;i<ExerciseNames.Length;i++)
        {if(PButton(i==2 && PraxenTrainingMode.UseGlasses?"Cambios de dirección":ExerciseNames[i],(int)exerciseChoice==i))exerciseChoice=(ExerciseChoice)i;GUILayout.Space(8);}
        GUILayout.Space(20);PDifficulty();
        individualSeconds=PStepper("Duración del set (segundos)",individualSeconds,5,5);
        individualSets=PStepper("Cantidad de sets",individualSets,1,1);
        individualRest=PStepper("Descanso (segundos)",individualRest,0,5);
        PHint("Tras encuadrarte tendrás 5 segundos para prepararte.");
        if(PButton("Iniciar ejercicio  >",true))StartIndividual();
    }
    private void PortraitCircuit()
    {
        PTitle("Configura tu circuito");PortraitModeSelector();PDifficulty();
        repetitions=PStepper("Sets / vueltas",repetitions,1,1);
        restSeconds=PStepper("Descanso (segundos)",restSeconds,0,5);
        PHint("Pon 0 segundos para desactivar un ejercicio.");
        handsSeconds=PStepper("Manos · segundos por set",handsSeconds,0,5);
        feetSeconds=PStepper("Pies · segundos por set",feetSeconds,0,5);
        runningSeconds=PStepper(PraxenTrainingMode.UseGlasses?"Cambios de dirección · segundos por set":"Piques y giros · segundos por set",runningSeconds,0,5);
        if(PButton("Iniciar circuito  >",true))StartCircuit();
    }
    private void PortraitAccount()
    {
        var cloud=PraxenCloud.EnsureInstance();
        if(cloud.AthleteId<=0)
        {
            PTitle(cloud.IsRegistering?"Crea tu cuenta":"Bienvenido a Praxen");
            PHint("Inicia sesión con tu usuario para vincular tus próximos entrenamientos.");
            Rect form=GUILayoutUtility.GetRect(0,cloud.RequiresPasswordChange?680:(cloud.IsRegistering?910:610),GUILayout.ExpandWidth(true));
            cloud.DrawAccount(form,accountFieldLabelStyle,accountInputStyle,selectedStyle,buttonStyle,accountHintStyle,accountErrorStyle);
            bool enabled=GUI.enabled;GUI.enabled=enabled && !cloud.IsAuthenticating;
            if(PButton("Continuar como invitado")){cloud.HideAccountSecrets();page=MenuPage.Home;}
            GUI.enabled=enabled;return;
        }
        PTitle("Tu perfil");GUILayout.BeginVertical(panelStyle);
        GUILayout.Label(string.IsNullOrWhiteSpace(cloud.DisplayName)?cloud.Username:cloud.DisplayName,accountNameStyle);
        PHint("@"+cloud.Username);GUILayout.Label("Federación",labelStyle);PHint(cloud.Federation);
        GUILayout.EndVertical();GUILayout.Space(20);
        if(PButton("Ver mis entrenamientos  >",true))OpenHistory();GUILayout.Space(20);
        GUILayout.Label(cloud.IsUploading?"Sincronizando…":cloud.PendingCount>0?$"{cloud.PendingCount} resultados pendientes":"Todo al día",labelStyle);
        PHint(cloud.Status);
        bool prior=GUI.enabled;GUI.enabled=prior && !cloud.IsBusy;
        if(cloud.PendingCount>cloud.BlockedCount && PButton("Sincronizar ahora"))cloud.SyncNow();
        GUILayout.Space(24);
        if(!confirmLogout){if(PButton("Cerrar sesión"))confirmLogout=true;}
        else
        {
            PHint(cloud.PendingCount>0?"Los resultados pendientes se conservarán para esta cuenta. ¿Cerrar sesión?":"¿Quieres cerrar tu sesión?");
            if(PButton("Cancelar"))confirmLogout=false;
            if(PButton("Sí, cerrar sesión")){cloud.Logout();confirmLogout=false;}
        }
        GUI.enabled=prior;
    }
    private void PortraitResults()
    {
        if(viewingHistory && (completedSession==null || completedSession.athleteId!=PraxenCloud.EnsureInstance().AthleteId))
        {completedSession=null;completedCards.Clear();page=MenuPage.Account;return;}
        PTitle(viewingHistory?"Resultados de tu sesión":"¡Entrenamiento completado!");
        PHint("Duración: "+PraxenCompletedTraining.Duration(completedSession));
        foreach(var card in completedCards)
        {
            Rect area=GUILayoutUtility.GetRect(0,305,GUILayout.ExpandWidth(true));DrawResultCard(area,card);GUILayout.Space(20);
        }
        if(completedCards.Count==0)PHint("No hay métricas disponibles.");
        bool linked=completedSession!=null && completedSession.athleteId>0 && completedSession.athleteId==PraxenCloud.EnsureInstance().AthleteId;
        PHint(viewingHistory?"Resultados sincronizados · "+PraxenTrainingMode.Label:linked?"Guardado en el teléfono. Sincronización automática.":"Resultado local de invitado, sin cuenta asociada.");
        if(PButton(viewingHistory?"Volver al historial":"Volver al inicio",true))PortraitBack();
    }
    private void PortraitHistory()
    {
        if(historyAccount<=0 || historyAccount!=PraxenCloud.EnsureInstance().AthleteId)
        {historyRevision++;poseHistory.Clear();sessionsHistory=null;page=MenuPage.Account;return;}
        PTitle("Mis entrenamientos");PHint(PraxenTrainingMode.UseGlasses?"Modo gafas · Resultados separados de Pose.":"Cámara frontal · Solo mediciones de Pose.");PHint(HistoryStatus());
        foreach(var session in poseHistory)
        {
            GUILayout.BeginVertical(panelStyle);GUILayout.Label(DateLabel(session.startedAt),labelStyle);
            PHint((session.mode=="circuit"?"Circuito":"Individual")+" · "+PraxenCompletedTraining.Duration(session));
            if(PButton("Ver resultados  >",true))
            {completedSession=session;completedCards=PraxenCompletedTraining.Cards(session);viewingHistory=true;page=MenuPage.Results;}
            GUILayout.EndVertical();GUILayout.Space(20);
        }
        if(!historyLoading && poseHistory.Count==0)PHint("No hay resultados de Pose en esta página.");
        bool enabled=GUI.enabled;GUI.enabled=enabled && !historyLoading;
        if(PButton("Actualizar"))OpenHistory(historyOffset);
        if(historyOffset>0 && PButton("Página más reciente"))OpenHistory(Mathf.Max(0,historyOffset-20));
        if(historyOffset+(sessionsHistory?.sesiones?.Length??0)<(sessionsHistory?.total??0) && PButton("Página más antigua"))OpenHistory(historyOffset+20);
        GUI.enabled=enabled;
    }
    private void PortraitAudio()
    {
        PTitle("Voces y música");
        PHint("Configura cada canal por separado. Los cambios se guardan en este teléfono.");
        GUILayout.BeginVertical(panelStyle);
        GUILayout.Label("Voces e indicaciones",labelStyle);
        PHint("Instrucciones, cuenta atrás y señales de izquierda / derecha.");
        PraxenAudioSettings.VoiceVolume=AudioLevel("Volumen de voz",PraxenAudioSettings.VoiceVolume);
        if(PButton("Probar voz"))PraxenVoice.Instance.Play("tracking_ready");
        GUILayout.EndVertical();GUILayout.Space(24);
        GUILayout.BeginVertical(panelStyle);
        GUILayout.Label("Música de entrenamiento",labelStyle);
        PHint("Base instrumental más animada. Baja automáticamente mientras habla la voz.");
        PraxenAudioSettings.MusicVolume=AudioLevel("Volumen de música",PraxenAudioSettings.MusicVolume);
        if(PButton("Probar música · 10 segundos"))PraxenMusic.Preview();
        PHint("La música se reproduce durante los ejercicios. Volumen 0 la silencia.");
        GUILayout.EndVertical();GUILayout.Space(24);
        if(PButton("Guardar y volver",true)) {PraxenAudioSettings.Save();page=MenuPage.Home;}
    }
    private float AudioLevel(string title,float value)
    {
        GUILayout.Label($"{title}: {Mathf.RoundToInt(value*100)}%",smallStyle);
        Rect area=GUILayoutUtility.GetRect(0,66,GUILayout.ExpandWidth(true));
        // Horizontal gestures adjust volume; vertical gestures scroll instead.
        return GUI.HorizontalSlider(new Rect(area.x+8,area.y+22,area.width-16,36),value,0,1);
    }

}

