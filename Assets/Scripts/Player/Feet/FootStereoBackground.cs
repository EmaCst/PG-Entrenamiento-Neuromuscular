using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class FootStereoBackground : MonoBehaviour
{
    [SerializeField] private FootCameraSource cameraSource;
    [SerializeField, Min(5f)] private float backgroundDistance = 100f;
    [SerializeField] private bool mirrorHorizontally = true;
    [SerializeField, Min(5f)] private float cameraStartupTimeout = 25f;

    private Camera sourceCamera;
    private Transform backgroundPlane;
    private Material backgroundMaterial;
    private string startupFailure;

    private IEnumerator Start()
    {
        sourceCamera = GetComponent<Camera>();
        if (cameraSource == null) cameraSource = GetComponent<FootCameraSource>();
        sourceCamera.clearFlags = CameraClearFlags.SolidColor;
        sourceCamera.backgroundColor = Color.black;

        float deadline = Time.realtimeSinceStartup + cameraStartupTimeout;
        while (cameraSource == null || !cameraSource.IsReady)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                startupFailure = cameraSource != null
                    ? "No se pudo iniciar la cámara: " + cameraSource.Status
                    : "No se encontró la fuente de cámara de pies en esta escena.";
                Debug.LogError("FootStereoBackground: " + startupFailure, this);
                yield break;
            }
            yield return null;
        }
        CreatePlane();
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(startupFailure)) return;
        GUI.color = Color.white;
        GUI.Box(new Rect(8f, 62f, Mathf.Max(220f, Screen.width - 16f), 58f), startupFailure);
    }

    private void LateUpdate()
    {
        if (backgroundPlane == null || cameraSource == null || !cameraSource.IsReady) return;
        backgroundMaterial.mainTexture = cameraSource.Texture;
        ResizePlane();
    }

    private void CreatePlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "FootCameraBackground";
        plane.transform.SetParent(transform, false);
        backgroundPlane = plane.transform;
        Collider collider = plane.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        backgroundMaterial = new Material(shader) { name = "FootCameraBackgroundMaterial" };
        backgroundMaterial.mainTexture = cameraSource.Texture;
        backgroundMaterial.mainTextureScale = mirrorHorizontally ? new Vector2(-1f, 1f) : Vector2.one;
        backgroundMaterial.mainTextureOffset = mirrorHorizontally ? new Vector2(1f, 0f) : Vector2.zero;
        plane.GetComponent<MeshRenderer>().material = backgroundMaterial;
        ResizePlane();
    }

    private void ResizePlane()
    {
        float distance = Mathf.Min(backgroundDistance, sourceCamera.farClipPlane - 1f);
        float aspect = Screen.height > 0 ? (Screen.width * 0.5f) / Screen.height : 1f;
        float height = 2f * distance * Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        backgroundPlane.localPosition = new Vector3(0f, 0f, distance);
        backgroundPlane.localRotation = Quaternion.identity;
        backgroundPlane.localScale = new Vector3(height * aspect, height, 1f);
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
    }
}
