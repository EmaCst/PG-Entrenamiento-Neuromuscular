using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using TMPro;

[Serializable]
public class RunningAreaData
{
    public Vector3 southWest, northWest, northEast, southEast;
}

// The AR session coordinate space owns the map; it is never parented to the camera.
public class RunningAreaCalibrator : MonoBehaviour
{
    public enum WalkPhase { WaitingTracking, CreatingMap, Drawing, Confirming, Ready, Retry }
    [SerializeField] private Camera calibrationCamera;
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private LineRenderer boundaryLine;
    [SerializeField, Min(0.5f)] private float estimatedHeadHeight = 1.6f;
    [SerializeField, Min(0.1f)] private float sampleSpacing = 0.2f;
    [SerializeField, Min(0.2f)] private float closingDistance = 0.6f;
    [SerializeField, Min(1f)] private float minimumArea = 4f;
    [SerializeField, Min(1f)] private float minimumPerimeter = 6f;
    private readonly List<RunningBoundaryGeometry.Point> points = new List<RunningBoundaryGeometry.Point>();
    private XROrigin trackingOrigin;
    private Transform mapRoot;
    private Mesh wallMesh, startMesh, confirmationMesh;
    private Material wallMaterial, lineMaterial;
    private Transform confirmButton;
    private TMP_Text confirmLabel;
    private float perimeter, groundY, gazeSeconds, trackingSeconds, retryUntil;
    private Vector3 previousHead;
    private bool hadTracking, awaitingRejoin;
    public WalkPhase Phase { get; private set; }
    public bool IsCalibrated => Phase == WalkPhase.Ready;
    public int CapturedCornerCount => points.Count;
    public Transform MapRoot => mapRoot;
    public bool IsTrackingStable => ARSession.state == ARSessionState.SessionTracking;
    public string MapReferenceStatus => mapRoot != null ? "espacio de sesión AR" : "sin mapa";
    public int DetectedPlaneCount => arPlaneManager != null ? arPlaneManager.trackables.count : 0;
    public bool HasDetectedGround
    {
        get
        {
            if (arPlaneManager == null) return false;
            foreach (var plane in arPlaneManager.trackables)
                if (plane.alignment == PlaneAlignment.HorizontalUp && plane.trackingState == TrackingState.Tracking) return true;
            return false;
        }
    }
    public string GroundStatus => Status;
    public string LastCaptureFailure => "La delimitación se realiza caminando, sin tocar la pantalla.";
    public string Status { get; private set; } = "Esperando seguimiento AR. El suelo es opcional.";
    public event Action<int> CalibrationProgressChanged;
    public event Action CalibrationCompleted;

