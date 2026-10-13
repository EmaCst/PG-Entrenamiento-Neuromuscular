using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

[Serializable]
public class RunningAreaData
{
    public Vector3 southWest;
    public Vector3 northWest;
    public Vector3 northEast;
    public Vector3 southEast;
}

public class RunningAreaCalibrator : MonoBehaviour
{
    [Header("Captura manual")]
    [SerializeField] private Camera calibrationCamera;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxRayDistance = 20f;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;

    [Header("Visualizacion")]
    [SerializeField] private Transform[] cornerMarkers = new Transform[4];
    [SerializeField] private LineRenderer boundaryLine;

    private readonly Vector3[] corners = new Vector3[4];
    private readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();
    private int capturedCornerCount;

    public bool IsCalibrated => capturedCornerCount == 4;
    public int CapturedCornerCount => capturedCornerCount;
    public int DetectedPlaneCount => arPlaneManager != null ? arPlaneManager.trackables.count : 0;
    public bool HasDetectedGround
    {
        get
        {
            if (arPlaneManager == null) return false;
            foreach (ARPlane plane in arPlaneManager.trackables)
                if (IsUsableGround(plane)) return true;
            return false;
        }
    }
    public string LastCaptureFailure { get; private set; } = "";
    private static bool IsUsableGround(ARPlane plane)
        => plane != null && plane.alignment == PlaneAlignment.HorizontalUp && plane.trackingState == TrackingState.Tracking;
    public string GroundStatus
    {
        get
        {
            var xr = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (xr == null || xr.activeLoader == null) return "AR sin proveedor XR activo. Comprueba ARCore para Android.";
            if (ARSession.state == ARSessionState.Unsupported) return "Este dispositivo no tiene AR disponible.";
            if (ARSession.state != ARSessionState.SessionTracking)
                return $"AR: {ARSession.state}. Esperando seguimiento ({ARSession.notTrackingReason}).";
            if (arPlaneManager == null || arPlaneManager.subsystem == null || !arPlaneManager.subsystem.running)
                return "AR tiene seguimiento, pero la detección de planos no está activa.";
            return HasDetectedGround ? "Suelo detectado." : $"Mueve el teléfono lentamente apuntando al suelo. Planos: {DetectedPlaneCount}.";
        }
    }
    private void Start() => EnsureARManagers();
    public event Action<int> CalibrationProgressChanged;
    public event Action CalibrationCompleted;

    private void Awake()
    {
        if (calibrationCamera == null)
        {
            calibrationCamera = Camera.main;
        }

        EnsureARManagers();
        EnsureVisuals();
        RefreshVisuals();
    }

