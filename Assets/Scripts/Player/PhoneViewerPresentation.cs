using System;
using System.Collections;
using UnityEngine;
using Unity.Profiling;

/// <summary>One final GPU pass over the complete stereo frame, including IMGUI and camera video.</summary>
[DisallowMultipleComponent]
public class PhoneViewerPresentation : MonoBehaviour
{
    private static PhoneViewerPresentation current;
    private static readonly ProfilerMarker PresentMarker = new ProfilerMarker("Praxen.Viewer.Present");
    private RenderTexture frame;
    private Material material;
    private bool applied;

    private void OnEnable()
    {
        current = this;
        Shader shader = Resources.Load<Shader>("PraxenViewerDistortion");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("Praxen: correccion del visor no disponible; se conserva la vista original.", this);
            return;
        }
        material = new Material(shader) { hideFlags = HideFlags.DontSave };
        StartCoroutine(PresentFrames());
    }

    private IEnumerator PresentFrames()
    {
        WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame;
            applied = false;
            if (!PhoneViewerSettings.Enabled || Screen.width <= 0 || Screen.height <= 0) continue;
            if (!EnsureFrame())
            {
                Debug.LogWarning("Praxen: no se pudo crear la textura del visor; se conserva la vista original.", this);
                ReleaseFrame();
                yield break;
            }
            using (PresentMarker.Auto())
            {
                // Capture the freshly rendered original, never the previous corrected frame.
                // This is a GPU texture copy: no ReadPixels, CPU readback, image encoding or disk I/O.
                ScreenCapture.CaptureScreenshotIntoRenderTexture(frame);
                material.SetTexture("_MainTex", frame);
                material.SetFloat("_ImageScale", PhoneViewerSettings.ImageScale);
                material.SetFloat("_LensSpacing", PhoneViewerSettings.LensSpacing);
                material.SetFloat("_VerticalOffset", PhoneViewerSettings.VerticalOffset);
                material.SetFloat("_Curvature", PhoneViewerSettings.Curvature);
                RenderTexture previousTarget = RenderTexture.active;
                bool previousSrgb = GL.sRGBWrite;
                GL.PushMatrix();
                try
                {
                    // Explicit display target: Camera.main may render ARCore to its own texture.
                    Graphics.SetRenderTarget((RenderTexture)null);
                    GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    GL.LoadOrtho();
                    if (!material.SetPass(0)) continue;
                    GL.Begin(GL.QUADS);
                    GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, 0f);
                    GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, 0f);
                    GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, 0f);
                    GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, 0f);
                    GL.End();
                    applied = true;
                }
                finally
                {
                    GL.PopMatrix();
                    GL.sRGBWrite = previousSrgb;
                    Graphics.SetRenderTarget(previousTarget);
                }
            }
        }
    }

    private bool EnsureFrame()
    {
        if (frame != null && frame.width == Screen.width && frame.height == Screen.height && frame.IsCreated()) return true;
        ReleaseFrame();
        frame = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Praxen Viewer Frame", filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
        };
        return frame.Create();
    }

    public static bool TryMapScreenPoint(Vector2 displayed, out Vector2 original)
    {
        original = displayed;
        if (current == null || !current.applied || !PhoneViewerSettings.Enabled) return true;
        if (!PhoneViewerSettings.TryMapToSource(new Vector2(displayed.x / Screen.width, displayed.y / Screen.height), out Vector2 uv))
            return false;
        original = new Vector2(uv.x * Screen.width, uv.y * Screen.height);
        return true;
    }

    // Scope used by exercise IMGUI controls. Black margins do not activate hidden buttons.
    public struct GuiPointerScope : IDisposable
    {
        private Event guiEvent;
        private Vector2 previous;
        public GuiPointerScope(bool map)
        {
            guiEvent = Event.current;
            previous = guiEvent != null ? guiEvent.mousePosition : Vector2.zero;
            if (guiEvent == null || !map) return;
            Vector2 screenPoint = new Vector2(previous.x, Screen.height - previous.y);
            guiEvent.mousePosition = TryMapScreenPoint(screenPoint, out Vector2 source)
                ? new Vector2(source.x, Screen.height - source.y)
                : new Vector2(-10000f, -10000f);
        }
        public void Dispose()
        {
            if (guiEvent != null) guiEvent.mousePosition = previous;
        }
    }

    private void ReleaseFrame()
    {
        if (frame == null) return;
        frame.Release();
        Destroy(frame);
        frame = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        applied = false;
        if (current == this) current = null;
        ReleaseFrame();
        if (material != null) Destroy(material);
        material = null;
    }
}
