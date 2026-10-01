using UnityEngine;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class PhoneStereoRig : MonoBehaviour
{
    [Header("Camara base")]
    [SerializeField] private Camera sourceCamera;

    [Header("Vista estereoscopica")]
    [SerializeField, Range(0.04f, 0.08f)]
    private float interpupillaryDistance = 0.064f;

    [SerializeField, Range(0f, 0.02f)]
    private float centerGap = 0.002f;

    [SerializeField] private bool useToeIn;

    [SerializeField, Min(0.25f)]
    private float convergenceDistance = 2f;

    [Header("Telefono")]
    [SerializeField] private bool forceLandscape = true;
    [SerializeField] private bool keepScreenAwake = true;
    [SerializeField] private bool forceFullScreen = true;

    [Header("Diagnostico")]
    [SerializeField] private bool showAlignmentGuide = true;

    private Camera leftEye;
    private Camera rightEye;
    private PhoneStereoOverlay overlay;
    private Component arCameraBackground;
    private Transform arBackgroundPlane;
    private MeshRenderer arBackgroundRenderer;
    private PropertyInfo arBackgroundMaterialProperty;

    public Camera LeftEye => leftEye;
    public Camera RightEye => rightEye;
    public Camera SourceCamera => sourceCamera;
    public bool IsReady => leftEye != null && rightEye != null;

    private void Reset()
    {
        sourceCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        ConfigurePhone();
        BuildRig();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && !IsReady)
        {
            BuildRig();
        }
    }

    private void LateUpdate()
    {
        if (!IsReady || sourceCamera == null)
        {
            return;
        }

        SynchronizeEye(leftEye, true);
        SynchronizeEye(rightEye, false);
        UpdateArCameraBackground();

        if (overlay != null)
        {
            overlay.ShowGuide = showAlignmentGuide;
        }
    }

    public void BuildRig()
    {
        if (sourceCamera == null)
        {
            sourceCamera = GetComponent<Camera>();
        }

        if (sourceCamera == null)
        {
            Debug.LogError("PhoneStereoRig requiere una Camera en el mismo objeto o en Source Camera.", this);
            enabled = false;
            return;
        }

        leftEye = FindOrCreateEye("PhoneStereoLeftEye");
        rightEye = FindOrCreateEye("PhoneStereoRightEye");

        SynchronizeEye(leftEye, true);
        SynchronizeEye(rightEye, false);

        sourceCamera.enabled = false;

        PrepareArCameraBackground();
        ConfigureTrackedPoseDriver();

        overlay = GetComponent<PhoneStereoOverlay>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<PhoneStereoOverlay>();
        }

        overlay.ShowGuide = showAlignmentGuide;
    }

    private void ConfigureTrackedPoseDriver()
    {
        TrackedPoseDriver driver = GetComponent<TrackedPoseDriver>();
        if (driver == null)
        {
            return;
        }

        if (driver.positionInput.action == null || driver.positionInput.action.bindings.Count == 0)
        {
            driver.positionInput = new InputActionProperty(
                new InputAction("Posicion XR", InputActionType.Value, "<XRHMD>/centerEyePosition")
            );
        }

        if (driver.rotationInput.action == null || driver.rotationInput.action.bindings.Count == 0)
        {
            driver.rotationInput = new InputActionProperty(
                new InputAction("Rotacion XR", InputActionType.Value, "<XRHMD>/centerEyeRotation")
            );
        }
    }

    private void PrepareArCameraBackground()
    {
        Component[] components = sourceCamera.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component != null && component.GetType().FullName ==
                "UnityEngine.XR.ARFoundation.ARCameraBackground")
            {
                arCameraBackground = component;
                arBackgroundMaterialProperty = component.GetType().GetProperty(
                    "material", BindingFlags.Instance | BindingFlags.Public
                );
                break;
            }
        }

        if (arCameraBackground == null || arBackgroundPlane != null)
        {
            return;
        }

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "ARCameraStereoBackground";
        plane.transform.SetParent(transform, false);
        arBackgroundPlane = plane.transform;
        arBackgroundRenderer = plane.GetComponent<MeshRenderer>();

        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider != null)
        {
            Destroy(planeCollider);
        }

        ResizeArBackground();
    }

    private void UpdateArCameraBackground()
    {
        if (arBackgroundRenderer == null || arBackgroundMaterialProperty == null)
        {
            return;
        }

        Material material = arBackgroundMaterialProperty.GetValue(arCameraBackground) as Material;
        if (material != null && arBackgroundRenderer.sharedMaterial != material)
        {
            arBackgroundRenderer.sharedMaterial = material;
        }

        ResizeArBackground();
    }

    private void ResizeArBackground()
    {
        if (arBackgroundPlane == null || sourceCamera == null)
        {
            return;
        }

        float distance = Mathf.Min(100f, sourceCamera.farClipPlane - 1f);
        float eyeAspect = Screen.height > 0
            ? (Screen.width * 0.5f) / Screen.height
            : sourceCamera.aspect * 0.5f;
        float height = 2f * distance *
            Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        arBackgroundPlane.localPosition = new Vector3(0f, 0f, distance);
        arBackgroundPlane.localRotation = Quaternion.identity;
        arBackgroundPlane.localScale = new Vector3(height * eyeAspect, height, 1f);
    }

    public void SetAlignmentGuideVisible(bool visible)
    {
        showAlignmentGuide = visible;
        if (overlay != null)
        {
            overlay.ShowGuide = visible;
        }
    }

    private Camera FindOrCreateEye(string eyeName)
    {
        Transform child = transform.Find(eyeName);
        GameObject eyeObject;

        if (child == null)
        {
            eyeObject = new GameObject(eyeName);
            eyeObject.transform.SetParent(transform, false);
        }
        else
        {
            eyeObject = child.gameObject;
        }

        Camera eyeCamera = eyeObject.GetComponent<Camera>();
        if (eyeCamera == null)
        {
            eyeCamera = eyeObject.AddComponent<Camera>();
        }

        AudioListener listener = eyeObject.GetComponent<AudioListener>();
        if (listener != null)
        {
            Destroy(listener);
        }

        return eyeCamera;
    }

    private void SynchronizeEye(Camera eye, bool isLeft)
    {
        bool wasEnabled = eye.enabled;
        eye.CopyFrom(sourceCamera);
        eye.enabled = wasEnabled || Application.isPlaying;
        eye.targetTexture = null;

        float halfGap = centerGap * 0.5f;
        eye.rect = isLeft
            ? new Rect(0f, 0f, 0.5f - halfGap, 1f)
            : new Rect(0.5f + halfGap, 0f, 0.5f - halfGap, 1f);

        // Cada ojo usa media pantalla y necesita su propia relacion de aspecto.
        float viewportWidth = Mathf.Max(1f, Screen.width * eye.rect.width);
        float viewportHeight = Mathf.Max(1f, Screen.height * eye.rect.height);
        eye.aspect = viewportWidth / viewportHeight;

        float eyeOffset = interpupillaryDistance * 0.5f * (isLeft ? -1f : 1f);
        eye.transform.localPosition = new Vector3(eyeOffset, 0f, 0f);

        if (useToeIn)
        {
            float yaw = Mathf.Atan2(-eyeOffset, convergenceDistance) * Mathf.Rad2Deg;
            eye.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            eye.transform.localRotation = Quaternion.identity;
        }
    }

    private void ConfigurePhone()
    {
        if (forceLandscape)
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        if (keepScreenAwake)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (forceFullScreen)
        {
            Screen.fullScreen = true;
        }
    }

    private void OnDestroy()
    {
        if (sourceCamera != null)
        {
            sourceCamera.enabled = true;
        }
    }
}
