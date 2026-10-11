using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FootExerciseManager : MonoBehaviour, INeuromuscularExercise
{
    [Header("Referencias")]
    [SerializeField] private FootDetectorSentis detector;
    [SerializeField] private FootTargetZone[] targets;
    [SerializeField] private FootExerciseStats stats;
    [SerializeField] private Camera projectionCamera;
    [Header("Prueba de alfombrilla unica")]
    [SerializeField] private bool useSingleMatReference = true;
    [SerializeField, Min(0.01f)] private float matDiameterMeters = 0.14f;
    private bool matContactArmed;
    private Texture2D matDisc;

    [Header("Dificultad")]
    [SerializeField] private float initialLifetime = 2.5f;
    [SerializeField] private float minimumLifetime = 0.75f;
    [SerializeField] private float lifetimeStep = 0.25f;
    [SerializeField] private int hitsToIncrease = 5;
    [SerializeField] private int missesToDecrease = 3;
    [SerializeField] private float detectionMemorySeconds = 0.20f;
    [SerializeField] private float respawnDelay = 0.25f;
    [SerializeField] private float minimumReactionSeconds = 0.15f;
    [SerializeField, Range(0f, 0.15f)] private float targetHitPadding = 0.04f;
    [SerializeField] private bool mirrorDetectionsToMatchPreview = true;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text instructionText;

    private int currentTarget = -1;
    private int previousTarget = -1;
    private FootSide requiredFoot;
    private float targetActivatedAt;
    private float currentLifetime;
    private int consecutiveHits;
    private int consecutiveMisses;
    private bool exerciseActive;
    private bool resolving;
    private bool targetHasHadDetection;
    private Coroutine targetTimer;
    private FootDetection? leftDetection;
    private FootDetection? rightDetection;
    private float leftSeenAt;
    private float rightSeenAt;

    private const float FootCueRadius = 22f;
    private Texture2D cueRing;
    private readonly List<FootDetection> displayDetections = new List<FootDetection>(2);

    public FootExerciseStats Stats => stats;
    public bool IsReady => detector != null && detector.IsReady &&
        (!useSingleMatReference || detector.HasStableMatReference);

    private Rect MatRect
    {
        get
        {
            Rect rect = detector.LatestMatRect;
            if (mirrorDetectionsToMatchPreview) rect.x = 1f - rect.xMax;
            return rect;
        }
    }

    private void Awake()
    {
        cueRing = CreateCueRing();
        matDisc = CreateMatDisc();
        if (PhoneTrainingOptions.AppliesTo("TelefonoPies"))
        {
            initialLifetime = PhoneTrainingOptions.FootTargetLifetime(PhoneTrainingOptions.DifficultyLevel);
            minimumLifetime = Mathf.Clamp(initialLifetime - 1.75f, 0.4f, 1.75f);
        }

        PhoneStereoRig stereoRig = Camera.main != null
            ? Camera.main.GetComponent<PhoneStereoRig>()
            : FindFirstObjectByType<PhoneStereoRig>();
        if (stereoRig != null && stereoRig.LeftEye != null)
            projectionCamera = stereoRig.LeftEye;
        else if (projectionCamera == null)
            projectionCamera = Camera.main;
        if (stats == null) stats = GetComponent<FootExerciseStats>();
        currentLifetime = initialLifetime;
        for (int i = 0; i < targets.Length; i++) targets[i]?.Configure(i);
        if (useSingleMatReference)
        {
            foreach (FootTargetZone target in targets) if (target != null) target.gameObject.SetActive(false);
            if (instructionText != null) instructionText.text = "Encuadra el circulo blanco de 14 cm, sin taparlo con los pies";
        }
        HideTargets();
        UpdateUI();
    }

    private void OnEnable()
    {
        if (detector != null) detector.DetectionsUpdated += OnDetectionsUpdated;
    }

    private void OnDisable()
    {
        if (detector != null) detector.DetectionsUpdated -= OnDetectionsUpdated;
    }

    private void Update()
    {
        if (!exerciseActive || resolving || currentTarget < 0) return;
        if (useSingleMatReference)
        {
            if (!detector.HasStableMatReference) return;
            FootDetection? foot = requiredFoot == FootSide.Left ? leftDetection : rightDetection;
            float seenAt = requiredFoot == FootSide.Left ? leftSeenAt : rightSeenAt;
            if (foot.HasValue && Time.time - seenAt <= detectionMemorySeconds &&
                !MatOverlapsFoot(MatRect, foot.Value.viewportRect)) matContactArmed = true;
        }
        TryContact(FootSide.Left, leftDetection, leftSeenAt);
        if (!resolving) TryContact(FootSide.Right, rightDetection, rightSeenAt);
    }

    private void OnDetectionsUpdated(IReadOnlyList<FootDetection> detections)
    {
        displayDetections.Clear();
        foreach (FootDetection detection in detections)
        {
            Rect rect = detection.viewportRect;
            if (mirrorDetectionsToMatchPreview)
                rect.x = 1f - rect.xMax;
            displayDetections.Add(new FootDetection { viewportRect = rect, confidence = detection.confidence });
        }
        displayDetections.Sort((a, b) => a.viewportRect.center.x.CompareTo(b.viewportRect.center.x));

        if (displayDetections.Count > 0 && exerciseActive && currentTarget >= 0)
            targetHasHadDetection = true;

        if (displayDetections.Count >= 2)
        {
            leftDetection = displayDetections[0];
            rightDetection = displayDetections[1];
            leftSeenAt = rightSeenAt = Time.time;
        }
        else if (displayDetections.Count == 1)
        {
            FootDetection detection = displayDetections[0];
            if (detection.viewportRect.center.x < 0.5f)
            {
                leftDetection = detection;
                leftSeenAt = Time.time;
            }
            else
            {
                rightDetection = detection;
                rightSeenAt = Time.time;
            }
        }

        if (useSingleMatReference && !exerciseActive && instructionText != null)
            instructionText.text = detector.HasStableMatReference
                ? "Alfombrilla localizada: preparate"
                : "Encuadra el circulo blanco de 14 cm, sin taparlo con los pies";
        UpdateUI();
    }

    private void TryContact(FootSide side, FootDetection? detection, float seenAt)
    {
        if (side != requiredFoot || !detection.HasValue) return;
        if (Time.time - targetActivatedAt < minimumReactionSeconds) return;
        if (Time.time - seenAt > detectionMemorySeconds) return;
        if (useSingleMatReference)
        {
            if (!matContactArmed || !detector.HasStableMatReference || detector.FrameCapturedAt < targetActivatedAt) return;
            if (MatOverlapsFoot(MatRect, detection.Value.viewportRect)) ResolveTarget(true, detection.Value.confidence);
            return;
        }
        FootTargetZone zone = targets[currentTarget];
        if (zone != null &&
            (zone.ContainsViewportPoint(projectionCamera, detection.Value.ContactPoint) ||
             zone.OverlapsDetection(projectionCamera, detection.Value.viewportRect, targetHitPadding)))
        {
            ResolveTarget(true, detection.Value.confidence);
        }
    }

    public void StartExercise()
    {
        if (targets == null || targets.Length < (useSingleMatReference ? 1 : 3) || detector == null || stats == null)
        {
            Debug.LogError("FootExerciseManager: faltan detector, estadisticas o los tres objetivos.");
            return;
        }

        stats.ResetStats();
        currentLifetime = initialLifetime;
        consecutiveHits = consecutiveMisses = 0;
        exerciseActive = true;
        ActivateNextTarget();
    }

    public void PauseExercise()
    {
        exerciseActive = false;
        resolving = false;
        if (targetTimer != null) StopCoroutine(targetTimer);
        targetTimer = null;
        HideTargets();
    }

    public void ResumeExercise()
    {
        exerciseActive = true;
        ActivateNextTarget();
    }

    public void StopExercise()
    {
        PauseExercise();
        if (instructionText != null) instructionText.text = "Ejercicio finalizado";
    }

    private void ActivateNextTarget()
    {
        if (!exerciseActive) return;
        resolving = false;
        HideTargets();
        int nextTarget = useSingleMatReference ? 0 : Random.Range(0, targets.Length);
        if (!useSingleMatReference && targets.Length > 1 && nextTarget == previousTarget)
        {
            nextTarget = (nextTarget + Random.Range(1, targets.Length)) % targets.Length;
        }
        currentTarget = nextTarget;
        previousTarget = currentTarget;
        requiredFoot = Random.value < 0.5f ? FootSide.Left : FootSide.Right;
        Color color = requiredFoot == FootSide.Left ? Color.blue : Color.red;
        if (!useSingleMatReference) targets[currentTarget].SetState(true, color);
        targetHasHadDetection = false;
        targetActivatedAt = Time.time;
        matContactArmed = false;
        if (instructionText != null)
            instructionText.text = requiredFoot == FootSide.Left ? "Pie izquierdo" : "Pie derecho";
        targetTimer = StartCoroutine(TargetTimeout());
    }

    private IEnumerator TargetTimeout()
    {
        yield return new WaitForSeconds(currentLifetime);
        if (useSingleMatReference && !detector.HasStableMatReference)
        {
            if (instructionText != null) instructionText.text = "Alfombrilla perdida: destapala y vuelve a encuadrarla";
            targetTimer = StartCoroutine(TargetTimeout());
            yield break;
        }
        if (!targetHasHadDetection)
        {
            if (instructionText != null)
                instructionText.text = "No detecto pies: ajusta el encuadre y la iluminacion";
            targetActivatedAt = Time.time;
            targetTimer = StartCoroutine(TargetTimeout());
            yield break;
        }
        ResolveTarget(false, 0f);
    }

    private void ResolveTarget(bool hit, float confidence)
    {
        if (resolving || !exerciseActive) return;
        resolving = true;
        if (targetTimer != null) StopCoroutine(targetTimer);
        targetTimer = null;
        float reaction = Time.time - targetActivatedAt;
        stats.Register(currentTarget, requiredFoot, hit, reaction, confidence);

        if (hit)
        {
            consecutiveHits++;
            consecutiveMisses = 0;
            if (consecutiveHits >= hitsToIncrease)
            {
                currentLifetime = Mathf.Max(minimumLifetime, currentLifetime - lifetimeStep);
                consecutiveHits = 0;
            }
        }
        else
        {
            consecutiveMisses++;
            consecutiveHits = 0;
            if (consecutiveMisses >= missesToDecrease)
            {
                currentLifetime = Mathf.Min(initialLifetime, currentLifetime + lifetimeStep);
                consecutiveMisses = 0;
            }
        }

        HideTargets();
        UpdateUI();
        StartCoroutine(PrepareNextTarget());
    }

    private IEnumerator PrepareNextTarget()
    {
        yield return new WaitForSeconds(respawnDelay);
        ActivateNextTarget();
    }

    private void HideTargets()
    {
        if (targets == null) return;
        foreach (FootTargetZone target in targets) target?.SetState(false, Color.white);
        currentTarget = -1;
    }

    private void UpdateUI()
    {
        if (scoreText != null && stats != null)
        {
            int detected = detector != null ? detector.LatestDetections.Count : 0;
            scoreText.text = $"Aciertos: {stats.Hits}  Fallos: {stats.Misses}  Pies detectados: {detected}";
            if (useSingleMatReference && detector != null)
                scoreText.text += detector.HasStableMatReference
                    ? (detector.MatVisible ? " | Alfombrilla visible" : " | Alfombrilla tapada: posicion temporal")
                    : " | Buscando alfombrilla";
        }
    }

    private void OnGUI()
    {
        if (useSingleMatReference && Event.current.type == EventType.Repaint && detector != null && detector.HasStableMatReference)
            DrawMatOverlay();
        if (!exerciseActive || resolving || currentTarget < 0 || Event.current.type != EventType.Repaint)
            return;

        FootDetection? requiredDetection = requiredFoot == FootSide.Left ? leftDetection : rightDetection;
        float seenAt = requiredFoot == FootSide.Left ? leftSeenAt : rightSeenAt;
        if (!requiredDetection.HasValue || Time.time - seenAt > detectionMemorySeconds)
            return;

        PhoneStereoRig stereoRig = projectionCamera != null
            ? projectionCamera.GetComponentInParent<PhoneStereoRig>()
            : null;
        Color cueColor = requiredFoot == FootSide.Left ? Color.blue : Color.red;
        if (stereoRig != null && stereoRig.IsReady)
        {
            DrawFootCue(stereoRig.LeftEye, requiredDetection.Value, cueColor);
            DrawFootCue(stereoRig.RightEye, requiredDetection.Value, cueColor);
        }
        else
        {
            DrawFootCue(projectionCamera, requiredDetection.Value, cueColor);
        }
    }

    private void DrawFootCue(Camera eyeCamera, FootDetection detection, Color color)
    {
        if (eyeCamera == null) return;

        Vector3 screen = eyeCamera.ViewportToScreenPoint(detection.viewportRect.center);
        Vector2 center = new Vector2(screen.x, Screen.height - screen.y);
        Color previousColor = GUI.color;
        GUI.color = Color.white;
        float outer = FootCueRadius + 3f;
        GUI.DrawTexture(new Rect(center.x - outer, center.y - outer, outer * 2f, outer * 2f), cueRing);
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - FootCueRadius, center.y - FootCueRadius,
            FootCueRadius * 2f, FootCueRadius * 2f), cueRing);
        GUI.color = previousColor;
    }

    private static Texture2D CreateCueRing()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude;
                float alpha = Mathf.Clamp01(Mathf.Min(size * 0.5f - distance, distance - 49f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static bool MatOverlapsFoot(Rect mat, Rect foot)
    {
        Vector2 center = mat.center;
        float x = Mathf.Clamp(center.x, foot.xMin, foot.xMax);
        float y = Mathf.Clamp(center.y, foot.yMin, foot.yMax);
        float nx = (x - center.x) / Mathf.Max(0.001f, mat.width * 0.5f);
        float ny = (y - center.y) / Mathf.Max(0.001f, mat.height * 0.5f);
        return nx * nx + ny * ny <= 1f;
    }

    private void DrawMatOverlay()
    {
        PhoneStereoRig rig = projectionCamera != null ? projectionCamera.GetComponentInParent<PhoneStereoRig>() : null;
        if (rig != null && rig.IsReady)
        {
            DrawMatForEye(rig.LeftEye);
            DrawMatForEye(rig.RightEye);
        }
        else DrawMatForEye(projectionCamera);
    }

    private void DrawMatForEye(Camera eye)
    {
        if (eye == null) return;
        Rect mat = MatRect;
        Vector3 bottom = eye.ViewportToScreenPoint(new Vector3(mat.xMin, mat.yMin, 0f));
        Vector3 top = eye.ViewportToScreenPoint(new Vector3(mat.xMax, mat.yMax, 0f));
        Rect screenRect = new Rect(bottom.x, Screen.height - top.y, top.x - bottom.x, top.y - bottom.y);
        bool active = exerciseActive && !resolving && currentTarget >= 0;
        Color color = active ? (requiredFoot == FootSide.Left ? Color.blue : Color.red) : Color.green;
        Color previous = GUI.color;
        GUI.color = new Color(color.r, color.g, color.b, 0.25f);
        GUI.DrawTexture(screenRect, matDisc);
        GUI.color = color;
        GUI.DrawTexture(screenRect, cueRing);
        GUI.color = Color.white;
        GUI.Label(new Rect(screenRect.x, screenRect.yMax + 4f, Mathf.Max(150f, screenRect.width), 28f),
            $"Alfombrilla {matDiameterMeters * 100f:0} cm");
        GUI.color = previous;
    }

    private static Texture2D CreateMatDisc()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(size * 0.5f - distance));
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDestroy()
    {
        if (cueRing != null) Destroy(cueRing);
        if (matDisc != null) Destroy(matDisc);
    }
}
