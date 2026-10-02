using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunningExerciseManager : MonoBehaviour, INeuromuscularExercise
{
    [Header("Referencias")]
    [SerializeField] private RunningAreaCalibrator areaCalibrator;
    [SerializeField] private RunningExerciseStats stats;
    [Tooltip("Debe apuntar a la camara del visor. Luego puede asignarse la camara del XROrigin.")]
    [SerializeField] private Transform trackedHead;
    [SerializeField] private GameObject targetPrefab;

    [Header("Objetivos")]
    [SerializeField, Min(0.2f)] private float arrivalRadius = 0.6f;
    [SerializeField, Min(0f)] private float safetyMargin = 0.5f;
    [SerializeField, Min(0.2f)] private float minimumTargetDistance = 1f;
    [SerializeField, Min(0.5f)] private float maximumTargetDistance = 3f;
    [SerializeField, Min(0.5f)] private float initialMaximumDistance = 1.5f;
    [SerializeField, Min(1)] private int targetsPerDistanceIncrease = 5;
    [SerializeField, Min(0.1f)] private float distanceIncrease = 0.5f;
    [SerializeField, Min(1)] private int generationAttempts = 25;
    [SerializeField] private float targetHeightOffset = 0.02f;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Image directionArrow;

    private GameObject currentTarget;
    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private float requestedTargetDistance;
    private string requestedDirection;
    private float targetStartedAt;
    private float travelledSinceTarget;
    private bool exerciseActive;
    private bool hasTarget;

    public RunningExerciseStats Stats => stats;
    public RunningAreaCalibrator AreaCalibrator => areaCalibrator;
    public bool IsReady => areaCalibrator != null && areaCalibrator.IsCalibrated;

    private void Awake()
    {
        if (PhoneTrainingOptions.AppliesTo("TelefonoCorrer"))
        {
            int level = PhoneTrainingOptions.DifficultyLevel;
            initialMaximumDistance = PhoneTrainingOptions.RunningInitialDistance(level);
            maximumTargetDistance = PhoneTrainingOptions.RunningMaximumDistance(level);
        }

        if (trackedHead == null && Camera.main != null) trackedHead = Camera.main.transform;
        if (stats == null) stats = GetComponent<RunningExerciseStats>();
        SetTargetVisible(false);
        UpdateScoreUI();
    }

    private void Update()
    {
        if (!exerciseActive || !hasTarget || trackedHead == null) return;

        Vector3 current = Horizontal(trackedHead.position);
        travelledSinceTarget += Vector3.Distance(current, previousPosition);
        previousPosition = current;
        UpdateDirectionUI(current);

        if (Vector3.Distance(current, Horizontal(targetPosition)) <= arrivalRadius)
        {
            ReachCurrentTarget(current);
        }
    }

    public void StartExercise()
    {
        if (!ValidateSetup()) return;
        stats.ResetStats();
        exerciseActive = true;
        SpawnNextTarget(Horizontal(trackedHead.position));
    }

    public void PauseExercise()
    {
        exerciseActive = false;
        hasTarget = false;
        SetTargetVisible(false);
        if (directionArrow != null) directionArrow.gameObject.SetActive(false);
    }

    public void ResumeExercise()
    {
        if (!ValidateSetup()) return;
        exerciseActive = true;
        SpawnNextTarget(Horizontal(trackedHead.position));
    }

    public void StopExercise()
    {
        PauseExercise();
        if (instructionText != null) instructionText.text = "Ejercicio finalizado";
    }

    private bool ValidateSetup()
    {
        if (areaCalibrator == null || !areaCalibrator.IsCalibrated)
        {
            Debug.Log("RunningExerciseManager: el ejercicio comenzara cuando se delimiten las cuatro esquinas.");
            if (instructionText != null) instructionText.text = "Primero delimita el area tocando sus cuatro esquinas.";
            return false;
        }

        if (trackedHead == null || stats == null)
        {
            Debug.LogError("RunningExerciseManager: faltan Tracked Head o Running Exercise Stats.");
            return false;
        }

        EnsureTargetExists();
        return currentTarget != null;
    }

    private void EnsureTargetExists()
    {
        if (currentTarget != null) return;
        currentTarget = targetPrefab != null
            ? Instantiate(targetPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        currentTarget.name = "RunningTarget";
        if (targetPrefab == null)
        {
            currentTarget.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            Renderer renderer = currentTarget.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.1f, 0.85f, 0.25f, 1f);
            Collider collider = currentTarget.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }

    private void SpawnNextTarget(Vector3 origin)
    {
        EnsureTargetExists();
        int completedLevels = stats.TargetsReached / Mathf.Max(1, targetsPerDistanceIncrease);
        float activeMaximumDistance = Mathf.Min(
            maximumTargetDistance,
            initialMaximumDistance + completedLevels * distanceIncrease
        );
        Vector3 candidate = areaCalibrator.GetPointInside(Random.value, Random.value, safetyMargin);
        float bestDistance = Vector3.Distance(origin, Horizontal(candidate));

        for (int i = 0; i < generationAttempts; i++)
        {
            Vector3 option = areaCalibrator.GetPointInside(Random.value, Random.value, safetyMargin);
            float distance = Vector3.Distance(origin, Horizontal(option));
            if (distance >= minimumTargetDistance && distance <= activeMaximumDistance)
            {
                candidate = option;
                bestDistance = distance;
                break;
            }

            if (Mathf.Abs(distance - activeMaximumDistance) < Mathf.Abs(bestDistance - activeMaximumDistance))
            {
                candidate = option;
                bestDistance = distance;
            }
        }

        targetPosition = candidate + Vector3.up * targetHeightOffset;
        currentTarget.transform.position = targetPosition;
        SetTargetVisible(true);
        hasTarget = true;
        targetStartedAt = Time.time;
        travelledSinceTarget = 0f;
        previousPosition = origin;
        requestedTargetDistance = bestDistance;
        requestedDirection = GetDirection(origin, Horizontal(targetPosition));

        if (instructionText != null) instructionText.text = $"{bestDistance:F1} m al {requestedDirection}";
    }

    private void ReachCurrentTarget(Vector3 athletePosition)
    {
        float elapsed = Time.time - targetStartedAt;
        stats.RegisterReachedTarget(
            requestedDirection,
            requestedTargetDistance,
            elapsed,
            travelledSinceTarget,
            targetPosition
        );
        UpdateScoreUI();
        SpawnNextTarget(athletePosition);
    }

    private void UpdateDirectionUI(Vector3 athletePosition)
    {
        if (directionArrow == null || trackedHead == null) return;
        directionArrow.gameObject.SetActive(true);
        Vector3 worldDirection = Horizontal(targetPosition) - athletePosition;
        Vector3 localDirection = trackedHead.InverseTransformDirection(worldDirection.normalized);
        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        directionArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Objetivos: {(stats == null ? 0 : stats.TargetsReached)}";
    }

    private void SetTargetVisible(bool visible)
    {
        if (currentTarget != null) currentTarget.SetActive(visible);
    }

    private static Vector3 Horizontal(Vector3 value) => new Vector3(value.x, 0f, value.z);

    private static string GetDirection(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        if (angle < 22.5f || angle >= 337.5f) return "norte";
        if (angle < 67.5f) return "noreste";
        if (angle < 112.5f) return "este";
        if (angle < 157.5f) return "sureste";
        if (angle < 202.5f) return "sur";
        if (angle < 247.5f) return "suroeste";
        if (angle < 292.5f) return "oeste";
        return "noroeste";
    }
}
