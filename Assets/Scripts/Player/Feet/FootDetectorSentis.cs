using System;
using System.Collections;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;
using Unity.Profiling;

public class FootDetectorSentis : MonoBehaviour
{
    [Header("Modelo y camara")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private FootCameraSource cameraSource;
    [SerializeField] private BackendType backend = BackendType.GPUCompute;

    [Header("Inferencia YOLO11")]
    [SerializeField] private int inputSize = 640;
    [SerializeField, Range(0.05f, 0.95f)] private float confidenceThreshold = 0.40f;
    [SerializeField, Range(0.05f, 0.95f)] private float iouThreshold = 0.50f;
    [SerializeField, Range(1f, 30f)] private float inferenceFps = 10f;
    [Tooltip("Capas de YOLO programadas por fotograma. Reduce este valor si hay tirones; aumentarlo reduce la latencia.")]
    [SerializeField, Min(1)] private int layersPerFrame = 64;
    [SerializeField] private bool flipHorizontal;
    [SerializeField] private bool flipVertical;

    private Worker worker;
    private Color32[] sourcePixels;
    private float[] inputData;
    private int[] samplingIndices;
    private int samplingWidth, samplingHeight, samplingSize, samplingRotation;
    private bool samplingMirror, samplingFlipHorizontal, samplingFlipVertical;
    private Tensor<float> activeInput;
    private static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("Praxen.Feet.PrepareInput");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("Praxen.Feet.ScheduleLayers");
    private static readonly ProfilerMarker DecodeMarker = new ProfilerMarker("Praxen.Feet.DecodeOutput");

    public IReadOnlyList<FootDetection> LatestDetections { get; private set; } = Array.Empty<FootDetection>();
    public bool IsReady => worker != null && cameraSource != null && cameraSource.IsReady;
    public event Action<IReadOnlyList<FootDetection>> DetectionsUpdated;

    private void OnEnable()
    {
        if (modelAsset == null)
        {
            Debug.LogError("FootDetectorSentis: falta asignar foot_detector_best.onnx.");
            enabled = false;
            return;
        }

        Model runtimeModel = ModelLoader.Load(modelAsset);
        BackendType selectedBackend = backend;
        if (backend == BackendType.GPUCompute && !SystemInfo.supportsComputeShaders)
        {
            selectedBackend = BackendType.CPU;
            Debug.LogWarning("FootDetectorSentis: GPUCompute no disponible; se utilizara CPU.");
        }

        worker = new Worker(runtimeModel, selectedBackend);
        inputData = new float[3 * inputSize * inputSize];
        StartCoroutine(InferenceLoop());
    }

    private IEnumerator InferenceLoop()
    {
        float nextInferenceAt = 0f;
        while (isActiveAndEnabled)
        {
            if (cameraSource != null && cameraSource.IsReady && cameraSource.Texture.didUpdateThisFrame &&
                Time.realtimeSinceStartup >= nextInferenceAt)
            {
                nextInferenceAt = Time.realtimeSinceStartup + 1f / Mathf.Max(1f, inferenceFps);
                // Solo una inferencia pendiente: no se acumulan imagenes antiguas.
                yield return RunInference(cameraSource.Texture);
            }
            yield return null;
        }
    }

    private IEnumerator RunInference(WebCamTexture source)
    {
        using (PrepareMarker.Auto()) PrepareInput(source);
        activeInput = new Tensor<float>(
            new TensorShape(1, 3, inputSize, inputSize),
            inputData
        );
        try
        {
            IEnumerator schedule = worker.ScheduleIterable(activeInput);
            bool moreLayers = true;
            while (moreLayers)
            {
                using (ScheduleMarker.Auto())
                {
                    for (int i = 0; i < Mathf.Max(1, layersPerFrame); i++)
                    {
                        moreLayers = schedule.MoveNext();
                        if (!moreLayers) break;
                    }
                }
                if (moreLayers) yield return null;
            }

            Tensor<float> output = worker.PeekOutput() as Tensor<float>;
            if (output == null) yield break;
            output.ReadbackRequest();
            while (!output.IsReadbackRequestDone()) yield return null;

            // El resultado ya esta disponible: no espera a la GPU en el hilo del render.
            using (DecodeMarker.Auto())
                LatestDetections = DecodeAndSuppress(output.DownloadToArray());
            DetectionsUpdated?.Invoke(LatestDetections);
        }
        finally
        {
            activeInput?.Dispose();
            activeInput = null;
        }
    }

    private void PrepareInput(WebCamTexture source)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        int required = sourceWidth * sourceHeight;
        if (sourcePixels == null || sourcePixels.Length != required) sourcePixels = new Color32[required];
        source.GetPixels32(sourcePixels);

        int plane = inputSize * inputSize;
        if (inputData == null || inputData.Length != 3 * plane) inputData = new float[3 * plane];
        int rotation = ((source.videoRotationAngle % 360) + 360) % 360;
        bool verticallyMirrored = source.videoVerticallyMirrored;
        if (samplingIndices == null || samplingWidth != sourceWidth || samplingHeight != sourceHeight ||
            samplingSize != inputSize || samplingRotation != rotation || samplingMirror != verticallyMirrored ||
            samplingFlipHorizontal != flipHorizontal || samplingFlipVertical != flipVertical)
        {
            BuildSamplingIndices(sourceWidth, sourceHeight, rotation, verticallyMirrored);
        }

        for (int index = 0; index < plane; index++)
        {
            Color32 pixel = sourcePixels[samplingIndices[index]];
            inputData[index] = pixel.r / 255f;
            inputData[plane + index] = pixel.g / 255f;
            inputData[2 * plane + index] = pixel.b / 255f;
        }
    }

