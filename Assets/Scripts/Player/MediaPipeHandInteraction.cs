using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;

/// <summary>
/// Convierte la punta del dedo indice detectada por MediaPipe en una
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
    public bool displayMirroredHorizontally = true;

    [Tooltip("Rectangulo donde se muestra el video de MediaPipe. Evita errores cuando hay franjas laterales.")]
    public RectTransform videoRect;

    [Header("Deteccion de objetivos")]
    public LayerMask targetLayers = ~0;
    public float maximumDistance = 100f;

    [Header("Pruebas")]
    public bool drawDebugRays = true;
    [Tooltip("Muestra temporalmente cuantas manos detecta MediaPipe y la posicion de cada indice en ambas mitades de la pantalla.")]
    public bool showDiagnostics = true;

    private Camera interactionCamera;
    private Camera rayCamera;
    private PhoneStereoRig stereoRig;
    private readonly Vector3[] videoCorners = new Vector3[4];
    private string lastRayResult = "sin mano detectada";
    private int detectedHands;

    private void Awake()
    {
        interactionCamera = GetComponent<Camera>();
        stereoRig = GetComponent<PhoneStereoRig>();
    }

    private void Update()
    {
        var hands = HandTrackingBridge.GetLatestHands();
        detectedHands = hands.Length;
        lastRayResult = detectedHands == 0 ? "sin mano detectada" : "sin objetivo bajo el indice";

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

        var viewportX = ShouldFlipHorizontal() ? 1f - hand.x : hand.x;
        var screenYTop = ShouldFlipVertical() ? 1f - hand.y : hand.y;
        var viewportY = 1f - screenYTop;

        if (viewportX < 0f || viewportX > 1f ||
            viewportY < 0f || viewportY > 1f)
        {
            return;
        }

        var ray = CreateInteractionRay(viewportX, viewportY);

        if (drawDebugRays)
        {
            Debug.DrawRay(ray.origin, ray.direction * maximumDistance, Color.yellow);
        }

        // Puede haber colliders de escenario delante de los objetivos. Busca
        // el objetivo alcanzado mas cercano en vez de abortar con el primer
        // collider que no pertenece al ejercicio.
        var hits = Physics.RaycastAll(ray, maximumDistance, targetLayers);
        TargetController target = null;
        var closestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var candidate = hit.collider.GetComponentInParent<TargetController>();
            if (candidate != null && hit.distance < closestDistance)
            {
                target = candidate;
                closestDistance = hit.distance;
            }
        }

        if (target == null)
        {
            return;
        }

        lastRayResult = "objetivo detectado: " + target.name;

        var usedHand = hand.handedness == "Left"
            ? RequiredHand.Left
            : RequiredHand.Right;

        var shouldSwapLabels = automaticCoordinateCorrection
            ? !HandTrackingBridge.SourceFlipHorizontally
            : swapHandLabels;

        if (shouldSwapLabels)
        {
            usedHand = usedHand == RequiredHand.Left
                ? RequiredHand.Right
                : RequiredHand.Left;
        }

        target.Touch(usedHand);
    }

    private Ray CreateInteractionRay(float normalizedX, float normalizedY)
    {
        // El rig de telefono renderiza cada mitad con una camara de ojo cuyo
        // campo de vision es distinto al de la camara base. MediaPipe entrega
        // coordenadas de la imagen completa, que deben proyectarse dentro de
        // cada ojo, no dentro de la pantalla completa.
        rayCamera = stereoRig != null && stereoRig.LeftEye != null
            ? stereoRig.LeftEye
            : interactionCamera;

        if (videoRect == null)
        {
            return rayCamera.ViewportPointToRay(
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

        return rayCamera.ScreenPointToRay(screenPoint);
    }

    private bool ShouldFlipHorizontal()
    {
        return automaticCoordinateCorrection
            ? displayMirroredHorizontally ^ HandTrackingBridge.SourceFlipHorizontally
            : flipHorizontal;
    }

    private bool ShouldFlipVertical()
    {
        return automaticCoordinateCorrection && HandTrackingBridge.SourceFlipVertically;
    }

    private void OnGUI()
    {
        if (!showDiagnostics)
        {
            return;
        }

        GUI.color = Color.white;
        GUI.Box(new Rect(8f, 8f, 310f, 48f),
            $"MediaPipe: {detectedHands} mano(s) | {lastRayResult}");

        var hands = HandTrackingBridge.GetLatestHands();
        foreach (var hand in hands)
        {
            var x = ShouldFlipHorizontal() ? 1f - hand.x : hand.x;
            var yFromTop = ShouldFlipVertical() ? 1f - hand.y : hand.y;
            if (x < 0f || x > 1f || yFromTop < 0f || yFromTop > 1f)
            {
                continue;
            }

            // OnGUI usa origen arriba-izquierda; el feed se duplica en ambas
            // mitades de la pantalla del telefono.
            var handName = hand.handedness == "Left" ? "L" : "R";
            var labelSwap = automaticCoordinateCorrection
                ? !HandTrackingBridge.SourceFlipHorizontally
                : swapHandLabels;
            if (labelSwap)
            {
                handName = handName == "L" ? "R" : "L";
            }

            DrawFingerMarker(x * Screen.width * 0.5f, yFromTop * Screen.height, handName);
            DrawFingerMarker(Screen.width * 0.5f + x * Screen.width * 0.5f,
                yFromTop * Screen.height, handName);
        }
    }

    private static void DrawFingerMarker(float x, float y, string handName)
    {
        const float markerSize = 24f;
        var oldColor = GUI.color;
        GUI.color = handName == "L" ? Color.cyan : Color.yellow;
        GUI.Label(new Rect(x - markerSize * 0.5f, y - markerSize * 0.5f,
            markerSize, markerSize), handName);
        GUI.color = oldColor;
    }
}
