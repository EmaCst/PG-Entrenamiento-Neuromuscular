using System;
using System.Collections;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using Screen = UnityEngine.Screen;
using Rect = UnityEngine.Rect;

// One upright, unmirrored input. Mirror ONLY its presentation and landmark x.
// One inference in flight; native callbacks copy values, never touch Unity objects.
public sealed class NatacionPoseSource : BaseRunner
{
    public static bool SingleView => UnityEngine.Object.FindFirstObjectByType<NatacionPoseSource>() != null;
    public struct Point { public float x, y, z, confidence; }
    [SerializeField, Range(5, 30)] private int inferenceFps = 15;
    public readonly Point[] Points = new Point[33];
    public readonly Point[] WorldPoints = new Point[33];
    private readonly Point[] pendingWorldPoints = new Point[33];
    public bool HasWorldPose { get; private set; }
    private bool pendingWorldValid;
    private readonly Point[] pendingPoints = new Point[33];
    private readonly object resultLock = new object();
    private readonly NatacionBodyContinuity bodyContinuity = new NatacionBodyContinuity();
    public bool BodyLost => bodyContinuity.IsLost(Time.realtimeSinceStartupAsDouble);
    private PoseLandmarker landmarker;
    private TextureFrame frame;
    private Image input;
    private WebCamSource source;
    private RenderTexture upright;
    private Material orient;
    private UnityEngine.UI.RawImage preview;
    private GameObject previewCanvas;
    private AsyncGPUReadbackRequest readback;
    private bool reading, inFlight, resultAvailable, accepting, pendingValid, paused;
    private double submittedAt, pendingCapturedAt;
    private int rotation, inputWidth, inputHeight;
    private long timestamp;
    public int Sequence { get; private set; }
    public double CapturedAt { get; private set; }
    public bool HasPose { get; private set; }
    public bool HasError { get; private set; }
    public string Status { get; private set; } = "Preparando cámara de selfies…";
    public bool IsFresh => HasPose && !paused && Time.realtimeSinceStartupAsDouble - CapturedAt <= 0.35;
    public float Aspect => upright != null ? (float)upright.width / upright.height : 1f;
    public Rect VideoRect
    {
        get
        {
            Rect safe = Screen.safeArea;
            safe.y = Screen.height - safe.yMax;
            float width = Mathf.Min(safe.width, safe.height * Aspect);
            float height = width / Aspect;
            return new Rect(safe.center.x - width / 2, safe.center.y - height / 2, width, height);
        }
    }
    public Vector2 DisplayPoint(int index)
    {
        var p = Points[index]; Rect r = VideoRect;
        return new Vector2(r.x + (1 - p.x) * r.width, r.y + p.y * r.height);
    }
    public bool Visible(int index) => IsFresh && Points[index].confidence >= 0.6f &&
        Points[index].x > 0.02f && Points[index].x < 0.98f && Points[index].y > 0.02f && Points[index].y < 0.98f;