    private void Awake()
    {
        if (calibrationCamera == null) calibrationCamera = Camera.main;
        trackingOrigin = FindFirstObjectByType<XROrigin>();
        var origin = trackingOrigin;
        if (origin == null) { Status = "Falta XR Origin."; return; }
        arPlaneManager = origin.GetComponent<ARPlaneManager>();
        if (arPlaneManager == null) arPlaneManager = origin.gameObject.AddComponent<ARPlaneManager>();
        arPlaneManager.enabled = true;
        arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
    }
    private void Update()
    {
        if (calibrationCamera == null || IsCalibrated) return;
        if ((Phase == WalkPhase.Drawing || Phase == WalkPhase.Confirming) && mapRoot == null)
        { Retry("Se perdió la referencia del mapa. Volveremos a delimitar."); return; }
        if (Phase == WalkPhase.Retry)
        { if (Time.unscaledTime >= retryUntil) ResetCalibration(); return; }
        if (!IsTrackingStable)
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || manager.activeLoader == null) Status = "AR sin proveedor XR activo.";
            else if (ARSession.state != ARSessionState.SessionTracking)
                Status = $"Sesión AR: {ARSession.state} ({ARSession.notTrackingReason}). Mantente quieto un momento.";
            if (Phase == WalkPhase.Drawing && points.Count > 1) awaitingRejoin = true;
            trackingSeconds = 0f; gazeSeconds = 0f; hadTracking = false;
            return;
        }
        if (Phase == WalkPhase.WaitingTracking)
        {
            trackingSeconds += Time.unscaledDeltaTime;
            Status = "Seguimiento detectado. Preparando el recorrido…";
            if (trackingSeconds >= 1f) BeginMap();
            return;
        }
        if (Phase == WalkPhase.CreatingMap) return;
        Vector3 head = calibrationCamera.transform.position;
        Vector3 localHead = mapRoot.InverseTransformPoint(head);
        // Compare in the same session-map frame as the polygon. Shared world-origin corrections
        // must not be mistaken for the user teleporting.
        if (Phase == WalkPhase.Drawing && hadTracking &&
            Vector2.Distance(new Vector2(localHead.x, localHead.z), new Vector2(previousHead.x, previousHead.z)) > 0.8f)
            awaitingRejoin = true;
        previousHead = localHead;
        hadTracking = true;
        if (Phase == WalkPhase.Drawing && awaitingRejoin && points.Count > 0)
        {
            var last = points[points.Count - 1];
            float gap = Vector2.Distance(new Vector2(localHead.x, localHead.z), new Vector2(last.X, last.Z));
            if (gap > 0.45f)
            {
                Status = $"Recorrido conservado ({perimeter:F1} m). Vuelve al extremo de la línea para continuar: {gap:F1} m.";
                return;
            }
            awaitingRejoin = false;
        }
        if (Phase == WalkPhase.Drawing) Record(head);
        else if (Phase == WalkPhase.Confirming) ConfirmByGaze();
    }
    private void BeginMap()
    {
        if (trackingOrigin == null || arPlaneManager == null)
        { Retry("No hay origen AR para crear el mapa."); return; }
        Phase = WalkPhase.CreatingMap;
        groundY = calibrationCamera.transform.position.y - estimatedHeadHeight;
        float best = float.MaxValue;
        foreach (var plane in arPlaneManager.trackables)
        {
            float height = calibrationCamera.transform.position.y - plane.transform.position.y;
            float distance = Vector2.Distance(new Vector2(plane.transform.position.x, plane.transform.position.z),
                new Vector2(calibrationCamera.transform.position.x, calibrationCamera.transform.position.z));
            if (plane.alignment != PlaneAlignment.HorizontalUp || plane.trackingState != TrackingState.Tracking || height < 0.7f || height > 2.5f) continue;
            if (distance < best) { best = distance; groundY = plane.transform.position.y; }
        }
        try
        {
            if (trackingOrigin == null) { Retry("No hay origen AR para crear el mapa."); return; }
            Vector3 position = calibrationCamera.transform.position; position.y = groundY;
            var root = new GameObject("Running session map");
            root.transform.SetParent(trackingOrigin.TrackablesParent != null ? trackingOrigin.TrackablesParent : trackingOrigin.transform, false);
            root.transform.SetPositionAndRotation(position, Quaternion.identity);
            mapRoot = root.transform;
            CreateVisuals(); points.Add(new RunningBoundaryGeometry.Point(0f, 0f));
            previousHead = mapRoot.InverseTransformPoint(calibrationCamera.transform.position); hadTracking = true;
            Phase = WalkPhase.Drawing; RefreshVisuals(false);
            CalibrationProgressChanged?.Invoke(points.Count);
        }
        catch (Exception error)
        { Debug.LogException(error); Retry("No se pudo crear la visualización del mapa. Se reintentará."); }
    }
    private void Record(Vector3 head)
    {
        Vector3 local = mapRoot.InverseTransformPoint(head);
        var next = new RunningBoundaryGeometry.Point(local.x, local.z);
        var last = points[points.Count - 1];
        float distance = Vector2.Distance(new Vector2(last.X, last.Z), new Vector2(next.X, next.Z));
        float toStart = Vector2.Distance(new Vector2(next.X, next.Z), new Vector2(points[0].X, points[0].Z));
        Status = $"Camina por el borde. Recorrido: {perimeter:F1} m. Vuelve al inicio para cerrar.";
        if (perimeter >= minimumPerimeter && points.Count >= 8 && toStart <= closingDistance)
        {
            if (!RunningBoundaryGeometry.IsSimple(points) || RunningBoundaryGeometry.Area(points) < minimumArea ||
                !RunningBoundaryGeometry.TryPoint(points, 0.5f, 0.5f, 0.5f, out _))
            { Retry("El contorno se cruza o es demasiado estrecho. Volveremos a delimitar."); return; }
            Phase = WalkPhase.Confirming; RefreshVisuals(true); CreateConfirmation(); return;
        }
        if (distance < sampleSpacing) return;
        if (distance > 0.8f)
        {
            awaitingRejoin = true;
            Status = "Recorrido conservado. Vuelve al extremo de la línea para continuar.";
            return;
        }
        if (points.Count >= 256)
        { Status = "Límite del recorrido alcanzado. Vuelve al inicio para cerrar el área."; return; }
        perimeter += distance; points.Add(next); RefreshVisuals(false);
        CalibrationProgressChanged?.Invoke(points.Count);
    }
    private void CreateVisuals()
    {
        var lineObject = new GameObject("Walked boundary"); lineObject.transform.SetParent(mapRoot, false);
        boundaryLine = lineObject.AddComponent<LineRenderer>(); boundaryLine.useWorldSpace = false;
        lineMaterial = new Material(Shader.Find("Sprites/Default")); boundaryLine.sharedMaterial = lineMaterial;
        boundaryLine.widthMultiplier = 0.04f; boundaryLine.startColor = boundaryLine.endColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        var start = RunningVisualMesh.Disc("Walk start", 0.15f, lineMaterial, out startMesh);
        start.transform.SetParent(mapRoot, false);
        start.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        var wall = new GameObject("Translucent boundary wall", typeof(MeshFilter), typeof(MeshRenderer)); wall.transform.SetParent(mapRoot, false);
        wallMesh = new Mesh(); wallMesh.MarkDynamic(); wall.GetComponent<MeshFilter>().sharedMesh = wallMesh;
        wallMaterial = new Material(Resources.Load<Material>("RunningBoundaryWall"));
        wall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
    }
    private void RefreshVisuals(bool close)
    {
        if (boundaryLine == null) return;
        int segments = points.Count - 1 + (close ? 1 : 0);
        boundaryLine.positionCount = points.Count; boundaryLine.loop = close;
        for (int i = 0; i < points.Count; i++) boundaryLine.SetPosition(i, new Vector3(points[i].X, 0.025f, points[i].Z));
        var vertices = new Vector3[segments * 4]; var triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            var a = points[i]; var b = points[(i + 1) % points.Count]; int v = i * 4, t = i * 6;
            vertices[v] = new Vector3(a.X, 0f, a.Z); vertices[v + 1] = vertices[v] + Vector3.up * 0.65f;
            vertices[v + 2] = new Vector3(b.X, 0f, b.Z); vertices[v + 3] = vertices[v + 2] + Vector3.up * 0.65f;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2; triangles[t + 4] = v + 1; triangles[t + 5] = v + 3;
        }
        wallMesh.Clear(); wallMesh.vertices = vertices; wallMesh.triangles = triangles; wallMesh.RecalculateBounds();
    }
    private void CreateConfirmation()
    {
        var button = RunningVisualMesh.Quad("Confirm area by gaze", wallMaterial, out confirmationMesh);
        button.name = "Confirm area by gaze"; button.transform.SetParent(mapRoot, false); confirmButton = button.transform;
        Vector3 forward = calibrationCamera.transform.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        confirmButton.position = calibrationCamera.transform.position + forward.normalized * 1.5f;
        confirmButton.rotation = Quaternion.LookRotation(forward.normalized); confirmButton.localScale = new Vector3(0.8f, 0.3f, 1f);
        button.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        var text = new GameObject("Confirmation label", typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro)); text.transform.SetParent(confirmButton, false);
        text.transform.localPosition = new Vector3(0f, 0f, -0.01f); text.transform.localScale = Vector3.one * 0.1f;
        confirmLabel = text.GetComponent<TextMeshPro>(); confirmLabel.font = TMP_Settings.defaultFontAsset;
        confirmLabel.rectTransform.sizeDelta = new Vector2(9f, 4f); confirmLabel.fontSize = 18f;
        confirmLabel.alignment = TextAlignmentOptions.Center; confirmLabel.text = "CONFIRMAR ÁREA";
    }
    private void ConfirmByGaze()
    {
        Vector3 direction = confirmButton.position - calibrationCamera.transform.position;
        bool looking = direction.magnitude < 4f && Vector3.Angle(calibrationCamera.transform.forward, direction) < 8f;
        gazeSeconds = looking ? gazeSeconds + Time.unscaledDeltaTime : 0f;
        Status = $"Área: {RunningBoundaryGeometry.Area(points):F1} m². Mira CONFIRMAR ÁREA durante 2 segundos.";
        confirmLabel.text = looking ? $"CONFIRMAR {Mathf.CeilToInt(Mathf.Max(0, 2f - gazeSeconds))}" : "CONFIRMAR ÁREA";
        if (gazeSeconds < 2f) return;
        Destroy(confirmButton.gameObject); Phase = WalkPhase.Ready;
        Status = "Área confirmada."; CalibrationCompleted?.Invoke();
    }
    private void Retry(string message)
    { Debug.LogWarning("Running calibration: " + message); Status = message; Phase = WalkPhase.Retry; retryUntil = Time.unscaledTime + 3f; hadTracking = false; }
    public void ResetCalibration()
    {
        Phase = WalkPhase.WaitingTracking; points.Clear(); perimeter = gazeSeconds = trackingSeconds = 0f; hadTracking = awaitingRejoin = false;
        if (mapRoot != null) Destroy(mapRoot.gameObject);
        if (wallMesh != null) Destroy(wallMesh);
        if (startMesh != null) Destroy(startMesh);
        if (confirmationMesh != null) Destroy(confirmationMesh);
        if (wallMaterial != null) Destroy(wallMaterial);
        if (lineMaterial != null) Destroy(lineMaterial);
        mapRoot = null; boundaryLine = null; confirmButton = null;
        Status = "Esperando seguimiento AR. El suelo es opcional."; CalibrationProgressChanged?.Invoke(0);
    }
    private void OnDestroy() { if (mapRoot != null) Destroy(mapRoot.gameObject); if (wallMesh != null) Destroy(wallMesh); if (startMesh != null) Destroy(startMesh); if (confirmationMesh != null) Destroy(confirmationMesh); if (wallMaterial != null) Destroy(wallMaterial); if (lineMaterial != null) Destroy(lineMaterial); }
    public Vector3 GetPointInside(float u, float v, float margin)
    {
        if (!IsCalibrated || !RunningBoundaryGeometry.TryPoint(points, u, v, margin, out var p))
            throw new InvalidOperationException("No hay un punto dentro del contorno con el margen solicitado.");
        return mapRoot.TransformPoint(new Vector3(p.X, 0f, p.Z));
    }
    // Kept for the editor-only demo; the phone uses the walked polygon.
    public void SetRectangularArea(Vector3 center, float width, float depth, float y)
    {
        ResetCalibration(); mapRoot = new GameObject("Demo running map").transform; mapRoot.SetParent(transform, false);
        mapRoot.position = new Vector3(center.x, y, center.z);
        points.Add(new RunningBoundaryGeometry.Point(-width/2, -depth/2)); points.Add(new RunningBoundaryGeometry.Point(-width/2, depth/2));
        points.Add(new RunningBoundaryGeometry.Point(width/2, depth/2)); points.Add(new RunningBoundaryGeometry.Point(width/2, -depth/2));
        Phase = WalkPhase.Ready; CreateVisuals(); RefreshVisuals(true); CalibrationCompleted?.Invoke();
    }
    public RunningAreaData GetAreaData()
    {
        if (points.Count == 0 || mapRoot == null) return new RunningAreaData();
        RunningBoundaryGeometry.Bounds(points, out var min, out var max);
        return new RunningAreaData { southWest = mapRoot.TransformPoint(new Vector3(min.X,0,min.Z)), northWest = mapRoot.TransformPoint(new Vector3(min.X,0,max.Z)),
            northEast = mapRoot.TransformPoint(new Vector3(max.X,0,max.Z)), southEast = mapRoot.TransformPoint(new Vector3(max.X,0,min.Z)) };
    }
}
