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
    [SerializeField, Min(0.01f)] private float matDiameterMeters = 0.30f;
    private readonly FootContactGate contactGate = new FootContactGate();
    private FootIdentityTracker identityTracker = new FootIdentityTracker();
    private int matGeneration;
    private float lastObservationAt = float.NegativeInfinity;
    private float targetElapsed;
    private bool contactDataUsable;
    private Texture2D matDisc;
    private FootStereoBackground cameraBackground;

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
            return ToDisplayRect(detector.LatestMatRect);
        }
    }

    private void Awake()
    {
        cameraBackground = detector != null ? detector.GetComponent<FootStereoBackground>() : null;
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
        if (targets != null) for (int i = 0; i < targets.Length; i++) targets[i]?.Configure(i);
        if (useSingleMatReference)
        {
            if (targets != null) foreach (FootTargetZone target in targets) if (target != null) target.gameObject.SetActive(false);
            if (instructionText != null) instructionText.text = "Encuadra el circulo blanco de 30 cm, sin taparlo con los pies";
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
        PauseExercise();
        contactGate.Reset();
        identityTracker = new FootIdentityTracker();
        lastObservationAt = float.NegativeInfinity;
    }

    // Contacts are evaluated only when a NEW camera observation is published.
    private void OnDetectionsUpdated(IReadOnlyList<FootDetection> detections)
    {
        displayDetections.Clear();
        foreach (FootDetection detection in detections)
        {
            Rect rect = ToDisplayRect(detection.viewportRect);
            displayDetections.Add(new FootDetection { viewportRect = rect, confidence = detection.confidence });
        }
        displayDetections.Sort((a, b) => a.viewportRect.center.x.CompareTo(b.viewportRect.center.x));

        float capturedAt = detector.FrameCapturedAt;
        if (capturedAt <= lastObservationAt) return;
        float sampleDelta = Mathf.Clamp(capturedAt - lastObservationAt, 0f, 0.35f);
        bool previousUsable = contactDataUsable;
        lastObservationAt = capturedAt;
        identityTracker.Assign(displayDetections.Count > 0 ? displayDetections[0].viewportRect : default,
            displayDetections.Count > 1 ? displayDetections[1].viewportRect : default,
            displayDetections.Count, capturedAt, out int leftIndex, out int rightIndex);
        // Missing tracks cannot borrow an old bounding box from a different camera frame.
        leftDetection = leftIndex >= 0 ? displayDetections[leftIndex] : (FootDetection?)null;
        rightDetection = rightIndex >= 0 ? displayDetections[rightIndex] : (FootDetection?)null;
        if (leftDetection.HasValue) leftSeenAt = capturedAt;
        if (rightDetection.HasValue) rightSeenAt = capturedAt;
        FootDetection? required = requiredFoot == FootSide.Left ? leftDetection : rightDetection;
        bool fresh = Time.time - capturedAt <= 0.35f;
        contactDataUsable = fresh && required.HasValue &&
            (!useSingleMatReference || detector.FrameHasMatReference);
        if (matGeneration != detector.MatGeneration)
        {
            matGeneration = detector.MatGeneration;
            contactGate.Reset();
        }
        if (exerciseActive && !resolving && currentTarget >= 0 && capturedAt >= targetActivatedAt)
        {
            if (contactDataUsable)
            {
                targetHasHadDetection = true;
                if (previousUsable) targetElapsed += Mathf.Min(sampleDelta, capturedAt - targetActivatedAt);
            }
            if (useSingleMatReference)
            {
                bool inside = required.HasValue && FootContactGate.Overlaps(MatRect, required.Value.viewportRect);
                bool outside = required.HasValue && !FootContactGate.Overlaps(MatRect, required.Value.viewportRect, 1.2f);
                if (contactGate.Observe(capturedAt, contactDataUsable,
                    inside && capturedAt - targetActivatedAt >= minimumReactionSeconds, outside))
                    ResolveTarget(true, required.Value.confidence);
            }
            else
            {
                TryContact(FootSide.Left, leftDetection, leftSeenAt);
                if (!resolving) TryContact(FootSide.Right, rightDetection, rightSeenAt);
            }
        }

        if (useSingleMatReference && !exerciseActive && instructionText != null)
            instructionText.text = detector.HasStableMatReference
                ? "Alfombrilla localizada: preparate"
                : "Encuadra el circulo blanco de 30 cm, sin taparlo con los pies";
        UpdateUI();
    }

    private void TryContact(FootSide side, FootDetection? detection, float seenAt)
    {
        if (side != requiredFoot || !detection.HasValue) return;
        if (Time.time - targetActivatedAt < minimumReactionSeconds) return;
        if (Time.time - seenAt > Mathf.Max(detectionMemorySeconds, 0.35f)) return;
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
        if ((!useSingleMatReference && (targets == null || targets.Length < 3)) || detector == null || stats == null)
        {
            Debug.LogError("FootExerciseManager: faltan detector, estadisticas o los tres objetivos.");
            return;
        }

        stats.ResetStats();
        identityTracker = new FootIdentityTracker();
        leftDetection = rightDetection = null;
        currentLifetime = initialLifetime;
        consecutiveHits = consecutiveMisses = 0;
        exerciseActive = true;
        ActivateNextTarget();
    }

    public void PauseExercise()
    {
        exerciseActive = false;
        resolving = false;
        StopAllCoroutines();
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
        contactGate.Reset();
        targetElapsed = 0f;
        contactDataUsable = false;
        if (instructionText != null)
            instructionText.text = requiredFoot == FootSide.Left ? "Pie izquierdo" : "Pie derecho";
        targetTimer = StartCoroutine(TargetTimeout());
    }

    private IEnumerator TargetTimeout()
    {
        // Lost mat/foot/camera time is not charged as an athlete miss.
        while (exerciseActive && !resolving && currentTarget >= 0)
        {
            if (useSingleMatReference)
            {
                bool usable = contactDataUsable && Time.time - detector.FrameCapturedAt <= 0.35f;
                if (instructionText != null)
                    instructionText.text = usable
                        ? (requiredFoot == FootSide.Left ? "Pie izquierdo" : "Pie derecho")
                        : "Encuadra la alfombrilla y ambos pies";
                if (targetElapsed >= currentLifetime) { ResolveTarget(false, 0f); yield break; }
            }
            else if (Time.time - targetActivatedAt >= currentLifetime && targetHasHadDetection)
            { ResolveTarget(false, 0f); yield break; }
            yield return null;
        }
    }

    private void ResolveTarget(bool hit, float confidence)
    {
        if (resolving || !exerciseActive) return;
        resolving = true;
        if (targetTimer != null) StopCoroutine(targetTimer);
        targetTimer = null;
        float reaction = useSingleMatReference ? targetElapsed : Time.time - targetActivatedAt;
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
        if (targets != null) foreach (FootTargetZone target in targets) target?.SetState(false, Color.white);
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
        if (TrainingTransitionScreen.IsVisible) return;
        if (useSingleMatReference && Event.current.type == EventType.Repaint && detector != null && detector.HasStableMatReference)
            DrawMatOverlay();
        if (!exerciseActive || resolving || currentTarget < 0 || Event.current.type != EventType.Repaint)
            return;

        FootDetection? requiredDetection = requiredFoot == FootSide.Left ? leftDetection : rightDetection;
        float seenAt = requiredFoot == FootSide.Left ? leftSeenAt : rightSeenAt;
        if (!requiredDetection.HasValue || Time.time - seenAt > Mathf.Max(detectionMemorySeconds, 0.35f))
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

        Rect rect = detection.viewportRect;
        if (cameraBackground != null && !cameraBackground.TryProjectDisplayRect(eyeCamera, rect, out rect)) return;
        Vector3 screen = eyeCamera.ViewportToScreenPoint(rect.center);
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

    private Rect ToDisplayRect(Rect rect)
    {
        // The legacy scene flag must not mirror a preview that is actually unmirrored.
        bool previewMirror = cameraBackground != null ? cameraBackground.MirrorHorizontally : mirrorDetectionsToMatchPreview;
        if (previewMirror ^ (detector != null && detector.InputFlipHorizontal)) rect.x = 1f - rect.xMax;
        if (detector != null && detector.InputFlipVertical) rect.y = 1f - rect.yMax;
        return rect;
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
        if (cameraBackground != null && !cameraBackground.TryProjectDisplayRect(eye, mat, out mat)) return;
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