    private void BuildSamplingIndices(int sourceWidth, int sourceHeight, int rotation, bool verticallyMirrored)
    {
        samplingIndices = new int[inputSize * inputSize];
        samplingWidth = sourceWidth;
        samplingHeight = sourceHeight;
        samplingSize = inputSize;
        samplingRotation = rotation;
        samplingMirror = verticallyMirrored;
        samplingFlipHorizontal = flipHorizontal;
        samplingFlipVertical = flipVertical;
        for (int y = 0; y < inputSize; y++)
        {
            for (int x = 0; x < inputSize; x++)
            {
                float u = (x + 0.5f) / inputSize;
                float v = 1f - (y + 0.5f) / inputSize;
                if (flipHorizontal) u = 1f - u;
                if (flipVertical) v = 1f - v;

                float rawU;
                float rawV;
                switch (rotation)
                {
                    case 90:
                        rawU = v;
                        rawV = 1f - u;
                        break;
                    case 180:
                        rawU = 1f - u;
                        rawV = 1f - v;
                        break;
                    case 270:
                        rawU = 1f - v;
                        rawV = u;
                        break;
                    default:
                        rawU = u;
                        rawV = v;
                        break;
                }

                if (verticallyMirrored) rawV = 1f - rawV;
                int sourceX = Mathf.Clamp((int)(rawU * sourceWidth), 0, sourceWidth - 1);
                int sourceY = Mathf.Clamp((int)(rawV * sourceHeight), 0, sourceHeight - 1);
                samplingIndices[y * inputSize + x] = sourceY * sourceWidth + sourceX;
            }
        }
    }

    private List<FootDetection> DecodeAndSuppress(float[] output)
    {
        const int valuesPerCandidate = 5;
        int candidates = output.Length / valuesPerCandidate;
        List<FootDetection> decoded = new List<FootDetection>();

        for (int i = 0; i < candidates; i++)
        {
            float confidence = output[4 * candidates + i];
            if (confidence < confidenceThreshold) continue;

            float centerX = output[i] / inputSize;
            float centerY = output[candidates + i] / inputSize;
            float width = output[2 * candidates + i] / inputSize;
            float height = output[3 * candidates + i] / inputSize;

            Rect rect = new Rect(
                Mathf.Clamp01(centerX - width * 0.5f),
                Mathf.Clamp01(1f - centerY - height * 0.5f),
                Mathf.Clamp01(width),
                Mathf.Clamp01(height)
            );

            decoded.Add(new FootDetection { viewportRect = rect, confidence = confidence });
        }

        decoded.Sort((a, b) => b.confidence.CompareTo(a.confidence));
        List<FootDetection> kept = new List<FootDetection>(2);
        foreach (FootDetection candidate in decoded)
        {
            bool overlaps = false;
            foreach (FootDetection existing in kept)
            {
                if (IntersectionOverUnion(candidate.viewportRect, existing.viewportRect) > iouThreshold)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps) kept.Add(candidate);
            if (kept.Count == 2) break;
        }

        kept.Sort((a, b) => a.viewportRect.center.x.CompareTo(b.viewportRect.center.x));
        return kept;
    }

    private static float IntersectionOverUnion(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        float intersection = Mathf.Max(0f, xMax - xMin) * Mathf.Max(0f, yMax - yMin);
        float union = a.width * a.height + b.width * b.height - intersection;
        return union <= 0f ? 0f : intersection / union;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        activeInput?.Dispose();
        activeInput = null;
        worker?.Dispose();
        worker = null;
        LatestDetections = Array.Empty<FootDetection>();
    }
}