    private void Awake() => NatacionPortrait.Configure();
    public override void Play()
    {
        base.Play(); accepting = true;
        StartCoroutine(Run());
    }
    private IEnumerator Run()
    {
        // Let Android finish switching before starting the webcam's fixed-size input.
        while (Application.isMobilePlatform && Screen.width > Screen.height)
        { Status = "Preparando la cámara en vertical…"; yield return null; }
        ImageSourceProvider.Switch(ImageSourceType.WebCamera);
        source = ImageSourceProvider.ImageSource as WebCamSource;
        if (source == null) { HasError = true; Status = "No se pudo abrir la cámara."; yield break; }
        source.RequireFrontFacing = true;
        yield return source.Play();
        if (!source.isPlaying || !source.isFrontFacing || source.textureWidth <= 16)
        { HasError = true; Status = "No se pudo abrir la cámara de selfies. Revisa el permiso de cámara."; yield break; }
        // Some Android cameras publish dimensions before their rotation/flip metadata.
        // Wait for three matching video frames before fixing the inference geometry.
        int stableFrames=0, lastRotation=-1, lastWidth=0, lastHeight=0;
        bool lastFlip=false;
        double metadataDeadline=Time.realtimeSinceStartupAsDouble+4;
        while(stableFrames<3 && Time.realtimeSinceStartupAsDouble<metadataDeadline)
        {
            var webcam=source.GetCurrentTexture() as WebCamTexture;
            if(webcam==null || webcam.didUpdateThisFrame)
            {
                int angle=(int)source.rotation,wNow=source.textureWidth,hNow=source.textureHeight;
                bool flipNow=source.isVerticallyFlipped;
                stableFrames=angle==lastRotation && wNow==lastWidth && hNow==lastHeight && flipNow==lastFlip?stableFrames+1:0;
                lastRotation=angle;lastWidth=wNow;lastHeight=hNow;lastFlip=flipNow;
            }
            yield return null;
        }
        rotation = (int)source.rotation; inputWidth = source.textureWidth; inputHeight = source.textureHeight;
        bool sideways = rotation == 90 || rotation == 270;
        float aspect = sideways ? (float)inputHeight / inputWidth : (float)inputWidth / inputHeight;
        int w = aspect >= 1 ? 640 : Mathf.RoundToInt(640 * aspect);
        int h = aspect >= 1 ? Mathf.RoundToInt(640 / aspect) : 640;
        upright = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "Natacion upright camera" };
        upright.Create();
        previewCanvas = new GameObject("Selfie preview", typeof(Canvas));
        previewCanvas.transform.SetParent(transform, false);
        var canvas = previewCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -100;
        var imageObject = new GameObject("Video", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        imageObject.transform.SetParent(previewCanvas.transform, false);
        preview = imageObject.GetComponent<UnityEngine.UI.RawImage>();
        preview.texture = upright; preview.uvRect = new Rect(1, 0, -1, 1); preview.raycastTarget = false;
        preview.rectTransform.anchorMin = preview.rectTransform.anchorMax = new Vector2(0, 1);
        preview.rectTransform.pivot = new Vector2(0, 1);
        var shader = Resources.Load<Shader>("NatacionCameraOrient");
        if (shader == null || !shader.isSupported) { HasError = true; Status = "No se pudo preparar la imagen de cámara."; yield break; }
        orient = new Material(shader);
        frame = new TextureFrame(w, h, TextureFormat.RGBA32);
        Status = "Cargando seguimiento del cuerpo…";
        yield return AssetLoader.PrepareAssetAsync("pose_landmarker_full.bytes");
        var config = new PoseLandmarkDetectionConfig();
        config.NumPoses = 1; config.OutputSegmentationMasks = false;
        if (GpuManager.GpuResources == null) config.Delegate = Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU;
        try { landmarker = PoseLandmarker.CreateFromOptions(config.GetPoseLandmarkerOptions(OnResult), GpuManager.GpuResources); }
        catch (Exception e) { Debug.LogException(e); HasError = true; Status = "No se pudo iniciar Pose. Vuelve al menú e inténtalo de nuevo."; }
        if (landmarker == null) yield break;
        Status = "Aléjate hasta que se vean tu cabeza, manos y pies.";
        double next = 0;
        var processing = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);
        while (accepting)
        {
            if (!paused && inFlight && Time.realtimeSinceStartupAsDouble - submittedAt > 5)
            { HasPose = false; HasError = true; Status = "El seguimiento no responde. Vuelve al menú y abre el ejercicio."; yield break; }
            if (paused || inFlight || Time.realtimeSinceStartupAsDouble < next || !source.isPlaying)
            { yield return null; continue; }
            if ((int)source.rotation != rotation || source.textureWidth != inputWidth || source.textureHeight != inputHeight)
            { HasPose = false; HasError = true; Status = "Cambió la orientación. Vuelve al menú y abre el ejercicio."; yield break; }
            UpdateVideo();
            submittedAt = Time.realtimeSinceStartupAsDouble;
            // Unity pixels start bottom-left, MediaPipe expects top-left rows.
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                reading = true; readback = frame.ReadTextureAsync(upright, false, true);
                while (!readback.done) yield return null;
                reading = false;
                if (readback.hasError) { HasError = true; Status = "No se pudo leer la cámara. Vuelve a abrir el ejercicio."; yield break; }
            }
            else frame.ReadTextureOnCPU(upright, false, true);
            input?.Dispose(); input = frame.BuildCPUImage();
            inFlight = true;
            timestamp = Math.Max(timestamp + 1, (long)(submittedAt * 1000));
            bool failed = false;
            try { landmarker.DetectAsync(input, timestamp, processing); }
            catch (Exception e) { inFlight = false; HasPose = false; HasError = true; Status = "Se interrumpió Pose. Vuelve a abrir el ejercicio."; Debug.LogException(e); failed = true; }
            if (failed) yield break;
            next = submittedAt + 1.0 / inferenceFps;
            yield return null;
        }
    }
    private void UpdateVideo()
    {
        if (orient == null || upright == null || source == null || !source.isPlaying) return;
        orient.SetFloat("_Rotation", rotation);
        orient.SetFloat("_FlipY", source.isVerticallyFlipped ? 1 : 0);
        Graphics.Blit(source.GetCurrentTexture(), upright, orient);
    }
    private void LateUpdate()
    {
        if (!paused) UpdateVideo();
        if (preview != null)
        {
            Rect rect = VideoRect;
            preview.rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
            preview.rectTransform.sizeDelta = rect.size;
        }
        lock (resultLock)
        {
            if (!resultAvailable) return;
            resultAvailable = false;
            Array.Copy(pendingPoints, Points, 33);
            Array.Copy(pendingWorldPoints, WorldPoints, 33);
            HasWorldPose = pendingWorldValid && !paused;
            HasPose = pendingValid && !paused;
            CapturedAt = pendingCapturedAt; Sequence++;
            if(HasPose) bodyContinuity.Detected(CapturedAt);
            inFlight = false;
        }
    }
    private void OnResult(PoseLandmarkerResult result, Image image, long resultTimestamp)
    {
        lock (resultLock)
        {
            if (!accepting) return;
            var landmarks = result.poseLandmarks != null && result.poseLandmarks.Count == 1 ? result.poseLandmarks[0].landmarks : null;
            pendingValid = landmarks != null && landmarks.Count == 33;
            var world = result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count == 1 ? result.poseWorldLandmarks[0].landmarks : null;
            pendingWorldValid = pendingValid && world != null && world.Count == 33;
            for (int i = 0; i < 33; i++)
            {
                pendingPoints[i] = default;
                pendingWorldPoints[i] = default;
                if (!pendingValid) continue;
                var p = landmarks[i];
                pendingPoints[i] = new Point { x = p.x, y = p.y, z = p.z,
                    confidence = Math.Min(p.visibility ?? 0f, p.presence ?? 0f) };
                if (pendingWorldValid)
                {
                    var w = world[i];
                    pendingWorldPoints[i] = new Point { x = w.x, y = w.y, z = w.z, confidence = pendingPoints[i].confidence };
                }
            }
            pendingCapturedAt = submittedAt; resultAvailable = true;
        }
    }
    private void OnApplicationPause(bool value)
    {
        paused = value; HasPose = HasWorldPose = false;
        if (value) source?.Pause();
        else if (source != null && source.isPrepared) StartCoroutine(source.Resume());
    }
    public override void Stop()
    {
        lock (resultLock) { accepting = false; HasPose = HasWorldPose = false; resultAvailable = false; }
        StopAllCoroutines(); base.Stop();
        if (reading) { readback.WaitForCompletion(); reading = false; }
        landmarker?.Close(); landmarker = null;
        input?.Dispose(); input = null;
        frame?.Dispose(); frame = null;
        if (source != null) { source.Stop(); source.RequireFrontFacing = false; }
        if (previewCanvas != null) { Destroy(previewCanvas); previewCanvas = null; preview = null; }
        if (upright != null) { upright.Release(); Destroy(upright); upright = null; }
        if (orient != null) { Destroy(orient); orient = null; }
    }
    private void OnDisable() => Stop();
}
