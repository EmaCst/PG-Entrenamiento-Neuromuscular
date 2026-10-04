using System.Collections;
using Mediapipe.Unity.Sample;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MediaPipeStereoBackground : MonoBehaviour
{
    [Tooltip("Distancia virtual del plano de video. Al separarla de la convergencia de los objetivos, cada ojo recibe un pequeño desplazamiento horizontal.")]
    [SerializeField, Min(1f)] private float backgroundDistance = 6f;
    [SerializeField] private bool mirrorHorizontally = false;
    [SerializeField, Min(1f)] private float cameraStartupTimeout = 20f;

    private Camera sourceCamera;
    private Transform backgroundPlane;
    private Material backgroundMaterial;
    private string startupFailure;

    private IEnumerator Start()
    {
        sourceCamera = GetComponent<Camera>();

        // The stereo rig copies this camera's settings to both eye cameras.
        // A skybox is rendered after the custom background queue and can cover
        // the webcam quad even while MediaPipe is receiving live frames.
        sourceCamera.clearFlags = CameraClearFlags.SolidColor;
        sourceCamera.backgroundColor = Color.black;

        // Permission dialogs can take longer than the camera frame timeout.
        float permissionDeadline = Time.realtimeSinceStartup + 60f;
        float frameDeadline = -1f;
        while (!HasLiveCameraFrames())
        {
            var imageSource = ImageSourceProvider.ImageSource;
            if (imageSource != null && imageSource.isPlaying && frameDeadline < 0f)
                frameDeadline = Time.realtimeSinceStartup + cameraStartupTimeout;
            float deadline = frameDeadline >= 0f ? frameDeadline : permissionDeadline;
            if (Time.realtimeSinceStartup >= deadline)
            {
                startupFailure = "No se recibió video de la cámara. Comprueba el permiso de cámara de NeuromuscularAR y vuelve a abrir el ejercicio.";
                Debug.LogError("MediaPipeStereoBackground: " + startupFailure, this);
                yield break;
            }

            yield return null;
        }

        CreateBackgroundPlane();
        UpdateTexture();
    }

    private bool HasLiveCameraFrames()
    {
        var imageSource = ImageSourceProvider.ImageSource;
        if (imageSource == null || !imageSource.isPrepared || !imageSource.isPlaying)
        {
            return false;
        }

        Texture texture = imageSource.GetCurrentTexture();
        return texture != null && texture.width > 16 && texture.height > 16;
    }

    private void LateUpdate()
    {
        if (backgroundPlane == null || sourceCamera == null)
        {
            return;
        }

        UpdateTexture();
        ResizePlane();
    }

    private void CreateBackgroundPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "MediaPipeCameraBackground";
        plane.transform.SetParent(transform, false);
        backgroundPlane = plane.transform;

        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider != null)
        {
            Destroy(planeCollider);
        }

        Material template = Resources.Load<Material>("MediaPipeCameraBackground");
        if (template == null || template.shader == null || !template.shader.isSupported)
        {
            startupFailure = "Falta el material o un shader compatible Resources/MediaPipeCameraBackground en esta compilación.";
            Debug.LogError("MediaPipeStereoBackground: " + startupFailure, this);
            Destroy(plane);
            backgroundPlane = null;
            return;
        }

        backgroundMaterial = new Material(template);

        backgroundMaterial.name = "MediaPipeCameraBackgroundMaterial";

        plane.GetComponent<MeshRenderer>().material = backgroundMaterial;
        ResizePlane();
    }

    private void ResizePlane()
    {
        // El video de una sola camara se representa como un plano compartido
        // por ambos ojos. Mantenerlo mas cerca que el plano de convergencia
        // produce una disparidad binocular leve y controlable.
        float distance = Mathf.Min(backgroundDistance, sourceCamera.farClipPlane - 1f);
        float eyeAspect = Screen.height > 0
            ? (Screen.width * 0.5f) / Screen.height
            : sourceCamera.aspect * 0.5f;
        float height = 2f * distance *
            Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        backgroundPlane.localPosition = new Vector3(0f, 0f, distance);
        backgroundPlane.localRotation = Quaternion.identity;
        var rig = GetComponent<PhoneStereoRig>();
        float eyeCoverage = rig != null && rig.IsReady
            ? Vector3.Distance(rig.LeftEye.transform.localPosition, rig.RightEye.transform.localPosition)
            : 0f;
        backgroundPlane.localScale = new Vector3(height * eyeAspect + eyeCoverage, height, 1f);
    }

    private void UpdateTexture()
    {
        if (backgroundMaterial == null || ImageSourceProvider.ImageSource == null)
        {
            return;
        }

        Texture texture = ImageSourceProvider.ImageSource.GetCurrentTexture();
        if (texture == null)
        {
            return;
        }

        backgroundMaterial.mainTexture = texture;
        var imageSource = ImageSourceProvider.ImageSource;
        backgroundMaterial.SetFloat("_CameraRotation", (int)imageSource.rotation);
        backgroundMaterial.SetFloat("_CameraVerticalFlip", imageSource.isVerticallyFlipped ? 1f : 0f);
        if (backgroundMaterial.HasProperty("_BaseMap"))
        {
            backgroundMaterial.SetTexture("_BaseMap", texture);
        }
        backgroundMaterial.mainTextureScale = mirrorHorizontally
            ? new Vector2(-1f, 1f)
            : Vector2.one;
        backgroundMaterial.mainTextureOffset = mirrorHorizontally
            ? new Vector2(1f, 0f)
            : Vector2.zero;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(startupFailure))
        {
            return;
        }

        GUI.color = Color.white;
        GUI.Box(new Rect(8f, 62f, Mathf.Max(220f, Screen.width - 16f), 58f), startupFailure);
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null)
        {
            Destroy(backgroundMaterial);
        }
    }
}
