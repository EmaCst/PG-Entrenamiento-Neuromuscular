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

    [Header("Dificultad")]
    [SerializeField] private float initialLifetime = 2.5f;
    [SerializeField] private float minimumLifetime = 0.75f;
    [SerializeField] private float lifetimeStep = 0.25f;
    [SerializeField] private int hitsToIncrease = 5;
    [SerializeField] private int missesToDecrease = 3;
    [SerializeField] private float detectionMemorySeconds = 0.20f;
    [SerializeField] private float respawnDelay = 0.25f;
    [SerializeField] private float minimumReactionSeconds = 0.15f;

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
    private Coroutine targetTimer;
    private FootDetection? leftDetection;
    private FootDetection? rightDetection;
    private float leftSeenAt;
    private float rightSeenAt;

    public FootExerciseStats Stats => stats;
    public bool IsReady => detector != null && detector.IsReady;

    private void Awake()
    {
        if (projectionCamera == null) projectionCamera = Camera.main;
        if (stats == null) stats = GetComponent<FootExerciseStats>();
        currentLifetime = initialLifetime;
        for (int i = 0; i < targets.Length; i++) targets[i]?.Configure(i);
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
        TryContact(FootSide.Left, leftDetection, leftSeenAt);
        if (!resolving) TryContact(FootSide.Right, rightDetection, rightSeenAt);
    }

    private void OnDetectionsUpdated(IReadOnlyList<FootDetection> detections)
    {
        if (detections.Count >= 2)
        {
            leftDetection = detections[0];
            rightDetection = detections[1];
            leftSeenAt = rightSeenAt = Time.time;
        }
        else if (detections.Count == 1)
        {
            FootDetection detection = detections[0];
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
    }

    private void TryContact(FootSide side, FootDetection? detection, float seenAt)
    {
        if (side != requiredFoot || !detection.HasValue) return;
        if (Time.time - targetActivatedAt < minimumReactionSeconds) return;
        if (Time.time - seenAt > detectionMemorySeconds) return;
        FootTargetZone zone = targets[currentTarget];
        if (zone != null && zone.ContainsViewportPoint(projectionCamera, detection.Value.ContactPoint))
        {
            ResolveTarget(true, detection.Value.confidence);
        }
    }

    public void StartExercise()
    {
        if (targets == null || targets.Length < 3 || detector == null || stats == null)
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
        int nextTarget = Random.Range(0, targets.Length);
        if (targets.Length > 1 && nextTarget == previousTarget)
        {
            nextTarget = (nextTarget + Random.Range(1, targets.Length)) % targets.Length;
        }
        currentTarget = nextTarget;
        previousTarget = currentTarget;
        requiredFoot = Random.value < 0.5f ? FootSide.Left : FootSide.Right;
        Color color = requiredFoot == FootSide.Left ? Color.blue : Color.red;
        targets[currentTarget].SetState(true, color);
        targetActivatedAt = Time.time;
        if (instructionText != null)
            instructionText.text = requiredFoot == FootSide.Left ? "Pie izquierdo" : "Pie derecho";
        targetTimer = StartCoroutine(TargetTimeout());
    }

    private IEnumerator TargetTimeout()
    {
        yield return new WaitForSeconds(currentLifetime);
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
            scoreText.text = $"Aciertos: {stats.Hits}  Fallos: {stats.Misses}";
    }
}
