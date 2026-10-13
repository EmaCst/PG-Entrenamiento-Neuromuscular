using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RunningCalibrationInput : MonoBehaviour
{
    [SerializeField] private RunningAreaCalibrator calibrator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RunningExerciseManager exerciseManager;
    [SerializeField] private bool acceptMouseInput = true;
    [SerializeField] private bool acceptTouchInput = true;
    private float nextStatusRefresh;
    private float failureMessageUntil;
    private Coroutine exerciseRoutine;
    private ExerciseSetRunner setRunner;

    private static readonly string[] CornerNames =
    {
        "suroeste",
        "noroeste",
        "noreste",
        "sureste"
    };

    private IEnumerator Start()
    {
        yield return null;
        if (calibrator != null && !calibrator.IsCalibrated && !calibrator.HasDetectedGround)
            PraxenVoice.Instance.Play("ground");
    }

    private void OnEnable()
    {
        if (calibrator != null)
        {
            calibrator.CalibrationProgressChanged += RefreshStatus;
            calibrator.CalibrationCompleted += StartExerciseAfterCalibration;
            RefreshStatus(calibrator.CapturedCornerCount);
        }
    }

    private void OnDisable()
    {
        CancelExerciseRoutine();
        if (calibrator != null)
        {
            calibrator.CalibrationProgressChanged -= RefreshStatus;
            calibrator.CalibrationCompleted -= StartExerciseAfterCalibration;
        }
    }

    private void Update()
    {
        if (calibrator == null || calibrator.IsCalibrated) return;

        if (Time.unscaledTime >= nextStatusRefresh && Time.unscaledTime >= failureMessageUntil)
        {
            RefreshStatus(calibrator.CapturedCornerCount);
        }

        var touchscreen = Touchscreen.current;
        if (acceptTouchInput && touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            TryCapture(touchscreen.primaryTouch.position.ReadValue());
            return;
        }

        var mouse = Mouse.current;
        if (acceptMouseInput && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            TryCapture(mouse.position.ReadValue());
        }
    }

    public void ResetCalibration()
    {
        CancelExerciseRoutine();
        if (exerciseManager != null) exerciseManager.StopExercise();
        if (calibrator != null) calibrator.ResetCalibration();
    }

    private void RefreshStatus(int captured)
    {
        if (statusText == null) return;
        if (captured >= 4)
        {
            statusText.text = "Area delimitada";
        }
        else if (captured == 0 && !calibrator.HasDetectedGround)
        {
            statusText.text = calibrator.GroundStatus;
        }
        else
        {
            string instruction = $"Toca la esquina {captured + 1}/4: {CornerNames[captured]}";
            statusText.text = captured == 0
                ? $"Suelo detectado. {instruction}"
                : instruction;
        }

        nextStatusRefresh = Time.unscaledTime + 0.5f;
    }

    private void TryCapture(Vector2 screenPosition)
    {
        if (!PhoneViewerPresentation.TryMapScreenPoint(screenPosition, out screenPosition)) return;
        if (calibrator.CaptureNextCornerFromScreen(screenPosition)) return;

        if (statusText != null)
        {
            statusText.text = string.IsNullOrEmpty(calibrator.LastCaptureFailure)
                ? calibrator.GroundStatus : calibrator.LastCaptureFailure;
        }

        failureMessageUntil = Time.unscaledTime + 2f;
        nextStatusRefresh = failureMessageUntil;
    }

    private void StartExerciseAfterCalibration()
    {
        PraxenVoice.Instance.Stop();
        if (CombinedExerciseSession.Instance != null && CombinedExerciseSession.Instance.IsRunning)
        {
            return;
        }

        if (exerciseManager != null && exerciseRoutine == null)
            exerciseRoutine = StartCoroutine(StartPreparedExercise());
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
