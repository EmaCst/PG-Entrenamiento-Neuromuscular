using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Head movement draws the map. There are no screen taps in this flow.
public class RunningCalibrationInput : MonoBehaviour
{
    [SerializeField] private RunningAreaCalibrator calibrator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RunningExerciseManager exerciseManager;
    private float nextStatusRefresh;
    private Coroutine exerciseRoutine;
    private ExerciseSetRunner setRunner;
    private RunningAreaCalibrator.WalkPhase lastPhase;
    private void OnEnable()
    {
        if (calibrator != null)
        {
            calibrator.CalibrationCompleted += StartExerciseAfterCalibration;
            lastPhase = calibrator.Phase;
        }
    }
    private void OnDisable()
    {
        CancelExerciseRoutine();
        if (calibrator != null) calibrator.CalibrationCompleted -= StartExerciseAfterCalibration;
    }
    private void Update()
    {
        if (calibrator == null || calibrator.IsCalibrated) return;
        if (lastPhase != calibrator.Phase)
        {
            lastPhase = calibrator.Phase;
            if (lastPhase == RunningAreaCalibrator.WalkPhase.Drawing) PraxenVoice.Instance.Play("delimit_optional");
            else if (lastPhase == RunningAreaCalibrator.WalkPhase.Retry) PraxenVoice.Instance.Stop();
        }
        if (Time.unscaledTime < nextStatusRefresh) return;
        nextStatusRefresh = Time.unscaledTime + 0.2f;
        if (statusText != null) statusText.text = calibrator.Status;
    }
    public void ResetCalibration()
    {
        CancelExerciseRoutine();
        if (exerciseManager != null) exerciseManager.StopExercise();
        if (calibrator != null) calibrator.ResetCalibration();
    }
    private void StartExerciseAfterCalibration()
    {
        PraxenVoice.Instance.Stop();
        if (CombinedExerciseSession.Instance != null && CombinedExerciseSession.Instance.IsRunning) return;
        if (exerciseManager != null && exerciseRoutine == null) exerciseRoutine = StartCoroutine(StartPreparedExercise());
    }
    private void CancelExerciseRoutine()
    {
        PraxenVoice.Instance.Stop();
        if (setRunner != null) setRunner.Cancel();
        if (exerciseRoutine != null) StopCoroutine(exerciseRoutine);
        exerciseRoutine = null;
    }
    private IEnumerator StartPreparedExercise()
    {
        if (calibrator == null || !calibrator.IsCalibrated || exerciseManager == null)
        { exerciseRoutine = null; yield break; }
        while (!calibrator.IsTrackingStable) yield return null;
        var result = PraxenCloud.Instance.Begin("individual");
        if (setRunner == null) setRunner = gameObject.AddComponent<ExerciseSetRunner>();
        yield return setRunner.Run(exerciseManager, PhoneTrainingOptions.SetCount,
            PhoneTrainingOptions.IndividualSeconds, PhoneTrainingOptions.RestSeconds);
        if (setRunner.Completed)
        {
            PraxenResultCapture.Add(result, "TelefonoCorrer", PhoneTrainingOptions.SetCount,
                PhoneTrainingOptions.IndividualSeconds, PhoneTrainingOptions.RestSeconds);
            PraxenCloud.Instance.Submit(result);
        }
        exerciseRoutine = null;
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
    }
}
