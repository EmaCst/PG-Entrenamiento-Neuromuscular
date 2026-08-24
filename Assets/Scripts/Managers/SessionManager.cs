using UnityEngine;
using TMPro;
using System;
using System.Collections;

public enum SessionMode
{
    Individual,
    Circuit,
    Custom
}

public class SessionManager : MonoBehaviour
{
    [Header("Modo de sesion")]
    public SessionMode sessionMode = SessionMode.Individual;

    [Header("Ejercicio actual")]
    public AimLabManager currentExercise;

    [Header("Resultados AimLab")]
    public AimLabStats aimLabStats;

    [Header("Configuracion")]
    public int totalSets = 4;
    public float setDuration = 60f;
    public float restDuration = 30f;

    [Header("Usuario")]
    public int athleteId = 1;

    [Header("UI")]
    public TMP_Text timerText;

    private int currentSet = 1;
    private float remainingTime;

    private bool sessionActive = false;
    private bool resting = false;

    private string sessionId;
    private DateTime sessionStartTime;

    public int CurrentSet => currentSet;
    public bool IsResting => resting;
    public bool SessionActive => sessionActive;

    void Start()
    {
        sessionId =
            Guid.NewGuid().ToString();

        sessionStartTime =
            DateTime.Now;

        if (sessionMode == SessionMode.Individual)
        {
            StartCoroutine(
                RunIndividualSession()
            );
        }
    }

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

            if (currentExercise.difficulty != null)
            {
                currentExercise
                    .difficulty
                    .ResetStreaks();
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
                remainingTime -=
                    Time.deltaTime;

                UpdateTimerUI();

                yield return null;
            }

            remainingTime = 0;

            UpdateTimerUI();

            currentExercise.PauseExercise();

            Debug.Log(
                "TERMINA SET " +
                currentSet +
                "/" +
                totalSets
            );

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
                remainingTime -=
                    Time.deltaTime;

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

        DateTime sessionEndTime =
            DateTime.Now;

        if (aimLabStats != null)
        {
            AimLabResult aimLabResult =
                aimLabStats.BuildResult(
                    totalSets,
                    setDuration,
                    restDuration
                );

            SessionResultData sessionResult =
                new SessionResultData();

            sessionResult.sessionId =
                sessionId;

            sessionResult.athleteId =
                athleteId;

            sessionResult.mode =
                sessionMode
                    .ToString()
                    .ToLower();

            sessionResult.startedAt =
                sessionStartTime
                    .ToString("o");

            sessionResult.endedAt =
                sessionEndTime
                    .ToString("o");

            sessionResult.exercises.Add(
                aimLabResult
            );

            string json =
                JsonUtility.ToJson(
                    sessionResult,
                    true
                );

            Debug.Log(
                "===== JSON SESION COMPLETA =====\n" +
                json
            );
        }

        Debug.Log(
            "SESION FINALIZADA"
        );
    }
}