using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class FootCameraSource : MonoBehaviour
{
    [SerializeField] private RawImage preview;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;
    [SerializeField] private bool useFrontCamera;

    private WebCamTexture cameraTexture;
    private string status = "Camara sin iniciar";

    public WebCamTexture Texture => cameraTexture;
    public bool IsReady => cameraTexture != null && cameraTexture.isPlaying && cameraTexture.width > 16;
    public string Status => status;

    private void OnEnable()
    {
        StartCoroutine(StartCamera());
    }

    private IEnumerator StartCamera()
    {
        status = "Solicitando permiso de camara";
        yield return RequestCameraPermission();
        if (!HasCameraPermission())
        {
            status = "Permiso de camara denegado";
            Debug.LogError("FootCameraSource: no se concedio el permiso de camara.", this);
            yield break;
        }

        status = "Buscando camara trasera";
        WebCamDevice[] devices = WebCamTexture.devices;
        float deviceWaitUntil = Time.realtimeSinceStartup + 5f;
        while (devices.Length == 0 && Time.realtimeSinceStartup < deviceWaitUntil)
        {
            yield return new WaitForSecondsRealtime(0.25f);
            devices = WebCamTexture.devices;
        }

        if (devices.Length == 0)
        {
            status = "No se encontro una camara";
            Debug.LogError("FootCameraSource: no se encontro una camara disponible despues del permiso.", this);
            yield break;
        }

        string selected = devices[0].name;
        foreach (WebCamDevice device in devices)
        {
            if (device.isFrontFacing == useFrontCamera)
            {
                selected = device.name;
                break;
            }
        }

        cameraTexture = new WebCamTexture(selected, requestedWidth, requestedHeight, requestedFps);
        cameraTexture.Play();
        status = "Esperando imagen de camara";

        if (preview != null)
        {
            preview.texture = cameraTexture;
        }

        float frameWaitUntil = Time.realtimeSinceStartup + 15f;
        while (cameraTexture != null && cameraTexture.isPlaying && cameraTexture.width <= 16 &&
               Time.realtimeSinceStartup < frameWaitUntil)
        {
            yield return null;
        }

        if (IsReady)
        {
            status = "Camara lista";
            Debug.Log($"FootCameraSource: camara lista ({cameraTexture.width}x{cameraTexture.height}, giro {cameraTexture.videoRotationAngle}).", this);
        }
        else
        {
            status = "La camara no envio imagen";
            Debug.LogError("FootCameraSource: la camara inicio, pero no envio imagen en 15 segundos.", this);
        }
    }

    private IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Camera)) yield break;

        bool requestFinished = false;
        bool granted = false;
        PermissionCallbacks callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => { granted = true; requestFinished = true; };
        callbacks.PermissionDenied += _ => { requestFinished = true; };
        callbacks.PermissionDeniedAndDontAskAgain += _ => { requestFinished = true; };
        Permission.RequestUserPermission(Permission.Camera, callbacks);

        float permissionWaitUntil = Time.realtimeSinceStartup + 30f;
        while (!requestFinished && Time.realtimeSinceStartup < permissionWaitUntil)
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                granted = true;
                break;
            }
            yield return null;
        }

        if (granted || Permission.HasUserAuthorizedPermission(Permission.Camera)) yield break;
#elif UNITY_IOS && !UNITY_EDITOR
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#else
        yield break;
#endif
    }

    private static bool HasCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS && !UNITY_EDITOR
        return Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
        return true;
#endif
    }

    private void Update()
    {
        if (preview == null || !IsReady) return;
        preview.rectTransform.localEulerAngles = new Vector3(0f, 0f, -cameraTexture.videoRotationAngle);
        preview.uvRect = cameraTexture.videoVerticallyMirrored
            ? new Rect(0f, 1f, 1f, -1f)
            : new Rect(0f, 0f, 1f, 1f);
    }

    private void OnDisable()
    {
        if (cameraTexture != null)
        {
            if (cameraTexture.isPlaying) cameraTexture.Stop();
            if (preview != null && preview.texture == cameraTexture) preview.texture = null;
            Destroy(cameraTexture);
            cameraTexture = null;
        }
        status = "Camara detenida";
    }
}
