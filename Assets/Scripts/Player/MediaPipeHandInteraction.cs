using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;

/// <summary>
/// Convierte las landmarks de la mano detectada por MediaPipe en una
/// interaccion con los objetivos 3D del ejercicio.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MediaPipeHandInteraction : MonoBehaviour
{
    [Header("Coordenadas de la camara")]
    [Tooltip("Ajuste manual horizontal cuando la correccion automatica esta desactivada.")]
    public bool flipHorizontal = true;

    [Tooltip("Intercambia manualmente las etiquetas Left y Right si la correccion automatica esta desactivada.")]
    public bool swapHandLabels = false;

    [Tooltip("Ajusta coordenadas y etiquetas segun los espejos que MediaPipe aplico realmente a la webcam.")]
    public bool automaticCoordinateCorrection = true;

    [Tooltip("El video que se muestra en la escena esta espejado horizontalmente.")]
    public bool displayMirroredHorizontally = false;

    [Tooltip("Rectangulo donde se muestra el video de MediaPipe. Evita errores cuando hay franjas laterales.")]
    public RectTransform videoRect;

    [Header("Deteccion de objetivos")]
    public LayerMask targetLayers = ~0;
    public float maximumDistance = 100f;
    [Tooltip("Radio extra de contacto alrededor de cada landmark, en unidades de la escena. Cero usa el contacto exacto anterior.")]
    [Min(0f)] public float landmarkContactRadius = 0.02f;
    [Tooltip("Las conexiones entre landmarks tambien pueden tocar la esfera activa.")]
    public bool detectHandConnections = true;

    [Header("Pruebas")]
    public bool drawDebugRays = false;
    [Tooltip("Muestra temporalmente cuantas manos detecta MediaPipe y la posicion de cada indice en ambas mitades de la pantalla.")]
    public bool showDiagnostics = false;
    [Tooltip("Dibuja las 21 landmarks y conexiones de cada mano sobre la camara.")]
    public bool showLandmarkOverlay = true;

    private MediaPipeStereoBackground cameraBackground;
    private Camera interactionCamera;
    private PhoneStereoRig stereoRig;
    private AimLabManager exerciseManager;
    private readonly Collider[] connectionHits = new Collider[32];
    private readonly Vector3[] videoCorners = new Vector3[4];
    private string lastRayResult = "sin mano detectada";
    private int detectedHands;
    private Texture2D overlayPixel;
    private Vector2[] overlayPoints = new Vector2[21];
    private readonly RaycastHit[] raycastHits = new RaycastHit[32];

    private static readonly int[,] LandmarkConnections =
    {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 },
        { 0, 5 }, { 5, 6 }, { 6, 7 }, { 7, 8 },
        { 5, 9 }, { 9, 10 }, { 10, 11 }, { 11, 12 },
        { 9, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 },
        { 13, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 },
        { 0, 17 }
    };

    private void Awake()
    {
        interactionCamera = GetComponent<Camera>();
        cameraBackground = GetComponent<MediaPipeStereoBackground>();
        stereoRig = GetComponent<PhoneStereoRig>();
        exerciseManager = FindFirstObjectByType<AimLabManager>();
        overlayPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        overlayPixel.SetPixel(0, 0, Color.white);
        overlayPixel.Apply();
    }

    private void Update()
    {
        if (TrainingTransitionScreen.IsVisible || (cameraBackground != null && !cameraBackground.IsReady)) return;
        var hands = HandTrackingBridge.GetLatestHands();
        detectedHands = hands.Length;
        lastRayResult = detectedHands == 0 ? "sin mano detectada" : "sin objetivo bajo la mano";

        foreach (var hand in hands)
        {
            TryInteract(hand);
        }
    }

    private void TryInteract(HandTrackingBridge.TrackedHand hand)
    {
        if (string.IsNullOrEmpty(hand.handedness))
        {
            lastRayResult = "mano detectada sin etiqueta izquierda/derecha";
            return;
        }

        if (hand.landmarks == null || hand.landmarks.Length == 0)
        {
            return;
        }

        var usedHand = hand.handedness == "Left"
            ? RequiredHand.Left
            : RequiredHand.Right;

        // Se comprueban las 21 landmarks en ambas vistas. Así el contacto
        // funciona al alcanzar la pelota con cualquier parte de la mano y no
        // depende de que la punta del índice coincida exactamente en los dos ojos.
        bool flipX = ShouldFlipHorizontal();
        bool flipY = ShouldFlipVertical();
        RequiredHand correctedHand = ShouldSwapLabels() ? SwapHand(usedHand) : usedHand;
        for (var landmarkIndex = 0; landmarkIndex < hand.landmarks.Length; landmarkIndex++)
        {
            var landmark = hand.landmarks[landmarkIndex];
            var viewportX = flipX ? 1f - landmark.x : landmark.x;
            var screenYTop = flipY ? 1f - landmark.y : landmark.y;
            var viewportY = 1f - screenYTop;

            if (viewportX < 0f || viewportX > 1f || viewportY < 0f || viewportY > 1f)
            {
                continue;
            }

            for (var eyeIndex = 0; eyeIndex < 2; eyeIndex++)
            {
                var eyeCamera = GetEyeCamera(eyeIndex);
                if (eyeCamera == null) continue;

                var ray = CreateInteractionRay(eyeCamera, viewportX, viewportY);
                if (drawDebugRays)
                {
                    Debug.DrawRay(ray.origin, ray.direction * maximumDistance,
                        eyeIndex == 0 ? Color.yellow : Color.cyan);
                }

                var target = FindTargetAlong(ray);
                if (target == null) continue;

                lastRayResult = $"objetivo {target.name} | landmark {landmarkIndex} | ojo {(eyeIndex == 0 ? "izq." : "der.")}";
                target.Touch(correctedHand);
            }
        }
        if (detectHandConnections)
            TryInteractConnections(hand, correctedHand, flipX, flipY);
    }

    private void TryInteractConnections(HandTrackingBridge.TrackedHand hand, RequiredHand usedHand, bool flipX, bool flipY)
    {
        GameObject active = exerciseManager != null ? exerciseManager.ActiveTarget : null;
        if (active == null) return;

        for (int eye = 0; eye < 2; eye++)
        {
            Camera eyeCamera = GetEyeCamera(eye);
            if (eyeCamera == null) continue;
            // Proyecta el esqueleto sobre la profundidad de la esfera virtual.
            // Cada conexion se comprueba entera, sin muestrear puntos separados.
            Plane targetPlane = new Plane(eyeCamera.transform.forward, active.transform.position);
            for (int i = 0; i < LandmarkConnections.GetLength(0); i++)
            {
                int a = LandmarkConnections[i, 0];
                int b = LandmarkConnections[i, 1];
                if (a >= hand.landmarks.Length || b >= hand.landmarks.Length) continue;
                if (!ProjectLandmark(hand.landmarks[a], eyeCamera, targetPlane, flipX, flipY, out Vector3 start) ||
                    !ProjectLandmark(hand.landmarks[b], eyeCamera, targetPlane, flipX, flipY, out Vector3 end)) continue;

                int count = Physics.OverlapCapsuleNonAlloc(start, end, Mathf.Max(0.001f, landmarkContactRadius),
                    connectionHits, targetLayers);
                for (int hit = 0; hit < count; hit++)
                {
                    TargetController target = connectionHits[hit].GetComponentInParent<TargetController>();
                    if (target == null || target.gameObject != active || !target.IsActiveTarget) continue;
                    lastRayResult = $"objetivo {target.name} | conexion {a}-{b} | ojo {eye}";
                    target.Touch(usedHand);
                    return;
                }
            }
        }
    }

    private bool ProjectLandmark(HandTrackingBridge.TrackedLandmark landmark, Camera eyeCamera,
        Plane plane, bool flipX, bool flipY, out Vector3 point)
    {
        point = default;
        float x = flipX ? 1f - landmark.x : landmark.x;
        float y = 1f - (flipY ? 1f - landmark.y : landmark.y);
        if (x < 0f || x > 1f || y < 0f || y > 1f) return false;
        Ray ray = CreateInteractionRay(eyeCamera, x, y);
        if (!plane.Raycast(ray, out float distance) || distance > maximumDistance) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    private TargetController FindTargetAlong(Ray ray)
    {
        var hitCount = landmarkContactRadius > 0f
            ? Physics.SphereCastNonAlloc(ray, landmarkContactRadius, raycastHits, maximumDistance, targetLayers)
            : Physics.RaycastNonAlloc(ray, raycastHits, maximumDistance, targetLayers);
        TargetController closestTarget = null;
        var closestDistance = float.MaxValue;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = raycastHits[i];
            var candidate = hit.collider.GetComponentInParent<TargetController>();
            if (candidate != null && candidate.IsActiveTarget && hit.distance < closestDistance)
            {
                closestTarget = candidate;
                closestDistance = hit.distance;
            }
        }

        return closestTarget;
    }

    private Camera GetEyeCamera(int eyeIndex)
    {
        if (stereoRig == null || !stereoRig.IsReady)
        {
            return eyeIndex == 0 ? interactionCamera : null;
        }

        return eyeIndex == 0 ? stereoRig.LeftEye : stereoRig.RightEye;
    }

    private Ray CreateInteractionRay(Camera eyeCamera, float normalizedX, float normalizedY)
    {
        if (cameraBackground != null && cameraBackground.TryGetDisplayPoint(
            new Vector2(normalizedX, normalizedY), out Vector3 world))
        {
            // Match the actual video quad in each eye, including its inset and stereo disparity.
            return new Ray(eyeCamera.transform.position, (world - eyeCamera.transform.position).normalized);
        }
        if (videoRect == null)
        {
            return eyeCamera.ViewportPointToRay(
                new Vector3(normalizedX, normalizedY, 0f)
            );
        }

        videoRect.GetWorldCorners(videoCorners);

        var canvas = videoRect.GetComponentInParent<Canvas>();
        var canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        var bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[0]);
        var topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[2]);

        var screenPoint = new Vector2(
            Mathf.Lerp(bottomLeft.x, topRight.x, normalizedX),
            Mathf.Lerp(bottomLeft.y, topRight.y, normalizedY)
        );

        return eyeCamera.ScreenPointToRay(screenPoint);
    }

    private static RequiredHand SwapHand(RequiredHand hand)
    {
        return hand == RequiredHand.Left ? RequiredHand.Right : RequiredHand.Left;
    }

    private bool ShouldFlipHorizontal()
    {
        return automaticCoordinateCorrection
            ? (cameraBackground != null ? cameraBackground.MirrorHorizontally : displayMirroredHorizontally) ^ HandTrackingBridge.SourceFlipHorizontally
            : flipHorizontal;
    }

    private bool ShouldFlipVertical()
    {
        // Ajuste para la convención vertical de la salida de esta fuente.
        return automaticCoordinateCorrection && !HandTrackingBridge.SourceFlipVertically;
    }

    private bool ShouldSwapLabels()
    {
        return automaticCoordinateCorrection
            ? HandTrackingBridge.SourceFlipHorizontally
            : swapHandLabels;
    }

    private void OnGUI()
    {
        if ((!showDiagnostics && !showLandmarkOverlay) || Event.current.type != EventType.Repaint ||
            TrainingTransitionScreen.IsVisible || (cameraBackground != null && !cameraBackground.IsReady))
        {
            return;
        }

        GUI.color = Color.white;
        if (showDiagnostics) GUI.Box(new Rect(8f, 8f, 430f, 48f),
            $"MediaPipe: {detectedHands} mano(s) | {lastRayResult} | flip H:{HandTrackingBridge.SourceFlipHorizontally} V:{HandTrackingBridge.SourceFlipVertically}");

        var hands = HandTrackingBridge.GetLatestHands();
        foreach (var hand in hands)
        {
            if (hand.landmarks == null || hand.landmarks.Length == 0)
            {
                continue;
            }

            var handName = hand.handedness == "Left" ? "L" : "R";
            if (ShouldSwapLabels())
            {
                handName = handName == "L" ? "R" : "L";
            }

            // Coincide con el color que AimLabHandController asigna a cada mano:
            // izquierda azul y derecha roja.
            var color = handName == "L" ? new Color(0.15f, 0.55f, 1f, 0.65f) : new Color(1f, 0.25f, 0.25f, 0.65f);
            for (var eye = 0; eye < 2; eye++)
            {
                if (overlayPoints.Length != hand.landmarks.Length)
                    overlayPoints = new Vector2[hand.landmarks.Length];
                var points = overlayPoints;
                bool flipX = ShouldFlipHorizontal();
                bool flipY = ShouldFlipVertical();
                for (var i = 0; i < hand.landmarks.Length; i++)
                {
                    points[i] = LandmarkToScreen(hand.landmarks[i], eye, flipX, flipY);
                }

                if (showLandmarkOverlay && Event.current.type == EventType.Repaint)
                {
                    DrawHandConnections(points, color);
                    DrawLandmarks(points, color);
                }

                if (showDiagnostics && hand.landmarks.Length > 8)
                {
                    DrawFingerMarker(points[8].x, points[8].y, handName, color);
                }
            }
        }
    }

    private Vector2 LandmarkToScreen(HandTrackingBridge.TrackedLandmark landmark, int eye, bool flipX, bool flipY)
    {
        var x = flipX ? 1f - landmark.x : landmark.x;
        var yFromTop = flipY ? 1f - landmark.y : landmark.y;
        Camera eyeCamera = GetEyeCamera(eye);
        if (eyeCamera == null) return new Vector2(-10000f, -10000f);
        Ray ray = CreateInteractionRay(eyeCamera, x, 1f - yFromTop);
        Vector3 screen = eyeCamera.WorldToScreenPoint(ray.GetPoint(1f));
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    private void DrawHandConnections(Vector2[] points, Color color)
    {
        GUI.color = color;
        for (var i = 0; i < LandmarkConnections.GetLength(0); i++)
        {
            var startIndex = LandmarkConnections[i, 0];
            var endIndex = LandmarkConnections[i, 1];
            if (startIndex < points.Length && endIndex < points.Length)
            {
                DrawLine(points[startIndex], points[endIndex], 1.5f);
            }
        }
    }

    private void DrawLandmarks(Vector2[] points, Color color)
    {
        GUI.color = color;
        const float size = 4f;
        foreach (var point in points)
        {
            GUI.DrawTexture(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), overlayPixel);
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, float thickness)
    {
        var delta = end - start;
        var previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), overlayPixel);
        GUI.matrix = previousMatrix;
    }

    private static void DrawFingerMarker(float x, float y, string handName, Color handColor)
    {
        const float markerSize = 24f;
        var oldColor = GUI.color;
        GUI.color = handColor;
        GUI.Label(new Rect(x - markerSize * 0.5f, y - markerSize * 0.5f,
            markerSize, markerSize), handName);
        GUI.color = oldColor;
    }

    private void OnDestroy()
    {
        if (overlayPixel != null)
        {
            Destroy(overlayPixel);
        }
    }
}

