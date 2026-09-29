using UnityEngine;

public class RunningExerciseDemoSetup : MonoBehaviour
{
    [SerializeField] private RunningAreaCalibrator calibrator;
    [SerializeField] private Transform trackedHead;
    [SerializeField] private bool calibrateOnStart = true;
    [SerializeField] private float width = 5f;
    [SerializeField] private float depth = 5f;
    [SerializeField] private float forwardOffset = 1.5f;
    [SerializeField] private float groundY;

    private void Start()
    {
        if (!calibrateOnStart || calibrator == null || calibrator.IsCalibrated) return;

        Vector3 reference = trackedHead != null ? trackedHead.position : transform.position;
        Vector3 forward = trackedHead != null ? trackedHead.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

        Vector3 center = reference + forward.normalized * forwardOffset;
        calibrator.SetRectangularArea(center, width, depth, groundY);
    }
}
