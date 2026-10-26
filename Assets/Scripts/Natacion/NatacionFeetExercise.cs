using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NatacionPoseSource), typeof(FootExerciseStats))]
public sealed class NatacionFeetExercise : MonoBehaviour, INeuromuscularExercise, INatacionExercise
{
    [Header("Tiempo disponible por objetivo (ajustar en pruebas)")]
    [SerializeField, Min(.5f)] private float initialLifetime = 1.8f;
    [SerializeField, Min(.3f)] private float minimumLifetime = .8f;
    [SerializeField, Min(.05f)] private float lifetimeStep = .25f;
    [SerializeField, Min(0)] private float respawnDelay = .3f;
    [Header("Distancias visuales relativas a hombros; no son metros")]
    [SerializeField, Range(.4f,1.2f)] private float lateralReach = .65f;
    [SerializeField, Range(.12f,.3f)] private float targetRadius = .22f;

    private static readonly int[] Body = {0,11,12,23,24,27,28,29,30,31,32};
    private NatacionPoseSource pose;
    private FootExerciseStats stats;
    private readonly NatacionFootAttempt attempt = new NatacionFootAttempt();
    private readonly Vector2[] targets = new Vector2[2];
    private Vector2 referenceHip, candidateHip, candidateLeft, candidateRight;
    private float width, torso, radius, stable, lost, wait;
    private double lastCapture = -1;
    private int sequence = -1, target = -1, level, hitStreak, missStreak;
    private bool calibrated, usable, locked, candidate, active;
    private Texture2D disc;
    private GUIStyle label, button;
    private string guidance = "Apoya el teléfono y encuadra cabeza y ambos pies.";
    public bool TrackingReady => calibrated && usable && !locked && pose.IsFresh;
    public bool IsReady => TrackingReady;
    private float Lifetime => Mathf.Max(minimumLifetime, initialLifetime - level*lifetimeStep);

