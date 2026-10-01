using System;
using System.Collections;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

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
    [SerializeField] private bool flipHorizontal;
    [SerializeField] private bool flipVertical;

    private Worker worker;
    private Color32[] sourcePixels;
    private float[] inputData;
    private bool running;

    public IReadOnlyList<FootDetection> LatestDetections { get; private set; } = Array.Empty<FootDetection>();
    public bool IsReady => worker != null && cameraSource != null && cameraSource.IsReady;
    public event Action<IReadOnlyList<FootDetection>> DetectionsUpdated;

    private void Start()
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
        WaitForSeconds wait = new WaitForSeconds(1f / Mathf.Max(1f, inferenceFps));
        while (enabled)
        {
            if (!running && cameraSource != null && cameraSource.IsReady)
            {
                running = true;
                RunInference(cameraSource.Texture);
                running = false;
            }

            yield return wait;
        }
    }

    private void RunInference(WebCamTexture source)
    {
        PrepareInput(source);
        using Tensor<float> input = new Tensor<float>(
            new TensorShape(1, 3, inputSize, inputSize),
            inputData
        );

        worker.Schedule(input);
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null) return;

        float[] values = output.DownloadToArray();
        LatestDetections = DecodeAndSuppress(values);
        DetectionsUpdated?.Invoke(LatestDetections);
    }

    private void PrepareInput(WebCamTexture source)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        int required = sourceWidth * sourceHeight;
        if (sourcePixels == null || sourcePixels.Length != required) sourcePixels = new Color32[required];
        source.GetPixels32(sourcePixels);

        int plane = inputSize * inputSize;
        int rotation = ((source.videoRotationAngle % 360) + 360) % 360;
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

                if (source.videoVerticallyMirrored) rawV = 1f - rawV;
                int sourceX = Mathf.Clamp((int)(rawU * sourceWidth), 0, sourceWidth - 1);
                int sourceY = Mathf.Clamp((int)(rawV * sourceHeight), 0, sourceHeight - 1);
                Color32 pixel = sourcePixels[sourceY * sourceWidth + sourceX];
                int index = y * inputSize + x;
                inputData[index] = pixel.r / 255f;
                inputData[plane + index] = pixel.g / 255f;
                inputData[2 * plane + index] = pixel.b / 255f;
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

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}
