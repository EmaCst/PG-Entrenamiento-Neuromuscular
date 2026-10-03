using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class FootStereoBackground : MonoBehaviour
{
    [SerializeField] private FootCameraSource cameraSource;
    [SerializeField, Min(1f)] private float backgroundDistance = 6f;
    [SerializeField] private bool mirrorHorizontally;
    [SerializeField, Min(5f)] private float cameraStartupTimeout = 25f;

    private Camera sourceCamera;
    private PhoneStereoRig stereoRig;
    private readonly List<Camera> backgroundCameras = new List<Camera>();
    private readonly List<Transform> backgroundPlanes = new List<Transform>();
    private Material backgroundMaterial;
    private string startupFailure;

    private IEnumerator Start()
    {
        sourceCamera = GetComponent<Camera>();
        stereoRig = GetComponent<PhoneStereoRig>();
        if (cameraSource == null) cameraSource = GetComponent<FootCameraSource>();
        sourceCamera.clearFlags = CameraClearFlags.SolidColor;
        sourceCamera.backgroundColor = Color.black;

        float missingSourceDeadline = Time.realtimeSinceStartup + cameraStartupTimeout;
        while (cameraSource == null && Time.realtimeSinceStartup < missingSourceDeadline)
        {
            yield return null;
        }

        if (cameraSource == null)
        {
            startupFailure = "No se encontró la fuente de cámara de pies en esta escena.";
            Debug.LogError("FootStereoBackground: " + startupFailure, this);
            yield break;
        }

        while (!cameraSource.IsReady && !cameraSource.StartupFailed) yield return null;
        if (!cameraSource.IsReady)
        {
            startupFailure = "No se pudo iniciar la cámara: " + cameraSource.Status;
            Debug.LogError("FootStereoBackground: " + startupFailure, this);
            yield break;
        }

        if (stereoRig != null)
        {
            while (!stereoRig.IsReady) yield return null;
            // Un unico plano anclado al centro de la camara: ambos ojos lo
            // observan desde su desplazamiento IPD y obtienen disparidad.
            CreatePlaneForCamera(stereoRig.LeftEye, "FootCameraBackgroundStereo");
        }
        else
        {
            CreatePlaneForCamera(sourceCamera, "FootCameraBackground");
        }
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(startupFailure)) return;
        GUI.color = Color.white;
        GUI.Box(new Rect(8f, 62f, Mathf.Max(220f, Screen.width - 16f), 58f), startupFailure);
    }

    private void LateUpdate()
    {
        if (backgroundPlanes.Count == 0 || backgroundMaterial == null || cameraSource == null || !cameraSource.IsReady) return;
        backgroundMaterial.mainTexture = cameraSource.Texture;
        if (backgroundMaterial.HasProperty("_BaseMap"))
            backgroundMaterial.SetTexture("_BaseMap", cameraSource.Texture);
        for (int i = 0; i < backgroundPlanes.Count; i++)
            ResizePlane(backgroundCameras[i], backgroundPlanes[i]);
    }

    private void CreatePlaneForCamera(Camera targetCamera, string planeName)
    {
        if (targetCamera == null) return;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = planeName;
        plane.layer = 2; // Ignore Raycast; each eye camera renders its own background.
        plane.transform.SetParent(sourceCamera.transform, false);
        Collider collider = plane.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Material template = Resources.Load<Material>("MediaPipeCameraBackground");
        if (template == null)
        {
            startupFailure = "Falta el material Resources/MediaPipeCameraBackground en esta compilación.";
            Debug.LogError("FootStereoBackground: " + startupFailure, this);
            Destroy(plane);
            return;
        }

        if (backgroundMaterial == null)
        {
            backgroundMaterial = new Material(template) { name = "FootCameraBackgroundMaterial" };
            backgroundMaterial.mainTextureScale = mirrorHorizontally ? new Vector2(-1f, 1f) : Vector2.one;
            backgroundMaterial.mainTextureOffset = mirrorHorizontally ? new Vector2(1f, 0f) : Vector2.zero;
        }

        backgroundMaterial.mainTexture = cameraSource.Texture;
        if (backgroundMaterial.HasProperty("_BaseMap"))
            backgroundMaterial.SetTexture("_BaseMap", cameraSource.Texture);
        plane.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;
        backgroundCameras.Add(targetCamera);
        backgroundPlanes.Add(plane.transform);
        ResizePlane(targetCamera, plane.transform);
    }

    private void ResizePlane(Camera targetCamera, Transform plane)
    {
        float distance = Mathf.Min(backgroundDistance, targetCamera.farClipPlane - 1f);
        float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        plane.localPosition = new Vector3(0f, 0f, distance);
        plane.localRotation = Quaternion.identity;
        plane.localScale = new Vector3(height * targetCamera.aspect, height, 1f);
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
    }
}
