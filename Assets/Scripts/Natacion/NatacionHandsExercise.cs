using UnityEngine;
using UnityEngine.SceneManagement;

// Screen-space contact is measured against the SAME fitted video rect as Pose.
// The calibrated 3x3 grid exists only in memory; only its active sphere is drawn.
[RequireComponent(typeof(NatacionPoseSource), typeof(AimLabStats), typeof(AimLabDifficulty))]
public sealed class NatacionHandsExercise : MonoBehaviour, INeuromuscularExercise, INatacionExercise
{
    private NatacionPoseSource pose;
    private AimLabStats stats;
    private AimLabDifficulty difficulty;
    private readonly Vector2[] targets = new Vector2[9];
    private static readonly int[] BodyPoints = { 0, 11, 12, 15, 16, 23, 24, 27, 28, 31, 32 };
    private static readonly int[,] HandSegments = { {15,17}, {15,19}, {15,21}, {16,18}, {16,20}, {16,22} };
    private Vector2 hip, candidateHip, previousHip;
    private float shoulderWidth, torsoHeight, radius, stableTime, lostTime;
    private double lastCapture = -1;
    private readonly NatacionHandAttempt attempt = new NatacionHandAttempt();
    private int lastSequence = -1, target = -1, previousTarget = -1, hits, misses;
    private float waitElapsed;
    private bool statsRunning;
    private bool active, calibrated, usable, wasUsable, candidate, lockedOut;
    private RequiredHand required;
    private Texture2D disc;
    private GUIStyle text, button;
    private string guidance = "Apoya el teléfono y encuadra el cuerpo completo.";
    public bool IsReady => calibrated && TrackingReady;
    public bool TrackingReady => usable && pose.IsFresh &&
        (target < 0 ? PalmReliable(RequiredHand.Left) && PalmReliable(RequiredHand.Right) : PalmReliable(required));
    public static bool SingleView => NatacionPoseSource.SingleView;

