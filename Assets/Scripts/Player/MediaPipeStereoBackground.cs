using System.Collections;
using Mediapipe.Unity.Sample;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MediaPipeStereoBackground : MonoBehaviour
{
    [SerializeField, Min(5f)] private float backgroundDistance = 100f;
    [SerializeField] private bool mirrorHorizontally = true;

    private Camera sourceCamera;
    private Transform backgroundPlane;
    private Material backgroundMaterial;

    private IEnumerator Start()
    {
        sourceCamera = GetComponent<Camera>();

        yield return new WaitUntil(() =>
            ImageSourceProvider.ImageSource != null &&
            ImageSourceProvider.ImageSource.isPrepared &&
            ImageSourceProvider.ImageSource.GetCurrentTexture() != null
        );

        CreateBackgroundPlane();
        UpdateTexture();
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

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        backgroundMaterial = new Material(shader)
        {
            name = "MediaPipeCameraBackgroundMaterial"
        };

        plane.GetComponent<MeshRenderer>().material = backgroundMaterial;
        ResizePlane();
    }

    private void ResizePlane()
    {
        float distance = Mathf.Min(backgroundDistance, sourceCamera.farClipPlane - 1f);
        float eyeAspect = Screen.height > 0
            ? (Screen.width * 0.5f) / Screen.height
            : sourceCamera.aspect * 0.5f;
        float height = 2f * distance *
            Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        backgroundPlane.localPosition = new Vector3(0f, 0f, distance);
        backgroundPlane.localRotation = Quaternion.identity;
        backgroundPlane.localScale = new Vector3(height * eyeAspect, height, 1f);
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
        backgroundMaterial.mainTextureScale = mirrorHorizontally
            ? new Vector2(-1f, 1f)
            : Vector2.one;
        backgroundMaterial.mainTextureOffset = mirrorHorizontally
            ? new Vector2(1f, 0f)
            : Vector2.zero;
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null)
        {
            Destroy(backgroundMaterial);
        }
    }
}
