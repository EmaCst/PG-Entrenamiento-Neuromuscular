using UnityEngine;
using TMPro;
using System.Collections;

public class AimLabManager : MonoBehaviour
{
    [Header("Referencias")]
    public AimLabGenerator generator;
    public AimLabDifficulty difficulty;
    public TMP_Text scoreText;

    private GameObject currentTarget;

    private int score = 0;
    private int hits = 0;
    private int misses = 0;

    private bool waitingForNextTarget = true;
    private bool exerciseActive = false;

    private Coroutine targetTimerCoroutine;

    private float targetActivationTime;

    // =========================
    // INICIALIZACIÓN
    // =========================

    void Awake()
    {
        if (generator == null)
        {
            Debug.LogError("AimLabManager: Generator no está asignado.");
            return;
        }

        if (difficulty == null)
        {
            Debug.LogError("AimLabManager: Difficulty no está asignado.");
            return;
        }

        generator.GenerateGrid();

        foreach (GameObject target in generator.targets)
        {
            TargetController controller =
                target.GetComponent<TargetController>();

            if (controller != null)
            {
                controller.SetManager(this);
            }
        }

        UpdateScoreUI();

        // Al iniciar la escena dejamos los targets ocultos
        SetTargetsVisible(false);
    }

    // =========================
    // OBJETIVOS
    // =========================

    void ActivateRandomTarget()
    {
        if (!exerciseActive)
        {
            return;
        }

        if (generator.targets.Count == 0)
        {
            Debug.LogError("AimLabManager: No hay targets generados.");
            return;
        }

        foreach (GameObject target in generator.targets)
        {
            Renderer renderer = target.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }
        }

        int index = Random.Range(
            0,
            generator.targets.Count
        );

        currentTarget = generator.targets[index];

        Renderer currentRenderer =
            currentTarget.GetComponent<Renderer>();

        if (currentRenderer != null)
        {
            currentRenderer.material.color = Color.green;
        }

        waitingForNextTarget = false;

        targetActivationTime = Time.time;

        Debug.Log(
            "Objetivo activo | Nivel: " +
            difficulty.currentLevel +
            " | Tiempo limite: " +
            difficulty.TargetLifetime +
            " s"
        );

        if (targetTimerCoroutine != null)
        {
            StopCoroutine(targetTimerCoroutine);
        }

        targetTimerCoroutine =
            StartCoroutine(TargetTimer());
    }

    IEnumerator TargetTimer()
    {
        yield return new WaitForSeconds(
            difficulty.TargetLifetime
        );

        if (exerciseActive)
        {
            MissTarget();
        }
    }

    // =========================
    // ACIERTO
    // =========================

    public void TargetTouched(GameObject touchedTarget)
    {
        if (!exerciseActive)
        {
            return;
        }

        if (waitingForNextTarget)
        {
            return;
        }

        if (touchedTarget != currentTarget)
        {
            return;
        }

        if (targetTimerCoroutine != null)
        {
            StopCoroutine(targetTimerCoroutine);
            targetTimerCoroutine = null;
        }

        float reactionTime =
            Time.time - targetActivationTime;

        hits++;
        score++;

        difficulty.RegisterHit();

        Debug.Log(
            "ACIerto | Reaccion: " +
            reactionTime.ToString("F3") +
            " s | Nivel: " +
            difficulty.currentLevel +
            " | Aciertos totales: " +
            hits
        );

        UpdateScoreUI();

        StartCoroutine(PrepareNextTarget());
    }

    // =========================
    // FALLO
    // =========================

    void MissTarget()
    {
        if (!exerciseActive)
        {
            return;
        }

        if (waitingForNextTarget)
        {
            return;
        }

        misses++;

        difficulty.RegisterMiss();

        Debug.Log(
            "FALLO | Nivel: " +
            difficulty.currentLevel +
            " | Fallos totales: " +
            misses
        );

        StartCoroutine(PrepareNextTarget());
    }

    // =========================
    // ESPERA ENTRE OBJETIVOS
    // =========================

    IEnumerator PrepareNextTarget()
    {
        waitingForNextTarget = true;

        if (currentTarget != null)
        {
            Renderer renderer =
                currentTarget.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }
        }

        float inicioEspera = Time.time;

        Debug.Log(
            "Inicio espera: " +
            inicioEspera +
            " | Nivel: " +
            difficulty.currentLevel +
            " | Espera configurada: " +
            difficulty.RespawnDelay +
            " s"
        );

        yield return new WaitForSeconds(
            difficulty.RespawnDelay
        );

        float finEspera = Time.time;

        Debug.Log(
            "Duracion real espera: " +
            (finEspera - inicioEspera)
        );

        if (exerciseActive)
        {
            ActivateRandomTarget();
        }
    }

    // =========================
    // VISIBILIDAD
    // =========================

    void SetTargetsVisible(bool visible)
    {
        foreach (GameObject target in generator.targets)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }
    }

    // =========================
    // CONTROL DESDE SESSION MANAGER
    // =========================

    public void StartExercise()
    {
        Debug.Log("AimLab iniciado.");

        exerciseActive = true;
        waitingForNextTarget = false;

        SetTargetsVisible(true);

        ActivateRandomTarget();
    }

    public void PauseExercise()
    {
        Debug.Log("AimLab pausado.");

        exerciseActive = false;
        waitingForNextTarget = true;

        StopAllCoroutines();

        targetTimerCoroutine = null;

        // Ocultar TODO el tablero durante el descanso
        SetTargetsVisible(false);
    }

    public void ResumeExercise()
    {
        Debug.Log("AimLab reanudado.");

        exerciseActive = true;
        waitingForNextTarget = false;

        SetTargetsVisible(true);

        ActivateRandomTarget();
    }

    public void StopExercise()
    {
        Debug.Log("AimLab finalizado.");

        PauseExercise();
    }

    // =========================
    // UI
    // =========================

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Puntos: " + score;
        }
    }
}