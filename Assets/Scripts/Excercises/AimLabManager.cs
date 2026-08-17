using UnityEngine;
using TMPro;
using System.Collections;

public class AimLabManager : MonoBehaviour
{
    public AimLabGenerator generator;
    public AimLabDifficulty difficulty;
    public TMP_Text scoreText;

    private GameObject currentTarget;

    private int score = 0;
    private int hits = 0;
    private int misses = 0;

    private bool waitingForNextTarget = false;

    private Coroutine targetTimerCoroutine;

    private float targetActivationTime;

    void Start()
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

        ActivateRandomTarget();
    }

    void ActivateRandomTarget()
    {
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

        int index = Random.Range(0, generator.targets.Count);

        currentTarget = generator.targets[index];

        Renderer currentRenderer =
            currentTarget.GetComponent<Renderer>();

        if (currentRenderer != null)
        {
            currentRenderer.material.color = Color.green;
        }

        waitingForNextTarget = false;

        // Guardamos el momento exacto en que aparece el objetivo.
        targetActivationTime = Time.time;

        Debug.Log(
            "Objetivo activo | Nivel: " +
            difficulty.currentLevel +
            " | Tiempo límite: " +
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

        MissTarget();
    }

    public void TargetTouched(GameObject touchedTarget)
    {
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
            "ACIerto | Reacción: " +
            reactionTime.ToString("F3") +
            " s | Nivel: " +
            difficulty.currentLevel +
            " | Aciertos totales: " +
            hits
        );

        UpdateScoreUI();

        StartCoroutine(PrepareNextTarget());
    }

    void MissTarget()
    {
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
            "Fin espera: " +
            finEspera
        );

        Debug.Log(
            "Duración real espera: " +
            (finEspera - inicioEspera)
        );

        ActivateRandomTarget();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Puntos: " + score;
        }
    }
}