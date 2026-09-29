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
    public bool flipHorizontal = false;

    [Tooltip("Intercambia las etiquetas Left y Right si la camara las reporta al reves.")]
    public bool swapHandLabels = false;

    [Tooltip("Rectangulo donde se muestra el video de MediaPipe. Evita errores cuando hay franjas laterales.")]
    public RectTransform videoRect;

    [Header("Deteccion de objetivos")]
    public LayerMask targetLayers = ~0;
    public float maximumDistance = 100f;

    [Header("Pruebas")]
    public bool drawDebugRays = true;

    private Camera interactionCamera;
    private readonly Vector3[] videoCorners = new Vector3[4];

    private void Awake()
    {
        interactionCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        var hands = HandTrackingBridge.GetLatestHands();

        foreach (var hand in hands)
        {
            TryInteract(hand);
        }
    }

    private void TryInteract(HandTrackingBridge.TrackedHand hand)
    {
        if (string.IsNullOrEmpty(hand.handedness))
        {
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

        if (!Physics.Raycast(ray, out var hit, maximumDistance, targetLayers))
        {
            return;
        }

        var target = hit.collider.GetComponentInParent<TargetController>();
        if (target == null)
        {
            return;
        }

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
        if (videoRect == null)
        {
            return interactionCamera.ViewportPointToRay(
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

        return interactionCamera.ScreenPointToRay(screenPoint);
    }
}
