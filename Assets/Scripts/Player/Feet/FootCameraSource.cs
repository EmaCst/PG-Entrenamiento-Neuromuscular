using UnityEngine;
using UnityEngine.UI;

public class FootCameraSource : MonoBehaviour
{
    [SerializeField] private RawImage preview;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;
    [SerializeField] private bool useFrontCamera;

    private WebCamTexture cameraTexture;

    public WebCamTexture Texture => cameraTexture;
    public bool IsReady => cameraTexture != null && cameraTexture.isPlaying && cameraTexture.width > 16;

    private void OnEnable()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("FootCameraSource: no se encontro una camara disponible.");
            return;
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

        if (preview != null)
        {
            preview.texture = cameraTexture;
        }
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
        if (cameraTexture != null && cameraTexture.isPlaying) cameraTexture.Stop();
    }
}
