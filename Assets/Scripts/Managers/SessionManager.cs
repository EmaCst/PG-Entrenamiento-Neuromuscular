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

public enum ExerciseType
{
    Hands,
    Feet,
    Running
}

public class SessionManager : MonoBehaviour
{
    [Header("Modo de sesion")]
    public SessionMode sessionMode = SessionMode.Individual;

    [Header("Ejercicio actual")]
    public ExerciseType exerciseType = ExerciseType.Hands;
    public AimLabManager currentExercise;

    [Header("Ejercicio de pies")]
    public FootExerciseManager currentFootExercise;
    public FootExerciseStats footStats;

    [Header("Ejercicio de correr")]
    public RunningExerciseManager currentRunningExercise;
    public RunningExerciseStats runningStats;

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
        if (!HasSelectedExercise())
        {
            Debug.LogError(
                "SessionManager: No hay ejercicio asignado."
            );

            yield break;
        }

        sessionActive = true;
        currentSet = 1;

        if (exerciseType == ExerciseType.Running)
        {
            Debug.Log("SessionManager: esperando calibracion del area de carrera.");
            while (currentRunningExercise != null && !currentRunningExercise.IsReady)
            {
                yield return null;
            }

            sessionStartTime = DateTime.Now;
        }

        if (exerciseType == ExerciseType.Feet)
        {
            Debug.Log("SessionManager: esperando camara y modelo del ejercicio de pies.");
            while (currentFootExercise != null && !currentFootExercise.IsReady)
            {
                yield return null;
            }

            sessionStartTime = DateTime.Now;
        }

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

            if (exerciseType == ExerciseType.Hands && currentExercise != null && currentExercise.difficulty != null)
            {
                currentExercise
                    .difficulty
                    .ResetStreaks();
            }

            if (currentSet == 1)
            {
                StartSelectedExercise();
            }
            else
            {
                ResumeSelectedExercise();
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

            PauseSelectedExercise();

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

        StopSelectedExercise();

        if (timerText != null)
        {
            timerText.text =
                "FINALIZADO";
        }

        DateTime sessionEndTime =
            DateTime.Now;

        SessionResultData sessionResult = new SessionResultData();
        sessionResult.sessionId = sessionId;
        sessionResult.athleteId = athleteId;
        sessionResult.mode = sessionMode.ToString().ToLower();
        sessionResult.startedAt = sessionStartTime.ToString("o");
        sessionResult.endedAt = sessionEndTime.ToString("o");

        if (exerciseType == ExerciseType.Hands && aimLabStats != null)
        {
            AimLabResult aimLabResult =
                aimLabStats.BuildResult(
                    totalSets,
                    setDuration,
                    restDuration
                );

            sessionResult.exercises.Add(
                aimLabResult
            );
        }

        if (exerciseType == ExerciseType.Running && runningStats != null && currentRunningExercise != null)
        {
            sessionResult.runningExercises.Add(
                runningStats.BuildResult(
                    totalSets,
                    setDuration,
                    restDuration,
                    currentRunningExercise.AreaCalibrator.GetAreaData()
                )
            );
        }

        if (exerciseType == ExerciseType.Feet && footStats != null)
        {
            sessionResult.footExercises.Add(
                footStats.BuildResult(totalSets, setDuration, restDuration)
            );
        }

        string json = JsonUtility.ToJson(sessionResult, true);
        Debug.Log("===== JSON SESION COMPLETA =====\n" + json);

        Debug.Log(
            "SESION FINALIZADA"
        );
    }

    private bool HasSelectedExercise()
    {
        switch (exerciseType)
        {
            case ExerciseType.Hands:
                return currentExercise != null;
            case ExerciseType.Feet:
                return currentFootExercise != null;
            case ExerciseType.Running:
                return currentRunningExercise != null;
            default:
                return false;
        }
    }

    private void StartSelectedExercise()
    {
        if (exerciseType == ExerciseType.Hands) currentExercise.StartExercise();
        else if (exerciseType == ExerciseType.Feet) currentFootExercise.StartExercise();
        else currentRunningExercise.StartExercise();
    }

    private void PauseSelectedExercise()
    {
        if (exerciseType == ExerciseType.Hands) currentExercise.PauseExercise();
        else if (exerciseType == ExerciseType.Feet) currentFootExercise.PauseExercise();
        else currentRunningExercise.PauseExercise();
    }

    private void ResumeSelectedExercise()
    {
        if (exerciseType == ExerciseType.Hands) currentExercise.ResumeExercise();
        else if (exerciseType == ExerciseType.Feet) currentFootExercise.ResumeExercise();
        else currentRunningExercise.ResumeExercise();
    }

    private void StopSelectedExercise()
    {
        if (!HasSelectedExercise()) return;
        if (exerciseType == ExerciseType.Hands) currentExercise.StopExercise();
        else if (exerciseType == ExerciseType.Feet) currentFootExercise.StopExercise();
        else currentRunningExercise.StopExercise();
    }
}
