using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunningExerciseManager : MonoBehaviour, INeuromuscularExercise
{
    [SerializeField] private RunningGyroSource gyroSource;
    [SerializeField] private FootCameraSource cameraSource;
    [SerializeField] private RunningExerciseStats stats;
    [SerializeField, Range(15f, 180f)] private float turnAngleDegrees = 90f;
    [SerializeField, Range(1f, 30f)] private float toleranceDegrees = 15f;
    [SerializeField, Min(0.05f)] private float stableSeconds = 0.15f;
    [SerializeField, Min(1f)] private float responseWindowSeconds = 6f;
    [SerializeField, Min(0f)] private float betweenCuesSeconds = 1.5f;
    [SerializeField] private TMP_Text scoreText, instructionText;
    [SerializeField] private Image directionArrow;
    private readonly TurnResponseEvaluator evaluation = new TurnResponseEvaluator();
    private bool active, pending, right;
    private double nextCue;
    private int discontinuities;
    private float stillSeconds, nextInstructionRefresh;
    private string status = "Preparando cámara y giroscopio…";
    private Texture2D pixel;
    public bool IsReady => gyroSource != null && gyroSource.IsReady && cameraSource != null && cameraSource.IsReady;
    public RunningExerciseStats Stats => stats;
    private void Awake()
    {
        if (gyroSource == null) gyroSource = Object.FindFirstObjectByType<RunningGyroSource>();
        if (cameraSource == null) cameraSource = Object.FindFirstObjectByType<FootCameraSource>();
        if (stats == null) stats = GetComponent<RunningExerciseStats>();
        if (directionArrow != null) directionArrow.gameObject.SetActive(false);
        if (PhoneTrainingOptions.AppliesTo("TelefonoCorrer"))
        {
            float level = (Mathf.Clamp(PhoneTrainingOptions.DifficultyLevel, 1, 5) - 1) / 4f;
            responseWindowSeconds = Mathf.Lerp(8f, 4f, level);
            betweenCuesSeconds = Mathf.Lerp(2f, 1f, level);
        }
        pixel = new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
        UpdateScore();
    }
    private void Update()
    {
        if (!active)
        {
            if (!IsReady && !TrainingTransitionScreen.IsVisible)
                SetStatus(gyroSource == null ? "No se encontró el giroscopio." : !gyroSource.IsReady ? gyroSource.Status : cameraSource == null ? "No se encontró la cámara." : cameraSource.Status);
            return;
        }
        if (!IsReady || gyroSource.Discontinuities != discontinuities)
        {
            if (pending) Finish("sensor_interrupted");
            discontinuities = gyroSource.Discontinuities; stillSeconds = 0;
            nextCue = Time.realtimeSinceStartupAsDouble + betweenCuesSeconds;
            SetStatus("Recuperando cámara y giroscopio. Mantén la cabeza estable."); return;
        }
        double now = Time.realtimeSinceStartupAsDouble;
        if (pending)
        {
            var result = evaluation.Update(gyroSource.YawDegrees, now);
            if (result == TurnResponseEvaluator.Result.Pending && Time.unscaledTime >= nextInstructionRefresh)
            {
                nextInstructionRefresh = Time.unscaledTime + 0.1f;
                double progress = evaluation.ProgressDegrees * (right ? 1 : -1);
                SetStatus($"Gira a la {(right ? "derecha" : "izquierda")} · {turnAngleDegrees:0}° · Progreso: {progress:F0}°");
            }
            if (result == TurnResponseEvaluator.Result.Completed) { Finish("completed"); SetStatus($"Giro completado · {evaluation.ElapsedSeconds:F2} s"); }
            else if (result == TurnResponseEvaluator.Result.Timeout) { Finish("timeout"); SetStatus("Tiempo agotado. Prepara el siguiente giro."); }
            else if (result == TurnResponseEvaluator.Result.Interrupted) Finish("sensor_interrupted");
            return;
        }
        stillSeconds = Mathf.Abs(gyroSource.RateDegreesPerSecond) < 15f ? stillSeconds + Time.unscaledDeltaTime : 0;
        if (now >= nextCue && stillSeconds >= 0.25f) IssueCue(now);
    }
    private void IssueCue(double now)
    {
        right = Random.value >= 0.5f;
        float angle = right ? turnAngleDegrees : -turnAngleDegrees;
        evaluation.Start(gyroSource.YawDegrees, now, angle, Mathf.Min(toleranceDegrees, turnAngleDegrees * 0.5f), stableSeconds, responseWindowSeconds);
        pending = true; stats.CueIssued();
        SetStatus($"Gira a la {(right ? "derecha" : "izquierda")} · {turnAngleDegrees:0}°");
        PraxenVoice.Instance.PlayIfAvailable(right ? "turn_right" : "turn_left");
    }
    private void Finish(string outcome)
    {
        if (!pending) return;
        if (evaluation.Outcome == TurnResponseEvaluator.Result.Pending) evaluation.Interrupt();
        int set = ExerciseSetRunner.ActiveRunner != null ? ExerciseSetRunner.ActiveRunner.CurrentSet :
            CombinedExerciseSession.Instance != null ? CombinedExerciseSession.Instance.CurrentSet : 1;
        stats.Register(right ? "right" : "left", outcome, set, right ? turnAngleDegrees : -turnAngleDegrees,
            Mathf.Min(toleranceDegrees, turnAngleDegrees * 0.5f), evaluation);
        pending = false; stillSeconds = 0; nextCue = Time.realtimeSinceStartupAsDouble + betweenCuesSeconds;
        UpdateScore();
    }
    public void StartExercise()
    {
        if (stats == null) return;
        stats.ResetStats(); active = true; pending = false; stillSeconds = 0;
        discontinuities = gyroSource != null ? gyroSource.Discontinuities : 0;
        nextCue = Time.realtimeSinceStartupAsDouble; SetStatus("Mantén la cabeza estable. Atento al cambio de dirección."); UpdateScore();
    }
    public void ResumeExercise()
    {
        active = true; pending = false; stillSeconds = 0;
        discontinuities = gyroSource != null ? gyroSource.Discontinuities : 0;
        nextCue = Time.realtimeSinceStartupAsDouble;
        SetStatus("Atento al siguiente cambio de dirección.");
    }
    public void PauseExercise() { Finish("set_ended"); active = false; }
    public void StopExercise() { PauseExercise(); SetStatus("Ejercicio finalizado"); }
    private void OnDisable() { if (pending && stats != null) Finish("sensor_interrupted"); active = false; }
    private void OnDestroy() { if (pixel != null) Destroy(pixel); }
    private void SetStatus(string text) { if (status == text) return; status = text; if (instructionText != null) instructionText.text = text; }
    private void UpdateScore() { if (scoreText != null && stats != null) scoreText.text = $"Giros: {stats.CompletedTurns} · Fallos: {stats.Misses}"; }
    private void DrawLine(Vector2 from, Vector2 to, float width)
    {
        Matrix4x4 saved = GUI.matrix;
        Vector2 delta = to - from;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);
        GUI.DrawTexture(new Rect(from.x,from.y-width*0.5f,delta.magnitude,width),pixel);
        GUI.matrix = saved;
    }
    private void OnGUI()
    {
        if (!active || !pending || TrainingTransitionScreen.IsVisible || pixel == null) return;
        Color saved = GUI.color; GUI.color = new Color(0.1f,0.85f,0.95f);
        for (int eye = 0; eye < 2; eye++)
        {
            float spacing = PhoneViewerSettings.Enabled ? PhoneViewerSettings.LensSpacing : 0f;
            float vertical = PhoneViewerSettings.Enabled ? PhoneViewerSettings.VerticalOffset : 0f;
            float x = Screen.width * ((eye == 0 ? 0.25f : 0.75f) + (eye == 0 ? -spacing : spacing) * 0.25f);
            float y = Screen.height * (0.48f - vertical * 0.5f);
            float size = Mathf.Min(Screen.width * 0.065f, Screen.height * 0.13f), sign = right ? 1f : -1f;
            DrawLine(new Vector2(x-size*0.8f,y), new Vector2(x+size*0.8f,y), size*0.18f);
            Vector2 tip = new Vector2(x+sign*size*0.8f,y);
            DrawLine(tip, tip+new Vector2(-sign*size*0.55f,-size*0.45f),size*0.18f);
            DrawLine(tip, tip+new Vector2(-sign*size*0.55f,size*0.45f),size*0.18f);
        }
        GUI.color = saved;
    }
}
