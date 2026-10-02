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
    [Tooltip("Activalo si el movimiento horizontal aparece invertido.")]
    public bool flipHorizontal = true;

    [Tooltip("Intercambia las etiquetas Left y Right si la camara las reporta al reves.")]
    public bool swapHandLabels = false;

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

        var viewportX = flipHorizontal ? 1f - hand.x : hand.x;
        var viewportY = 1f - hand.y;

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

        if (swapHandLabels)
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
            var x = flipHorizontal ? 1f - hand.x : hand.x;
            var y = 1f - hand.y;
            if (x < 0f || x > 1f || y < 0f || y > 1f)
            {
                continue;
            }

            // OnGUI usa origen arriba-izquierda; el feed se duplica en ambas
            // mitades de la pantalla del telefono.
            DrawFingerMarker(x * Screen.width * 0.5f, (1f - y) * Screen.height);
            DrawFingerMarker(Screen.width * 0.5f + x * Screen.width * 0.5f,
                (1f - y) * Screen.height);
        }
    }

    private static void DrawFingerMarker(float x, float y)
    {
        const float markerSize = 18f;
        var oldColor = GUI.color;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(x - markerSize * 0.5f, y - markerSize * 0.5f,
            markerSize, markerSize), "●");
        GUI.color = oldColor;
    }
}
