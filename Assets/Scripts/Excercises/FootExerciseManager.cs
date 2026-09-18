using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

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