    private void Awake()
    {
        if (Camera.main == null)
        {
            var cameraObject = new GameObject("Selfie Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        }
        pose = GetComponent<NatacionPoseSource>(); stats = GetComponent<AimLabStats>();
        difficulty = GetComponent<AimLabDifficulty>(); difficulty.stats = stats;
        difficulty.ConfigureMaximum(PhoneTrainingOptions.MaximumDifficultyLevel);
        disc = new Texture2D(64,64,TextureFormat.RGBA32,false);
        var pixels = new Color[64*64];
        for (int y=0;y<64;y++) for(int x=0;x<64;x++)
        {
            float d = Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));
            float light = Mathf.Lerp(1f, .65f, Mathf.Clamp01(d / 32));
            pixels[y*64+x] = new Color(light,light,light,Mathf.Clamp01(32-d));
        }
        disc.SetPixels(pixels); disc.Apply(false,true);
    }
    private Vector2 P(int i) => new Vector2(1 - pose.Points[i].x, pose.Points[i].y);
    private bool FullBody()
    {
        foreach (int i in BodyPoints) if (!pose.Visible(i)) return false;
        return true;
    }
    private void LateUpdate()
    {
        bool track = active && TrackingReady && !TrainingTransitionScreen.IsVisible;
        if (track == statsRunning) return;
        if (track) stats.ResumeDifficultyTracking(difficulty.currentLevel); else stats.StopDifficultyTracking();
        statsRunning = track;
    }
    private void Update()
    {
        if (!pose.IsFresh)
        {
            usable = wasUsable = false; attempt.Suspend(); stableTime = 0; candidate = false;
            if (calibrated && (lostTime += Time.unscaledDeltaTime) > 1f) lockedOut = true;
            return;
        }
        if (pose.Sequence == lastSequence) return;
        lastSequence = pose.Sequence;
        float dt = lastCapture < 0 ? 0 : Mathf.Clamp((float)(pose.CapturedAt-lastCapture),0,.2f);
        lastCapture = pose.CapturedAt;
        bool full = FullBody();
        if (!full) guidance = "Encuadra cabeza, manos y pies. Se necesita ver el cuerpo completo.";
        Vector2 currentHip = (P(23)+P(24))*.5f;
        float width = Mathf.Abs(P(11).x-P(12).x);
        float height = Mathf.Abs(currentHip.y-(P(11).y+P(12).y)*.5f);
        if (full && !calibrated && !lockedOut)
        {
            bool stable = candidate && Vector2.Distance(currentHip,candidateHip) < .025f;
            stableTime = stable ? stableTime + dt : 0;
            if (!stable) candidateHip = currentHip;
            candidate = true;
            guidance = "Ponte de frente, con los brazos separados, y mantén la posición.";
            if (stableTime >= 1f && width > .055f && height > .08f)
            {
                float left = currentHip.x-width*1.3f, right = currentHip.x+width*1.3f;
                float top = (P(11).y+P(12).y)*.5f-height*.35f, bottom = currentHip.y+height*.12f;
                radius = width*pose.Aspect*.23f; // radius in units of image height
                if (left-radius/pose.Aspect < .02f || right+radius/pose.Aspect > .98f || top-radius < .02f || bottom+radius > .98f)
                { guidance = "Aléjate un poco: falta espacio para alcanzar los objetivos."; stableTime = 0; }
                else
                {
                    for(int row=0;row<3;row++) for(int col=0;col<3;col++)
                        targets[row*3+col] = new Vector2(Mathf.Lerp(left,right,col*.5f),Mathf.Lerp(top,bottom,row*.5f));
                    hip = previousHip = currentHip; shoulderWidth = width; torsoHeight = height;
                    calibrated = true; guidance = "Encuadre listo";
                }
            }
        }
        bool samePosition = calibrated && Vector2.Distance(currentHip,hip) < shoulderWidth*.7f &&
            width > shoulderWidth*.65f && width < shoulderWidth*1.4f &&
            height > torsoHeight*.7f && height < torsoHeight*1.4f;
        if (calibrated && full && Vector2.Distance(currentHip,previousHip) > shoulderWidth*.55f) lockedOut = true;
        if (full) previousHip = currentHip;
        usable = full && samePosition && !lockedOut;
        if (!usable)
        {
            if (calibrated && !samePosition) guidance = "Vuelve a tu posición o pulsa Recalibrar.";
            if (calibrated && (lostTime += dt) > 1f) lockedOut = true;
            stableTime = full ? stableTime : 0; if (!full) candidate = false;
            wasUsable = false; attempt.Suspend();
            return;
        }
        lostTime = 0;
        if (!active || TrainingTransitionScreen.IsVisible) { wasUsable = false; attempt.Suspend(); return; }
        if (!wasUsable)
        {
            wasUsable = true; attempt.Suspend();
            stats.ResumeDifficultyTracking(difficulty.currentLevel); statsRunning = true;
            return;
        }
        if (target < 0)
        {
            if (!TrackingReady) return;
            waitElapsed -= dt;
            if (waitElapsed <= 0) ActivateTarget();
            return;
        }
        bool reliable = PalmReliable(required);
        var outcome = attempt.Observe(pose.CapturedAt, reliable,
            required == RequiredHand.Left && TouchesTarget(required, targets[target]),
            required == RequiredHand.Right && TouchesTarget(required, targets[target]));
        if (outcome == NatacionHandAttempt.Outcome.Hit)
        {
            hits++; stats.RegisterHit(required,attempt.Elapsed); difficulty.RegisterHit(); Resolve();
        }
        else if (outcome == NatacionHandAttempt.Outcome.Miss)
        {
            misses++; stats.RegisterMiss(required); difficulty.RegisterMiss(); Resolve();
        }
    }
    private bool PalmReliable(RequiredHand hand)
    {
        int first = hand == RequiredHand.Left ? 0 : 3;
        int reliable = 0;
        for (int i=first; i<first+3; i++)
            if (ReliableSegment(HandSegments[i,0], HandSegments[i,1])) reliable++;
        return reliable >= 2;
    }
    private bool ReliableSegment(int a, int b)
    {
        if (!pose.Visible(a) || !pose.Visible(b)) return false;
        Vector2 start=P(a), end=P(b); start.x*=pose.Aspect; end.x*=pose.Aspect;
        // Reject stretched/hallucinated palm landmarks instead of enlarging contact.
        return Vector2.Distance(start,end) <= shoulderWidth*pose.Aspect*.45f;
    }
    private bool TouchesTarget(RequiredHand hand, Vector2 location)
    {
        int first = hand == RequiredHand.Left ? 0 : 3;
        Vector2 center = location; center.x *= pose.Aspect;
        for (int i=first;i<first+3;i++)
        {
            int a = HandSegments[i,0], b = HandSegments[i,1];
            if (!ReliableSegment(a,b)) continue;
            Vector2 start=P(a), end=P(b); start.x*=pose.Aspect; end.x*=pose.Aspect;
            if (NatacionPoseGeometry.SegmentTouchesCircle(start.x,start.y,end.x,end.y,center.x,center.y,radius+shoulderWidth*pose.Aspect*.06f)) return true;
        }
        return false;
    }
    private void ActivateTarget()
    {
        required = Random.value < .5f ? RequiredHand.Left : RequiredHand.Right;
        if (!PalmReliable(required)) return;
        // Do not spawn beneath a resting hand and award an automatic hit.
        int start = Random.Range(0,9);
        for (int offset=0; offset<9; offset++)
        {
            int choice = (start+offset)%9;
            if (choice == previousTarget || TouchesTarget(required,targets[choice])) continue;
            target=previousTarget=choice;
            attempt.Begin(required == RequiredHand.Left, Time.realtimeSinceStartupAsDouble, difficulty.TargetLifetime);
            return;
        }
    }
    private void Resolve() { target=-1; attempt.Suspend(); waitElapsed=difficulty.RespawnDelay; }
    public void StartExercise()
    {
        difficulty.ConfigureMaximum(PhoneTrainingOptions.MaximumDifficultyLevel);
        stats.Initialize(difficulty.currentLevel); hits=misses=0; ResumeExercise();
    }
    public void ResumeExercise()
    {
        active=true; target=-1; waitElapsed=0; wasUsable=false;
        stats.ResumeDifficultyTracking(difficulty.currentLevel); statsRunning = true;
    }
    public void PauseExercise()
    { active=false; target=-1; attempt.Suspend(); stats.StopDifficultyTracking(); statsRunning=false; }
    public void StopExercise() => PauseExercise();
    private void OnGUI()
    {
        if (text == null)
        {
            text = new GUIStyle(GUI.skin.box) { alignment=TextAnchor.MiddleCenter, wordWrap=true, fontSize=24 };
            button = new GUIStyle(GUI.skin.button) { fontSize=22 };
        }
        text.fontSize=Mathf.Clamp(Screen.height/26,18,34);
        button.fontSize=Mathf.Clamp(Screen.height/32,16,28);
        Rect safe=Screen.safeArea; safe.y=Screen.height-safe.yMax;
        if (TrainingTransitionScreen.IsVisible) return;
        string message = pose.HasError ? pose.Status : !pose.IsFresh ? (pose.Sequence > 0 ? "Seguimiento perdido. Recupera el encuadre completo." : pose.Status) : lockedOut ? "Recupera tu posición y pulsa Recalibrar." :
            !IsReady ? (usable ? (target >= 0 ? $"Muestra completa la mano {(required == RequiredHand.Left ? "izquierda" : "derecha")}. Tiempo en pausa." : "Muestra ambas manos completas. Tiempo en pausa.") : guidance) : active && target >= 0 ? $"Mano {(required == RequiredHand.Left ? "izquierda · azul" : "derecha · roja")}   |   Aciertos: {hits}   Fallos: {misses}" : active ? $"Atento al siguiente objetivo | Aciertos: {hits} Fallos: {misses}" : "Encuadre listo. Prepárate.";
        string timer = ExerciseSetRunner.ActiveRunner != null ? ExerciseSetRunner.ActiveRunner.HudText : CombinedExerciseSession.Instance != null ? CombinedExerciseSession.Instance.HudText : "";
        GUI.Box(new Rect(safe.x+12,safe.y+10,safe.width-24,Mathf.Max(70,Screen.height*.12f)),message+"\n"+timer,text);
        if (active && usable && pose.IsFresh && target>=0)
        {
            Rect video=pose.VideoRect; Vector2 uv=targets[target]; float diameter=radius*video.height*2;
            Color saved=GUI.color; GUI.color=required==RequiredHand.Left ? new Color(.1f,.65f,1f) : new Color(1f,.2f,.12f);
            GUI.DrawTexture(new Rect(video.x+uv.x*video.width-diameter/2,video.y+uv.y*video.height-diameter/2,diameter,diameter),disc);
            GUI.color=saved;
        }
        // Small wrist dots help verify alignment without covering the participant.
        for(int i=15;i<=16;i++) if(pose.Visible(i))
        {
            Vector2 p=pose.DisplayPoint(i); Color saved=GUI.color;
            GUI.color=i==15?Color.cyan:Color.red; GUI.DrawTexture(new Rect(p.x-5,p.y-5,10,10),disc); GUI.color=saved;
        }
        float by=safe.yMax-60;
        if (GUI.Button(new Rect(safe.x+12,by,150,48),"Salir",button))
        {
            ExerciseSetRunner.ActiveRunner?.Cancel();
            if (CombinedExerciseSession.Instance != null) Destroy(CombinedExerciseSession.Instance.gameObject);
            SceneManager.LoadScene("MenuTelefono");
        }
        if (GUI.Button(new Rect(safe.xMax-220,by,208,48),"Recalibrar",button))
        {
            calibrated=usable=wasUsable=candidate=lockedOut=false; stableTime=lostTime=0;
            target=-1; attempt.Suspend(); waitElapsed=0;
        }
    }
    private void OnDisable() { if(stats!=null) PauseExercise(); }
    private void OnDestroy() { if(disc!=null) Destroy(disc); }
}
