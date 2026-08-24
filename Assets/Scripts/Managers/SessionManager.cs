using UnityEngine;
using TMPro;
using System.Collections;

public enum SessionMode
{
    Individual,
    Circuit,
    Custom
}

public class SessionManager : MonoBehaviour
{
   [Header("Resultados AimLab")]
    public AimLabStats aimLabStats;

    [Header("Modo de sesion")]
    public SessionMode sessionMode = SessionMode.Individual;

    [Header("Ejercicio actual")]
    public AimLabManager currentExercise;

    [Header("Configuracion")]
    public int totalSets = 4;
    public float setDuration = 60f;
    public float restDuration = 30f;

    [Header("UI")]
    public TMP_Text timerText;

    private int currentSet = 1;
    private float remainingTime;

    private bool sessionActive = false;
    private bool resting = false;

    public int CurrentSet => currentSet;
    public bool IsResting => resting;
    public bool SessionActive => sessionActive;

    void Start()
    {
        if (sessionMode == SessionMode.Individual)
        {
            StartCoroutine(
                RunIndividualSession()
            );
        }
    }

    // =========================
    // SESIÓN INDIVIDUAL
    // =========================

    IEnumerator RunIndividualSession()
    {
        if (currentExercise == null)
        {
            Debug.LogError(
                "SessionManager: No hay ejercicio asignado."
            );

            yield break;
        }

        sessionActive = true;
        currentSet = 1;

        while (currentSet <= totalSets)
        {
            // =========================
            // SET
            // =========================

            resting = false;
            remainingTime = setDuration;

            Debug.Log(
                "INICIA SET " +
                currentSet +
                "/" +
                totalSets
            );

            // Reiniciar las rachas,
            // pero conservar el nivel
            if (currentExercise.difficulty != null)
            {
                currentExercise.difficulty.ResetStreaks();
            }

            if (currentSet == 1)
            {
                currentExercise.StartExercise();
            }
            else
            {
                currentExercise.ResumeExercise();
            }

            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;

                UpdateTimerUI();

                yield return null;
            }

            remainingTime = 0;

            UpdateTimerUI();

            // Pausar AimLab
            currentExercise.PauseExercise();

            Debug.Log(
                "TERMINA SET " +
                currentSet +
                "/" +
                totalSets
            );

            // Si fue el último set, termina
            if (currentSet >= totalSets)
            {
                break;
            }

            // =========================
            // DESCANSO
            // =========================

            resting = true;
            remainingTime = restDuration;

            Debug.Log("INICIA DESCANSO");

            while (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;

                UpdateTimerUI();

                yield return null;
            }

            remainingTime = 0;

            UpdateTimerUI();

            Debug.Log("TERMINA DESCANSO");

            currentSet++;
        }

        FinishSession();
    }

    // =========================
    // UI DEL TEMPORIZADOR
    // =========================

    void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        int seconds =
            Mathf.CeilToInt(remainingTime);

        int minutes =
            seconds / 60;

        int remainingSeconds =
            seconds % 60;

        if (resting)
        {
            timerText.text =
                "Descanso: " +
                minutes.ToString("00") +
                ":" +
                remainingSeconds.ToString("00");
        }
        else
        {
            timerText.text =
                "Set " +
                currentSet +
                "/" +
                totalSets +
                "  " +
                minutes.ToString("00") +
                ":" +
                remainingSeconds.ToString("00");
        }
    }

    // =========================
    // FINALIZAR
    // =========================

    void FinishSession()
    {
        sessionActive = false;
        resting = false;

        if (currentExercise != null)
        {
            currentExercise.StopExercise();
        }

        if (timerText != null)
        {
            timerText.text =
                "FINALIZADO";
        }

        if (aimLabStats != null)
{
    string json = aimLabStats.BuildJson(
        totalSets,
        setDuration,
        restDuration
    );

    Debug.Log(
        "===== JSON RESULTADOS =====\n" +
        json
    );
}

        Debug.Log("SESION FINALIZADA");
    }
}