    public bool CaptureNextCornerFromScreen(Vector2 screenPosition)
    {
        if (IsCalibrated || calibrationCamera == null)
        {
            return false;
        }

        LastCaptureFailure = "";
        EnsureARManagers();
        Ray ray = GetScreenRay(screenPosition);

        if (arRaycastManager != null && arRaycastManager.isActiveAndEnabled)
        {
            if (ARSession.state != ARSessionState.SessionTracking || !HasDetectedGround)
            { LastCaptureFailure = GroundStatus; return false; }
            if (arRaycastManager.subsystem == null || !arRaycastManager.subsystem.running)
            { LastCaptureFailure = "El servicio AR de rayos no está activo."; return false; }
            arHits.Clear();
            if (!arRaycastManager.Raycast(ray, arHits, TrackableType.PlaneWithinPolygon) || arHits.Count == 0)
            {
                LastCaptureFailure = "Toca dentro del suelo detectado. Ese punto está fuera de los planos.";
                return false;
            }
            foreach (var arHit in arHits)
            {
                if (!IsUsableGround(arPlaneManager.GetPlane(arHit.trackableId))) continue;
                SetNextCorner(arHit.pose.position);
                return true;
            }
            LastCaptureFailure = "El toque no corresponde a un plano de suelo horizontal con seguimiento.";
            return false;
        }

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask))
        {
            LastCaptureFailure = "No hay un detector AR activo ni una superficie física en ese punto.";
            return false;
        }

        SetNextCorner(hit.point);
        return true;
    }

    private Ray GetScreenRay(Vector2 screenPosition)
    {
        PhoneStereoRig rig = calibrationCamera.GetComponent<PhoneStereoRig>();
        if (rig == null || !rig.IsReady || Screen.width <= 0 || Screen.height <= 0)
        {
            return calibrationCamera.ScreenPointToRay(screenPosition);
        }

        bool leftEye = screenPosition.x < Screen.width * 0.5f;
        float localX = leftEye
            ? screenPosition.x / (Screen.width * 0.5f)
            : (screenPosition.x - Screen.width * 0.5f) / (Screen.width * 0.5f);
        Camera eye = leftEye ? rig.LeftEye : rig.RightEye;
        return eye.ViewportPointToRay(new Vector3(localX, screenPosition.y / Screen.height, 0f));
    }

    private void EnsureARManagers()
    {
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            return;
        }

        if (arPlaneManager == null)
        {
            arPlaneManager = origin.GetComponent<ARPlaneManager>();
            if (arPlaneManager == null) arPlaneManager = origin.gameObject.AddComponent<ARPlaneManager>();
        }

        arPlaneManager.enabled = true;
        arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

        if (arRaycastManager == null)
        {
            arRaycastManager = origin.GetComponent<ARRaycastManager>();
            if (arRaycastManager == null) arRaycastManager = origin.gameObject.AddComponent<ARRaycastManager>();
        }
        arRaycastManager.enabled = true;
    }

    private void EnsureVisuals()
    {
        if (cornerMarkers == null || cornerMarkers.Length != 4)
        {
            cornerMarkers = new Transform[4];
        }

        for (int i = 0; i < cornerMarkers.Length; i++)
        {
            if (cornerMarkers[i] != null) continue;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Esquina del area {i + 1}";
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * 0.12f;
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null) markerRenderer.material.color = new Color(0.1f, 0.9f, 1f, 1f);
            cornerMarkers[i] = marker.transform;
        }

        if (boundaryLine == null)
        {
            GameObject lineObject = new GameObject("Limite del area de carrera");
            lineObject.transform.SetParent(transform, false);
            boundaryLine = lineObject.AddComponent<LineRenderer>();
            boundaryLine.material = new Material(Shader.Find("Sprites/Default"));
            boundaryLine.startColor = new Color(0.1f, 0.9f, 1f, 1f);
            boundaryLine.endColor = boundaryLine.startColor;
            boundaryLine.widthMultiplier = 0.025f;
            boundaryLine.useWorldSpace = true;
        }
    }

    public void SetNextCorner(Vector3 worldPoint)
    {
        if (IsCalibrated)
        {
            return;
        }

        corners[capturedCornerCount] = worldPoint;
        capturedCornerCount++;
        RefreshVisuals();
        CalibrationProgressChanged?.Invoke(capturedCornerCount);

        if (IsCalibrated)
        {
            CalibrationCompleted?.Invoke();
        }
    }

    public void SetRectangularArea(Vector3 center, float width, float depth, float groundY)
    {
        float halfWidth = Mathf.Max(0.5f, width * 0.5f);
        float halfDepth = Mathf.Max(0.5f, depth * 0.5f);

        corners[0] = new Vector3(center.x - halfWidth, groundY, center.z - halfDepth);
        corners[1] = new Vector3(center.x - halfWidth, groundY, center.z + halfDepth);
        corners[2] = new Vector3(center.x + halfWidth, groundY, center.z + halfDepth);
        corners[3] = new Vector3(center.x + halfWidth, groundY, center.z - halfDepth);
        capturedCornerCount = 4;
        RefreshVisuals();
        CalibrationProgressChanged?.Invoke(capturedCornerCount);
        CalibrationCompleted?.Invoke();
    }

    public void ResetCalibration()
    {
        capturedCornerCount = 0;
        Array.Clear(corners, 0, corners.Length);
        RefreshVisuals();
        CalibrationProgressChanged?.Invoke(0);
    }

    public Vector3 GetPointInside(float normalizedX, float normalizedZ, float safetyMarginMeters)
    {
        if (!IsCalibrated)
        {
            throw new InvalidOperationException("El area de carrera no ha sido calibrada.");
        }

        float width = 0.5f * (Vector3.Distance(corners[0], corners[3]) + Vector3.Distance(corners[1], corners[2]));
        float depth = 0.5f * (Vector3.Distance(corners[0], corners[1]) + Vector3.Distance(corners[3], corners[2]));
        float marginX = Mathf.Clamp(safetyMarginMeters / Mathf.Max(width, 0.01f), 0f, 0.49f);
        float marginZ = Mathf.Clamp(safetyMarginMeters / Mathf.Max(depth, 0.01f), 0f, 0.49f);

        float x = Mathf.Lerp(marginX, 1f - marginX, Mathf.Clamp01(normalizedX));
        float z = Mathf.Lerp(marginZ, 1f - marginZ, Mathf.Clamp01(normalizedZ));
        Vector3 west = Vector3.Lerp(corners[0], corners[1], z);
        Vector3 east = Vector3.Lerp(corners[3], corners[2], z);
        return Vector3.Lerp(west, east, x);
    }

    public RunningAreaData GetAreaData()
    {
        return new RunningAreaData
        {
            southWest = corners[0],
            northWest = corners[1],
            northEast = corners[2],
            southEast = corners[3]
        };
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < cornerMarkers.Length; i++)
        {
            if (cornerMarkers[i] == null) continue;
            bool visible = i < capturedCornerCount;
            cornerMarkers[i].gameObject.SetActive(visible);
            if (visible) cornerMarkers[i].position = corners[i];
        }

        if (boundaryLine == null) return;
        boundaryLine.positionCount = capturedCornerCount < 2 ? 0 : capturedCornerCount + (IsCalibrated ? 1 : 0);
        for (int i = 0; i < capturedCornerCount; i++) boundaryLine.SetPosition(i, corners[i]);
        if (IsCalibrated) boundaryLine.SetPosition(4, corners[0]);
    }
}
