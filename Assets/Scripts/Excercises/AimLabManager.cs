using UnityEngine;
using TMPro;
using System.Collections;

public class AimLabManager : MonoBehaviour
{
    [Header("Referencias")]
    public AimLabGenerator generator;
    public AimLabDifficulty difficulty;
    public AimLabHandController handController;
    public AimLabStats stats;
    public TMP_Text scoreText;

    private GameObject currentTarget;

    private int score = 0;

    private bool waitingForNextTarget = true;
    private bool exerciseActive = false;

    private Coroutine targetTimerCoroutine;

    private float targetActivationTime;

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

        if (handController == null)
        {
            Debug.LogError("AimLabManager: HandController no está asignado.");
            return;
        }

        if (stats == null)
        {
            Debug.LogError("AimLabManager: Stats no está asignado.");
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

        stats.Initialize(difficulty.currentLevel);

        UpdateScoreUI();
        SetTargetsVisible(false);
    }

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

        int index = Random.Range(0, generator.targets.Count);

        currentTarget = generator.targets[index];

        handController.GenerateRandomHand();

        Renderer currentRenderer =
            currentTarget.GetComponent<Renderer>();

        if (currentRenderer != null)
        {
            currentRenderer.material.color =
                handController.GetCurrentColor();
        }

        waitingForNextTarget = false;

        targetActivationTime = Time.time;

        Debug.Log(
            "Objetivo activo | Mano: " +
            handController.CurrentHand +
            " | Nivel: " +
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

    public void TargetTouched(
        GameObject touchedTarget,
        RequiredHand usedHand
    )
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

        // Mano incorrecta: ignorar
        if (!handController.IsCorrectHand(usedHand))
        {
            Debug.Log(
                "Mano incorrecta ignorada. Requerida: " +
                handController.CurrentHand +
                " | Usada: " +
                usedHand
            );

            return;
        }

        if (targetTimerCoroutine != null)
        {
            StopCoroutine(targetTimerCoroutine);
            targetTimerCoroutine = null;
        }

        float reactionTime =
            Time.time - targetActivationTime;

        score++;

        stats.RegisterHit(
            usedHand,
            reactionTime
        );

        difficulty.RegisterHit();

        Debug.Log(
            "ACIERTO | Mano: " +
            usedHand +
            " | Reaccion: " +
            reactionTime.ToString("F3") +
            " s | Nivel: " +
            difficulty.currentLevel
        );

        UpdateScoreUI();

        StartCoroutine(PrepareNextTarget());
    }

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

        stats.RegisterMiss(
            handController.CurrentHand
        );

        difficulty.RegisterMiss();

        Debug.Log(
            "FALLO | Mano requerida: " +
            handController.CurrentHand +
            " | Nivel: " +
            difficulty.currentLevel
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
            "Duracion real espera: " +
            (finEspera - inicioEspera)
        );

        if (exerciseActive)
        {
            ActivateRandomTarget();
        }
    }

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

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Puntos: " + score;
        }
    }
}