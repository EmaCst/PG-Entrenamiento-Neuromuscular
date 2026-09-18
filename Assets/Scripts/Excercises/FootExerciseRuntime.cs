using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

public enum RequiredFoot
{
    Left,
    Right
}

public class FootTargetController : MonoBehaviour
{
    private FootExerciseManager manager;

    public void Configure(FootExerciseManager exerciseManager)
    {
        manager = exerciseManager;
    }

    public void Touch(RequiredFoot usedFoot)
    {
        manager?.TargetTouched(this, usedFoot);
    }
}

public class FootExerciseManager : MonoBehaviour
{
    [Header("Sesion")]
    public int totalSets = 4;
    public float setDuration = 60f;
    public float restDuration = 30f;

    private readonly List<FootTargetController> targets = new List<FootTargetController>();
    private FootTargetController activeTarget;
    private RequiredFoot requiredFoot;
    private Coroutine targetTimer;

    private int score;
    private int misses;
    private int currentSet;
    private int level;
    private int consecutiveHits;
    private int consecutiveMisses;
    private float remainingTime;
    private float activationTime;
    private bool acceptingContact;
    private bool sessionRunning;
    private bool resting;

    public void Configure(IEnumerable<FootTargetController> configuredTargets)
    {
        targets.Clear();
        targets.AddRange(configuredTargets);

        foreach (var target in targets)
        {
            target.Configure(this);
            SetColor(target, Color.white);
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        if (targets.Count != 3)
        {
            Debug.LogError("FootExerciseManager: se necesitan exactamente tres objetivos.");
            yield break;
        }

        sessionRunning = true;

        for (currentSet = 1; currentSet <= totalSets; currentSet++)
        {
            resting = false;
            remainingTime = setDuration;
            Debug.Log($"PIERNAS | Inicia set {currentSet}/{totalSets}");
            ActivateRandomTarget();

            while (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                yield return null;
            }

            acceptingContact = false;
            StopTargetTimer();
            ClearTargets();

            if (currentSet < totalSets)
            {
                resting = true;
                remainingTime = restDuration;
                Debug.Log($"PIERNAS | Descanso despues del set {currentSet}");

                while (remainingTime > 0f)
                {
                    remainingTime -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        sessionRunning = false;
        resting = false;
        remainingTime = 0f;
        ClearTargets();

        Debug.Log(
            $"PIERNAS FINALIZADO | Puntos: {score} | Fallos: {misses} | Nivel final: {GetLevelName()}"
        );
    }

    public void TargetTouched(FootTargetController touchedTarget, RequiredFoot usedFoot)
    {
        if (!sessionRunning || resting || !acceptingContact)
        {
            return;
        }

        if (touchedTarget != activeTarget)
        {
            return;
        }

        if (usedFoot != requiredFoot)
        {
            Debug.Log($"Pie incorrecto ignorado | Requerido: {requiredFoot} | Usado: {usedFoot}");
            return;
        }

        acceptingContact = false;
        StopTargetTimer();

        var reactionTime = Time.time - activationTime;
        score++;
        consecutiveHits++;
        consecutiveMisses = 0;

        if (consecutiveHits >= 5 && level < 4)
        {
            level++;
            consecutiveHits = 0;
        }

        Debug.Log(
            $"ACIERTO PIERNAS | Pie: {usedFoot} | Reaccion: {reactionTime:F3} s | Nivel: {GetLevelName()}"
        );

        StartCoroutine(PrepareNextTarget());
    }

    private void ActivateRandomTarget()
    {
        if (!sessionRunning || resting || targets.Count == 0)
        {
            return;
        }

        ClearTargets();

        activeTarget = targets[Random.Range(0, targets.Count)];
        requiredFoot = Random.value < 0.5f ? RequiredFoot.Left : RequiredFoot.Right;

        SetColor(activeTarget, requiredFoot == RequiredFoot.Left ? Color.blue : Color.red);

        acceptingContact = true;
        activationTime = Time.time;
        targetTimer = StartCoroutine(TargetLifetimeTimer());

        Debug.Log(
            $"Objetivo de piernas activo | Pie: {requiredFoot} | Nivel: {GetLevelName()} | Limite: {TargetLifetime:F2} s"
        );
    }

    private IEnumerator TargetLifetimeTimer()
    {
        yield return new WaitForSeconds(TargetLifetime);

        if (sessionRunning && !resting && acceptingContact)
        {
            acceptingContact = false;
            misses++;
            consecutiveMisses++;
            consecutiveHits = 0;

            if (consecutiveMisses >= 3 && level > 0)
            {
                level--;
                consecutiveMisses = 0;
            }

            Debug.Log($"FALLO PIERNAS | Pie requerido: {requiredFoot} | Nivel: {GetLevelName()}");
            StartCoroutine(PrepareNextTarget());
        }
    }

    private IEnumerator PrepareNextTarget()
    {
        ClearTargets();
        yield return new WaitForSeconds(RespawnDelay);

        if (sessionRunning && !resting)
        {
            ActivateRandomTarget();
        }
    }

    private void StopTargetTimer()
    {
        if (targetTimer != null)
        {
            StopCoroutine(targetTimer);
            targetTimer = null;
        }
    }

    private void ClearTargets()
    {
        foreach (var target in targets)
        {
            SetColor(target, Color.white);
        }

        activeTarget = null;
    }

    private static void SetColor(FootTargetController target, Color color)
    {
        if (target != null && target.TryGetComponent<Renderer>(out var targetRenderer))
        {
            targetRenderer.material.color = color;
        }
    }

    private float TargetLifetime
    {
        get
        {
            float[] values = { 2.5f, 2f, 1.5f, 1f, 0.5f };
            return values[Mathf.Clamp(level, 0, values.Length - 1)];
        }
    }

    private float RespawnDelay
    {
        get
        {
            float[] values = { 1f, 0.75f, 0.5f, 0.25f, 0f };
            return values[Mathf.Clamp(level, 0, values.Length - 1)];
        }
    }

    private string GetLevelName()
    {
        string[] names = { "Inicial", "Basico", "Intermedio", "Avanzado", "Intensivo" };
        return names[Mathf.Clamp(level, 0, names.Length - 1)];
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(18, Screen.height / 32),
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(20, 15, 500, 50), $"Puntos: {score}   Fallos: {misses}", style);

        var phase = sessionRunning
            ? (resting ? "Descanso" : $"Set {currentSet}/{totalSets}")
            : "Finalizado";

        GUI.Label(
            new Rect(20, 55, 700, 50),
            $"{phase}   Tiempo: {Mathf.CeilToInt(Mathf.Max(0f, remainingTime))}   Nivel: {GetLevelName()}",
            style
        );
    }
}

[RequireComponent(typeof(Camera))]
public class MediaPipeFootInteraction : MonoBehaviour
{
    public bool flipHorizontal;
    public bool swapFootLabels;
    public float minimumVisibility = 0.5f;
    public float maximumDistance = 100f;
    public RectTransform videoRect;

    private Camera interactionCamera;
    private readonly Vector3[] videoCorners = new Vector3[4];

    private void Awake()
    {
        interactionCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        foreach (var foot in PoseTrackingBridge.GetLatestFeet())
        {
            if (foot.visibility < minimumVisibility)
            {
                continue;
            }

            TryInteract(foot);
        }
    }

    private void TryInteract(PoseTrackingBridge.TrackedFoot foot)
    {
        float x = flipHorizontal ? 1f - foot.x : foot.x;
        float y = 1f - foot.y;

        if (x < 0f || x > 1f || y < 0f || y > 1f)
        {
            return;
        }

        var ray = CreateInteractionRay(x, y);
        var hits = Physics.RaycastAll(ray, maximumDistance);

        foreach (var hit in hits)
        {
            var target = hit.collider.GetComponentInParent<FootTargetController>();
            if (target == null)
            {
                continue;
            }

            var side = foot.side == PoseTrackingBridge.FootSide.Left
                ? RequiredFoot.Left
                : RequiredFoot.Right;

            if (swapFootLabels)
            {
                side = side == RequiredFoot.Left ? RequiredFoot.Right : RequiredFoot.Left;
            }

            target.Touch(side);
            return;
        }
    }

    private Ray CreateInteractionRay(float normalizedX, float normalizedY)
    {
        if (videoRect == null)
        {
            return interactionCamera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0f));
        }

        videoRect.GetWorldCorners(videoCorners);

        var canvas = videoRect.GetComponentInParent<Canvas>();
        var canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        var bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[0]);
        var topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[2]);

