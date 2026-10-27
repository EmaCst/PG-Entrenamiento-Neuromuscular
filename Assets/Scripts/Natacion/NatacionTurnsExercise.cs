using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NatacionPoseSource), typeof(RunningExerciseStats))]
public sealed class NatacionTurnsExercise : MonoBehaviour, INeuromuscularExercise, INatacionExercise
{
    [Header("Umbrales estimados de Pose; validar con la fisio")]
    [SerializeField, Range(25,60)] private float turnDegrees = 40;
    [SerializeField, Range(2,5)] private float initialResponseSeconds = 3;
    [SerializeField] private bool invertDirection;
    private static readonly int[] Body = {0,11,12,23,24,27,28};
    private NatacionPoseSource pose;
    private RunningExerciseStats stats;
    private readonly NatacionTurnAttempt attempt = new NatacionTurnAttempt();
    private readonly NatacionTurnCadence cadence = new NatacionTurnCadence();
    private float referenceShoulders, referenceHips, candidateShoulders, candidateHips;
    private float shoulders, hips, stable, referenceTorso, referenceWidth;
    private Vector2 referenceCenter, candidateCenter;
    private double lastCapture = -1;
    private int sequence = -1, level, hits, misses;
    private bool calibrated, usable, candidate, active, left, neutral;
    private string guidance = "Apoya el teléfono y encuadra todo tu cuerpo de frente.";
    private GUIStyle label, cue, button;
    public bool TrackingReady => calibrated && usable && pose.IsFresh;
    public bool IsReady => TrackingReady && neutral;
    private float ResponseSeconds => Mathf.Max(2,initialResponseSeconds-level*.25f);
    private int SetIndex => ExerciseSetRunner.ActiveRunner != null ? ExerciseSetRunner.ActiveRunner.CurrentSet :
        CombinedExerciseSession.Instance != null ? CombinedExerciseSession.Instance.CurrentSet : 1;

