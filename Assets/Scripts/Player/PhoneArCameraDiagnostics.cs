using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

// Only attached to AR scenes; shows whether the provider actually supplies frames.
public class PhoneArCameraDiagnostics : MonoBehaviour
{
    private ARCameraManager cameraManager;
    private int frames;
    private float nextRefresh;
    private ARPlaneManager planes;
    private RunningAreaCalibrator calibrator;
    private string text;
    private GUIStyle style;
    private void OnEnable()
    {
        cameraManager = GetComponent<ARCameraManager>();
        if (cameraManager != null) cameraManager.frameReceived += OnFrame;
    }
    private void OnDisable()
    {
        if (cameraManager != null) cameraManager.frameReceived -= OnFrame;
    }
    private void OnFrame(ARCameraFrameEventArgs args) { frames++; }
    private void OnGUI()
    {
        if (Time.unscaledTime >= nextRefresh || text == null)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            if (planes == null) planes = Object.FindFirstObjectByType<ARPlaneManager>();
            if (calibrator == null) calibrator = Object.FindFirstObjectByType<RunningAreaCalibrator>();
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            string loader = manager != null && manager.activeLoader != null ? manager.activeLoader.name : "sin loader XR";
            int count = planes != null ? planes.trackables.count : 0;
            text = $"AR: {ARSession.state} | {loader} | video: {frames} | suelo opcional: {count} planos | mapa: {(calibrator != null ? calibrator.Phase.ToString() : "sin mapa")} | referencia: {(calibrator != null ? calibrator.MapReferenceStatus : "sin ancla")} | {ARSession.notTrackingReason}";
        }
        if (!Debug.isDebugBuild && frames > 0 && ARSession.state == ARSessionState.SessionTracking &&
            calibrator != null && (calibrator.IsCalibrated || calibrator.Phase == RunningAreaCalibrator.WalkPhase.Drawing ||
            calibrator.Phase == RunningAreaCalibrator.WalkPhase.Confirming)) return;
        if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(12, Screen.height / 40), wordWrap = true };
        for (int eye = 0; eye < 2; eye++)
            GUI.Box(new Rect(eye * Screen.width * 0.5f + 10, Screen.height * 0.82f,
                Screen.width * 0.5f - 20, Screen.height * 0.12f), text, style);
    }
}
