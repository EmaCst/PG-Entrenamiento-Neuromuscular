using TMPro;
using UnityEngine;

public class RunningCalibrationInput : MonoBehaviour
{
    [SerializeField] private RunningAreaCalibrator calibrator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RunningExerciseManager exerciseManager;
    [SerializeField] private bool acceptMouseInput = true;
    [SerializeField] private bool acceptTouchInput = true;
    private float nextStatusRefresh;
    private float failureMessageUntil;

    private static readonly string[] CornerNames =
    {
        "suroeste",
        "noroeste",
        "noreste",
        "sureste"
    };

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

        if (acceptTouchInput && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryCapture(Input.GetTouch(0).position);
            return;
        }

        if (acceptMouseInput && Input.GetMouseButtonDown(0))
        {
            TryCapture(Input.mousePosition);
        }
    }

    public void ResetCalibration()
    {
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
            statusText.text = "Mueve el telefono lentamente para detectar el suelo.";
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
        if (calibrator.CaptureNextCornerFromScreen(screenPosition)) return;

        if (statusText != null)
        {
            statusText.text = calibrator.HasDetectedGround
                ? "No se encontro suelo en ese punto. Toca una esquina sobre el piso."
                : "Aun no se detecta el suelo. Mueve el telefono despacio y vuelve a tocar.";
        }

        failureMessageUntil = Time.unscaledTime + 2f;
        nextStatusRefresh = failureMessageUntil;
    }

    private void StartExerciseAfterCalibration()
    {
        if (CombinedExerciseSession.Instance != null && CombinedExerciseSession.Instance.IsRunning)
        {
            return;
        }

        if (exerciseManager != null)
        {
            exerciseManager.StartExercise();
        }
    }
}
