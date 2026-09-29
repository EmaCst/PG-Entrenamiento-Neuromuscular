using TMPro;
using UnityEngine;

public class RunningCalibrationInput : MonoBehaviour
{
    [SerializeField] private RunningAreaCalibrator calibrator;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool acceptMouseInput = true;
    [SerializeField] private bool acceptTouchInput = true;

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
            RefreshStatus(calibrator.CapturedCornerCount);
        }
    }

    private void OnDisable()
    {
        if (calibrator != null) calibrator.CalibrationProgressChanged -= RefreshStatus;
    }

    private void Update()
    {
        if (calibrator == null || calibrator.IsCalibrated) return;

        if (acceptTouchInput && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            calibrator.CaptureNextCornerFromScreen(Input.GetTouch(0).position);
            return;
        }

        if (acceptMouseInput && Input.GetMouseButtonDown(0))
        {
            calibrator.CaptureNextCornerFromScreen(Input.mousePosition);
        }
    }

    public void ResetCalibration()
    {
        if (calibrator != null) calibrator.ResetCalibration();
    }

    private void RefreshStatus(int captured)
    {
        if (statusText == null) return;
        statusText.text = captured >= 4
            ? "Area calibrada"
            : $"Marca la esquina {captured + 1}/4: {CornerNames[captured]}";
    }
}
