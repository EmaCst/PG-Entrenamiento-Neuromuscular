using UnityEngine;
using UnityEngine.Rendering;
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
    private bool hasCameraBackgroundPass;
    private int stereoCullingMask;
    private int originalCullingMask;
    private float originalCameraDepth;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private bool originalCameraEnabled;
    private WebCamTexture editorCameraTexture;
    private Transform editorBackgroundPlane;
    private Material editorBackgroundMaterial;

    public Camera LeftEye => leftEye;
    public Camera RightEye => rightEye;
    public Camera SourceCamera => sourceCamera;
    public float ConvergenceDistance => convergenceDistance;
    public bool UsesConvergence => useToeIn;
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
        UpdateEditorCameraPreview();

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

        PrepareArCameraBackground();
        hasCameraBackgroundPass = arCameraBackground != null;
        originalCullingMask = sourceCamera.cullingMask;
        stereoCullingMask = originalCullingMask;
        originalCameraDepth = sourceCamera.depth;
        originalClearFlags = sourceCamera.clearFlags;
        originalBackgroundColor = sourceCamera.backgroundColor;
        originalCameraEnabled = sourceCamera.enabled;

        if (hasCameraBackgroundPass)
        {
            // Render the live camera background once over the full display. The
            // two eye cameras then overlay stereo objects without clearing color.
            sourceCamera.clearFlags = CameraClearFlags.SolidColor;
            sourceCamera.backgroundColor = Color.black;
            sourceCamera.depth = -2f;
            sourceCamera.cullingMask = 0;
            sourceCamera.enabled = true;
        }
        else
        {
            sourceCamera.enabled = false;
        }

        leftEye = FindOrCreateEye("PhoneStereoLeftEye");
        rightEye = FindOrCreateEye("PhoneStereoRightEye");
        SynchronizeEye(leftEye, true);
        SynchronizeEye(rightEye, false);

        ConfigureTrackedPoseDriver();
        StartEditorCameraPreview();

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
                break;
            }
        }
    }

    private void StartEditorCameraPreview()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || editorCameraTexture != null || arCameraBackground == null)
        {
            return;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("PhoneStereoRig: el Editor no encontro una webcam para previsualizar la escena AR.", this);
            return;
        }

        editorCameraTexture = new WebCamTexture(devices[0].name, 1280, 720, 30);
        editorCameraTexture.Play();

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "EditorWebcamBackground";
        plane.layer = 2; // Ignore Raycast; the base camera renders only this preview.
        plane.transform.SetParent(transform, false);
        editorBackgroundPlane = plane.transform;

        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider != null)
        {
            Destroy(planeCollider);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        editorBackgroundMaterial = new Material(shader) { name = "EditorWebcamBackgroundMaterial" };
        plane.GetComponent<MeshRenderer>().material = editorBackgroundMaterial;
        sourceCamera.cullingMask = 1 << plane.layer;
        ResizeEditorCameraPreview();
        Debug.Log($"PhoneStereoRig: usando '{devices[0].name}' como camara de vista previa en el Editor.", this);
#endif
    }

    private void UpdateEditorCameraPreview()
    {
#if UNITY_EDITOR
        if (editorCameraTexture == null || editorBackgroundMaterial == null || editorBackgroundPlane == null)
        {
            return;
        }

        if (editorCameraTexture.width > 16)
        {
            editorBackgroundMaterial.mainTexture = editorCameraTexture;
            editorBackgroundMaterial.mainTextureScale = editorCameraTexture.videoVerticallyMirrored
                ? new Vector2(1f, -1f)
                : Vector2.one;
            editorBackgroundMaterial.mainTextureOffset = editorCameraTexture.videoVerticallyMirrored
                ? new Vector2(0f, 1f)
                : Vector2.zero;
        }

        ResizeEditorCameraPreview();
#endif
    }

    private void ResizeEditorCameraPreview()
    {
        if (editorBackgroundPlane == null || sourceCamera == null) return;

        float distance = Mathf.Min(100f, sourceCamera.farClipPlane - 1f);
        float eyeAspect = Screen.height > 0
            ? (Screen.width * 0.5f) / Screen.height
            : sourceCamera.aspect * 0.5f;
        float height = 2f * distance *
            Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        editorBackgroundPlane.localPosition = new Vector3(0f, 0f, distance);
        editorBackgroundPlane.localRotation = Quaternion.identity;
        editorBackgroundPlane.localScale = new Vector3(height * eyeAspect, height, 1f);
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
        eye.cullingMask = hasCameraBackgroundPass ? stereoCullingMask : sourceCamera.cullingMask;
        if (hasCameraBackgroundPass)
        {
            // URP Base cameras clear color for Depth; Nothing preserves the
            // AR background rendered by the source camera. Viewports do not overlap.
            eye.clearFlags = GraphicsSettings.currentRenderPipeline != null
                ? CameraClearFlags.Nothing
                : CameraClearFlags.Depth;
            eye.depth = sourceCamera.depth + 1f;
        }

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
#if UNITY_EDITOR
        if (editorCameraTexture != null && editorCameraTexture.isPlaying) editorCameraTexture.Stop();
        if (editorBackgroundMaterial != null) Destroy(editorBackgroundMaterial);
#endif
        if (sourceCamera != null)
        {
            sourceCamera.cullingMask = originalCullingMask;
            sourceCamera.depth = originalCameraDepth;
            sourceCamera.clearFlags = originalClearFlags;
            sourceCamera.backgroundColor = originalBackgroundColor;
            sourceCamera.enabled = originalCameraEnabled;
        }
    }
}