        var screenPoint = new Vector2(
            Mathf.Lerp(bottomLeft.x, topRight.x, normalizedX),
            Mathf.Lerp(bottomLeft.y, topRight.y, normalizedY)
        );

        return interactionCamera.ScreenPointToRay(screenPoint);
    }
}

public static class FootExerciseBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Object.FindFirstObjectByType<PoseLandmarkerRunner>() == null)
        {
            return;
        }

        var left = GameObject.Find("TargetLeft");
        var center = GameObject.Find("TargetCenter");
        var right = GameObject.Find("TargetRight");

        if (left == null || center == null || right == null)
        {
            Debug.LogWarning("FootExerciseBootstrap: no se encontraron los tres objetivos.");
            return;
        }

        var runtime = new GameObject("FootExerciseRuntime");
        var manager = runtime.AddComponent<FootExerciseManager>();

        var targetControllers = new[]
        {
            AddTargetController(left),
            AddTargetController(center),
            AddTargetController(right)
        };

        manager.Configure(targetControllers);

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("FootExerciseBootstrap: no se encontro la camara principal.");
            return;
        }

        var interaction = camera.gameObject.AddComponent<MediaPipeFootInteraction>();

        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if (rect.name == "Screen")
            {
                interaction.videoRect = rect;
                break;
            }
        }
    }

    private static FootTargetController AddTargetController(GameObject target)
    {
        var controller = target.GetComponent<FootTargetController>();
        return controller != null ? controller : target.AddComponent<FootTargetController>();
    }
}