    private void Awake()
    {
        pose=GetComponent<NatacionPoseSource>(); stats=GetComponent<RunningExerciseStats>();
        stats.MeasurementMode=NatacionMeasurement.Turns;
        if(Camera.main==null)
        {
            var obj=new GameObject("Selfie Camera",typeof(Camera),typeof(AudioListener)); obj.tag="MainCamera";
            var camera=obj.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        }
        ResetCadence();
    }
    private void ResetCadence() => cadence.Reset(Random.Range(3,6));
    private Vector2 P(int i) => new Vector2(pose.Points[i].x,pose.Points[i].y);
    private bool Axis(int a,int b,out float angle)
    {
        var p=pose.WorldPoints[a];var q=pose.WorldPoints[b];
        float x=q.x-p.x,z=q.z-p.z;
        float length=x*x+z*z;
        angle=Mathf.Atan2(z,x)*Mathf.Rad2Deg;
        return !float.IsNaN(angle) && !float.IsInfinity(angle) && length>.01f && length<1;
    }
    private bool Measure(out float s,out float h)
    {
        s=h=0;
        if(!pose.IsFresh || !pose.HasWorldPose) return false;
        foreach(int i in Body) if(!pose.Visible(i)) return false;
        return Axis(11,12,out s) && Axis(23,24,out h);
    }
    private void Lose()
    {
        usable=neutral=candidate=false;stable=0;
        Finish(attempt.Interrupt(Time.realtimeSinceStartupAsDouble));
        ResetCadence();
    }
    private void Update()
    {
        if(!pose.IsFresh) { Lose(); guidance="Seguimiento perdido. Vuelve de frente al encuadre; tiempo en pausa."; return; }
        if(sequence==pose.Sequence) return;
        sequence=pose.Sequence;
        float dt=lastCapture<0?0:Mathf.Clamp((float)(pose.CapturedAt-lastCapture),0,.35f);
        lastCapture=pose.CapturedAt;
        if(!Measure(out float s,out float h))
        { Lose(); guidance="Muestra cabeza, hombros, cadera y pies. Tiempo en pausa."; return; }
        Vector2 center=(P(23)+P(24))*.5f;
        float torso=Mathf.Abs(center.y-(P(11).y+P(12).y)*.5f);
        if(!calibrated)
        {
            // Baseline must be frontal, not merely a stable profile pose.
            bool frontal=Mathf.Abs(Mathf.DeltaAngle(180,s))<25 && Mathf.Abs(Mathf.DeltaAngle(180,h))<25;
            bool still=candidate && frontal && Mathf.Abs(Mathf.DeltaAngle(candidateShoulders,s))<6 &&
                Mathf.Abs(Mathf.DeltaAngle(candidateHips,h))<6 && Vector2.Distance(center,candidateCenter)<.025f;
            stable=still?stable+dt:0;
            if(!still) { candidateShoulders=s;candidateHips=h;candidateCenter=center; }
            candidate=frontal;
            guidance="Ponte de frente y quédate quieto un segundo para calibrar.";
            if(stable<1 || torso<.08f) return;
            referenceShoulders=s;referenceHips=h;referenceCenter=center;referenceTorso=torso;
            referenceWidth=Mathf.Abs(P(11).x-P(12).x);calibrated=true;
        }
        if(Mathf.Abs(center.x-referenceCenter.x)>referenceWidth*1.2f ||
            Mathf.Abs(center.y-referenceCenter.y)>referenceTorso*.7f || torso<referenceTorso*.6f || torso>referenceTorso*1.5f)
        { Lose(); guidance="Vuelve a tu posición o pulsa Recalibrar si moviste el teléfono."; return; }
        float sign=invertDirection?-1:1;
        shoulders=(float)NatacionTurnAttempt.Delta(referenceShoulders,s)*sign;
        hips=(float)NatacionTurnAttempt.Delta(referenceHips,h)*sign;
        usable=true;neutral=NatacionTurnAttempt.Neutral(shoulders,hips);
        if(!active || TrainingTransitionScreen.IsVisible) return;
        if(attempt.Active)
        {
            Finish(attempt.Observe(pose.CapturedAt,true,shoulders,hips));
            return;
        }
        if(cadence.Observe(dt,true,neutral))
        {
            left=Random.Range(0,2)==0;
            attempt.Begin(Time.realtimeSinceStartupAsDouble,left,ResponseSeconds,turnDegrees);
            stats.CueIssued();
        }
    }
    private void Finish(NatacionTurnAttempt.Outcome outcome)
    {
        if(outcome==NatacionTurnAttempt.Outcome.None) return;
        string name=outcome==NatacionTurnAttempt.Outcome.Completed?"completed":
            outcome==NatacionTurnAttempt.Outcome.Timeout?"timeout":"interrupted";
        stats.Register(left?"left":"right",name,SetIndex,turnDegrees,0,
            (float)attempt.Progress,(float)attempt.Elapsed,(float)attempt.MovementOnset);
        if(outcome==NatacionTurnAttempt.Outcome.Completed)
        {
            misses=0;
            if(++hits>=5) { level=Mathf.Min(level+1,PhoneTrainingOptions.MaximumDifficultyLevel);hits=0; }
        }
        else if(outcome==NatacionTurnAttempt.Outcome.Timeout)
        {
            hits=0;
            if(++misses>=3) { level=Mathf.Max(0,level-1);misses=0; }
        }
        ResetCadence();
    }
    public void StartExercise() { stats.ResetStats();level=hits=misses=0;ResumeExercise(); }
    public void ResumeExercise() { active=true;ResetCadence(); }
    public void PauseExercise() { active=false;Finish(attempt.Interrupt(Time.realtimeSinceStartupAsDouble));ResetCadence(); }
    public void StopExercise() => PauseExercise();
    private void OnDisable() => PauseExercise();
    private void OnApplicationPause(bool paused) { if(paused) Lose(); }
    private void OnGUI()
    {
        if(TrainingTransitionScreen.IsVisible) return;
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,wordWrap=true};
            cue=new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button);
        }
        label.fontSize=Mathf.Clamp(Screen.height/30,18,30);cue.fontSize=Mathf.Clamp(Screen.width/17,28,64);
        button.fontSize=Mathf.Clamp(Screen.height/32,16,28);
        Rect safe=Screen.safeArea;safe.y=Screen.height-safe.yMax;
        string timer=ExerciseSetRunner.ActiveRunner!=null?ExerciseSetRunner.ActiveRunner.HudText:
            CombinedExerciseSession.Instance!=null?CombinedExerciseSession.Instance.HudText:"";
        string message=pose.HasError?pose.Status:!TrackingReady?guidance:!active?(neutral?"Encuadre listo. Prepárate.":"Vuelve de frente para comenzar el set."):
            attempt.Active?"Gira hombros y cadera hacia el lado indicado":
            !cadence.Waiting?"Vuelve de frente y mantén los piques":"Mantén los piques · espera la indicación";
        NatacionTrainingHud.Draw("Piques y giros",message,
            $"Giros: {stats.CompletedTurns} · Fallos: {stats.Misses} · Interrumpidos: {stats.InterruptedTurns}",timer);
        if(active && attempt.Active && TrackingReady)
        {
            Color saved=GUI.backgroundColor;
            GUI.backgroundColor=left?new Color(.1f,.65f,1):new Color(1,.2f,.12f);
            GUI.Box(new Rect(safe.center.x-safe.width*.32f,safe.center.y-65,safe.width*.64f,130),
                left?"← IZQUIERDA":"DERECHA →",cue);GUI.backgroundColor=saved;
        }
        if(GUI.Button(new Rect(safe.x+12,safe.yMax-58,140,46),"Salir",button))
        {
            StopExercise();ExerciseSetRunner.ActiveRunner?.Cancel();
            if(CombinedExerciseSession.Instance!=null) Destroy(CombinedExerciseSession.Instance.gameObject);
            SceneManager.LoadScene("MenuTelefono");
        }
        if(GUI.Button(new Rect(safe.xMax-220,safe.yMax-58,208,46),"Recalibrar",button))
        { Lose();calibrated=false;lastCapture=-1; }
    }
}
