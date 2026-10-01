using UnityEngine;

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

        overlay = GetComponent<PhoneStereoOverlay>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<PhoneStereoOverlay>();
        }

        overlay.ShowGuide = showAlignmentGuide;
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