    private void Awake()
    {
        pose = GetComponent<NatacionPoseSource>(); stats = GetComponent<FootExerciseStats>();
        if (Camera.main == null)
        {
            var obj = new GameObject("Selfie Camera", typeof(Camera), typeof(AudioListener));
            obj.tag = "MainCamera";
            var camera = obj.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        }
        minimumLifetime = Mathf.Min(minimumLifetime,initialLifetime);
        disc = new Texture2D(64,64,TextureFormat.RGBA32,false);
        var pixels = new Color[4096];
        for(int y=0;y<64;y++) for(int x=0;x<64;x++)
        {
            float distance = Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));
            pixels[y*64+x] = new Color(1,1,1,Mathf.Clamp01(32-distance));
        }
        disc.SetPixels(pixels); disc.Apply(false,true);
    }
    private Vector2 P(int i) => new Vector2(1-pose.Points[i].x,pose.Points[i].y);
    private Vector2 Foot(int side) => (P(29+side)+P(31+side))*.5f;
    private bool FullBody()
    {
        foreach(int i in Body) if(!pose.Visible(i)) return false;
        // Do not use a long hallucinated heel/toe segment as a contact area.
        float scale = calibrated ? width : Mathf.Abs(P(11).x-P(12).x);
        for(int side=0;side<2;side++)
        {
            Vector2 a=P(29+side), b=P(31+side); a.x*=pose.Aspect; b.x*=pose.Aspect;
            if(Vector2.Distance(a,b)>scale*pose.Aspect*.8f) return false;
        }
        return true;
    }
    private void Lose(float dt)
    {
        usable=false; stable=0; candidate=false; attempt.Suspend();
        if(calibrated && (lost+=dt)>1f) locked=true;
    }
    private void Update()
    {
        if(!pose.IsFresh) { Lose(Time.unscaledDeltaTime); return; }
        if(sequence==pose.Sequence) return;
        sequence=pose.Sequence;
        float dt=lastCapture<0?0:Mathf.Clamp((float)(pose.CapturedAt-lastCapture),0,.35f);
        lastCapture=pose.CapturedAt;
        if(!FullBody()) { guidance="Muestra el cuerpo y ambos pies completos. Tiempo en pausa."; Lose(dt); return; }
        Vector2 hip=(P(23)+P(24))*.5f;
        float currentWidth=Mathf.Abs(P(11).x-P(12).x);
        float currentTorso=Mathf.Abs(hip.y-(P(11).y+P(12).y)*.5f);
        if(!calibrated && !locked)
        {
            bool still=candidate && Vector2.Distance(hip,candidateHip)<.025f &&
                Vector2.Distance(Foot(0),candidateLeft)<.02f && Vector2.Distance(Foot(1),candidateRight)<.02f;
            stable=still?stable+dt:0;
            if(!still) { candidateHip=hip; candidateLeft=Foot(0); candidateRight=Foot(1); }
            candidate=true;
            guidance="Ponte de frente, con pies separados, y mantente quieto para calibrar.";
            if(stable>=1 && currentWidth>.055f && currentTorso>.08f)
            {
                Vector2 left=Foot(0), right=Foot(1);
                if(Mathf.Abs(left.x-right.x)<currentWidth*.35f) { guidance="Separa un poco los pies."; return; }
                float sign=Mathf.Sign(left.x-right.x);
                float y=Mathf.Max(left.y,right.y);
                float r=currentWidth*pose.Aspect*targetRadius;
                Vector2 a=new Vector2(left.x+sign*currentWidth*lateralReach,y);
                Vector2 b=new Vector2(right.x-sign*currentWidth*lateralReach,y);
                if(!Fits(a,r) || !Fits(b,r))
                { guidance="Aléjate o ajusta el teléfono: falta espacio a los lados o debajo de los pies."; return; }
                targets[0]=a; targets[1]=b; radius=r; width=currentWidth; torso=currentTorso;
                referenceHip=hip; calibrated=true;
            }
        }
        if(!calibrated) return;
        // Allow vertical piques and lateral reach, but reject large relocation/scale change.
        bool same=Mathf.Abs(hip.x-referenceHip.x)<width*1.2f &&
            Mathf.Abs(hip.y-referenceHip.y)<torso*.65f &&
            currentWidth>width*.65f && currentWidth<width*1.4f &&
            currentTorso>torso*.65f && currentTorso<torso*1.4f;
        if(!same || locked) { guidance="Vuelve a la posición inicial o pulsa Recalibrar."; Lose(dt); return; }
        usable=true; lost=0;
        if(!active || TrainingTransitionScreen.IsVisible) { attempt.Suspend(); return; }
        if(target<0)
        {
            // Both feet must leave both zones before another stimulus can appear.
            if(Overlaps(0,0,1.25f)||Overlaps(0,1,1.25f)||Overlaps(1,0,1.25f)||Overlaps(1,1,1.25f))
            { wait=respawnDelay; guidance="Retira el pie del objetivo y sigue con los piques."; return; }
            wait-=dt;
            if(wait<=0)
            {
                target=Random.Range(0,2);
                attempt.Begin(Time.realtimeSinceStartupAsDouble,Lifetime);
            }
            return;
        }
        bool inside=Overlaps(target,target,1);
        bool outside=!Overlaps(target,target,1.25f);
        var outcome=attempt.Observe(pose.CapturedAt,TrackingReady,inside,outside);
        if(outcome==NatacionFootAttempt.Outcome.None) return;
        bool hit=outcome==NatacionFootAttempt.Outcome.Hit;
        float confidence=Mathf.Min(pose.Points[27+target].confidence,
            Mathf.Min(pose.Points[29+target].confidence,pose.Points[31+target].confidence));
        stats.Register(target,target==0?FootSide.Left:FootSide.Right,hit,attempt.Elapsed,confidence);
        if(hit)
        {
            hitStreak++; missStreak=0;
            if(hitStreak>=5) { level=Mathf.Min(level+1,PhoneTrainingOptions.MaximumDifficultyLevel); hitStreak=0; }
        }
        else
        {
            missStreak++; hitStreak=0;
            if(missStreak>=3) { level=Mathf.Max(0,level-1); missStreak=0; }
        }
        target=-1; wait=respawnDelay; attempt.Suspend();
    }
    private bool Fits(Vector2 p,float r) => p.x-r/pose.Aspect>.02f &&
        p.x+r/pose.Aspect<.98f && p.y-r>.02f && p.y+r<.98f;
    private bool Overlaps(int side,int zone,float multiplier)
    {
        Vector2 a=P(29+side),b=P(31+side),c=targets[zone];
        a.x*=pose.Aspect;b.x*=pose.Aspect;c.x*=pose.Aspect;
        return NatacionPoseGeometry.SegmentTouchesCircle(a.x,a.y,b.x,b.y,c.x,c.y,
            (radius+width*pose.Aspect*.04f)*multiplier);
    }
    public void StartExercise() { stats.ResetStats(); level=hitStreak=missStreak=0; ResumeExercise(); }
    public void ResumeExercise() { active=true;target=-1;wait=respawnDelay;attempt.Suspend(); }
    public void PauseExercise() { active=false;target=-1;attempt.Suspend(); }
    public void StopExercise() => PauseExercise();
    private void OnDisable() => PauseExercise();
    private void OnDestroy() { if(disc!=null) Destroy(disc); }
    private void OnGUI()
    {
        if(TrainingTransitionScreen.IsVisible) return;
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,wordWrap=true};
            button=new GUIStyle(GUI.skin.button);
        }
        label.fontSize=Mathf.Clamp(Screen.height/28,18,32); button.fontSize=Mathf.Clamp(Screen.height/32,16,28);
        Rect safe=Screen.safeArea;safe.y=Screen.height-safe.yMax;
        string message=pose.HasError?pose.Status:!pose.IsFresh?(pose.Sequence>0?"Seguimiento perdido. Recupera el encuadre completo.":pose.Status):locked?
            "Recupera el encuadre y pulsa Recalibrar.":!TrackingReady?guidance:!active?
            "Encuadre listo. Prepárate.":target>=0?
            $"Pie {(target==0?"izquierdo · azul":"derecho · rojo")} · {Lifetime:0.0} s":
            "Mantén los piques y vuelve al centro entre objetivos.";
        string timer=ExerciseSetRunner.ActiveRunner!=null?ExerciseSetRunner.ActiveRunner.HudText:
            CombinedExerciseSession.Instance!=null?CombinedExerciseSession.Instance.HudText:"";
        GUI.Box(new Rect(safe.x+12,safe.y+10,safe.width-24,Mathf.Max(80,Screen.height*.15f)),
            message+"\n"+$"Aciertos: {stats.Hits} · Fallos: {stats.Misses} · Nivel: {level+1}  "+timer,label);
        if(calibrated && pose.IsFresh)
        {
            Rect video=pose.VideoRect;
            for(int i=0;i<2;i++)
            {
                float diameter=radius*video.height*2;Vector2 point=targets[i];
                Color saved=GUI.color;
                GUI.color=i==0?new Color(.1f,.65f,1f,i==target?1:.18f):new Color(1,.2f,.12f,i==target?1:.18f);
                GUI.DrawTexture(new Rect(video.x+point.x*video.width-diameter/2,
                    video.y+point.y*video.height-diameter/2,diameter,diameter),disc);GUI.color=saved;
            }
        }
        if(GUI.Button(new Rect(safe.x+12,safe.yMax-58,140,46),"Salir",button))
        {
            ExerciseSetRunner.ActiveRunner?.Cancel();
            if(CombinedExerciseSession.Instance!=null) Destroy(CombinedExerciseSession.Instance.gameObject);
            SceneManager.LoadScene("MenuTelefono");
        }
        if(GUI.Button(new Rect(safe.xMax-220,safe.yMax-58,208,46),"Recalibrar",button))
        {
            calibrated=usable=locked=candidate=false;stable=lost=0;target=-1;wait=respawnDelay;attempt.Suspend();
        }
    }
}
