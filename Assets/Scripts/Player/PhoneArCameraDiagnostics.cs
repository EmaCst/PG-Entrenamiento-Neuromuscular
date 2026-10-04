using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

// Only attached to AR scenes; shows whether the provider actually supplies frames.
public class PhoneArCameraDiagnostics : MonoBehaviour
{
    private ARCameraManager cameraManager;
    private int frames;
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
        if (!Debug.isDebugBuild && frames > 0 && ARSession.state == ARSessionState.SessionTracking) return;
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        string loader = manager != null && manager.activeLoader != null ? manager.activeLoader.name : "sin loader XR";
        string text = $"AR: {ARSession.state} | {loader} | video: {frames} frames";
        var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(12, Screen.height / 40), wordWrap = true };
        for (int eye = 0; eye < 2; eye++)
            GUI.Box(new Rect(eye * Screen.width * 0.5f + 10, Screen.height * 0.82f,
                Screen.width * 0.5f - 20, Screen.height * 0.12f), text, style);
    }
}
