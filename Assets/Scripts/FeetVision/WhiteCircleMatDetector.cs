using System;
using UnityEngine;

/// <summary>Bounded CPU image processing; no Unity objects, camera calls or per-frame arrays.
/// Input: normalized RGB NCHW, top-left origin. Output: bottom-left viewport coordinates.</summary>
public sealed class WhiteCircleMatDetector
{
    private const int Grid = 320;
    private const int Samples = 32;
    private readonly byte[] mask = new byte[Grid * Grid];
    private readonly float[] brightness = new float[Grid * Grid];
    private readonly int[] histogram = new int[64];
    private readonly int[] queue = new int[Grid * Grid];
    private readonly float[] cos = new float[Samples], sin = new float[Samples];

    private readonly float[] backgroundSamples = new float[40 * 40];
    private bool haveBackground;
    private Rect previousMat;
    public bool CanRetainPrevious { get; private set; }

    public WhiteCircleMatDetector()
    {
        for (int i = 0; i < Samples; i++)
        {
            cos[i] = Mathf.Cos(i * Mathf.PI * 2f / Samples);
            sin[i] = Mathf.Sin(i * Mathf.PI * 2f / Samples);
        }
    }

    public bool TryDetect(float[] rgb, int size, out Rect viewportRect, out float confidence)
    {
        viewportRect = default;
        confidence = 0f;
        if (size < Grid || rgb == null || rgb.Length < (long)size * size * 3) return false;
        int plane = size * size;
        Array.Clear(histogram, 0, histogram.Length);
        for (int y = 0; y < Grid; y++)
        {
            int sy = y * size / Grid, sy2 = Mathf.Min(size - 1, (y + 1) * size / Grid - 1);
            for (int x = 0; x < Grid; x++)
            {
                int sx = x * size / Grid, sx2 = Mathf.Min(size - 1, (x + 1) * size / Grid - 1);
                int index = sy * size + sx;
                float r = rgb[index], g = rgb[plane + index], b = rgb[2 * plane + index];
                // Preserve thin dark outlines instead of skipping them with nearest-neighbour decimation.
                float value = Mathf.Min((r + g + b) / 3f, Pixel(rgb, plane, sy * size + sx2));
                value = Mathf.Min(value, Pixel(rgb, plane, sy2 * size + sx));
                value = Mathf.Min(value, Pixel(rgb, plane, sy2 * size + sx2));
                int cell = y * Grid + x;
                brightness[cell] = value;
                histogram[Mathf.Clamp((int)(value * 63f), 0, 63)]++;
                mask[cell] = (byte)(Mathf.Max(r, Mathf.Max(g, b)) - Mathf.Min(r, Mathf.Min(g, b)) < 0.35f ? 1 : 0);
            }
        }
        CheckBackgroundMotion();
        // Blend dark/surface separation with a robust bright reference. A low threshold alone
        // merges a distant disc into the floor when its thin outline is antialiased.
        float threshold = Mathf.Clamp((Threshold() + BrightReference()) * 0.5f, 0.18f, 0.80f);
        for (int i = 0; i < mask.Length; i++)
            if (brightness[i] < threshold) mask[i] = 0;

        for (int seed = 0; seed < mask.Length; seed++)
        {
            if (mask[seed] != 1) continue;
            int read = 0, count = 1, xMin = Grid, yMin = Grid, xMax = 0, yMax = 0;
            double sumX = 0, sumY = 0, sumXX = 0, sumYY = 0, sumXY = 0;
            queue[0] = seed;
            mask[seed] = 2;
            while (read < count)
            {
                int index = queue[read++], x = index % Grid, y = index / Grid;
                xMin = Mathf.Min(xMin, x); xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
                sumX += x + 0.5; sumY += y + 0.5;
                sumXX += (x + 0.5) * (x + 0.5); sumYY += (y + 0.5) * (y + 0.5);
                sumXY += (x + 0.5) * (y + 0.5);
                if (x > 0) Enqueue(index - 1, ref count);
                if (x + 1 < Grid) Enqueue(index + 1, ref count);
                if (y > 0) Enqueue(index - Grid, ref count);
                if (y + 1 < Grid) Enqueue(index + Grid, ref count);
            }
            int width = xMax - xMin + 1, height = yMax - yMin + 1;
            if (count < 16 || count > Grid * Grid / 4 || width < 4 || height < 4 ||
                xMin == 0 || yMin == 0 || xMax == Grid - 1 || yMax == Grid - 1) continue;
            float fill = (float)count / (width * height);
            if (fill < 0.48f || fill > (Mathf.Min(width, height) < 10 ? 0.95f : 0.88f)) continue;
            float cx = (float)(sumX / count), cy = (float)(sumY / count);
            float xx = (float)(sumXX / count - cx * cx), yy = (float)(sumYY / count - cy * cy);
            float xy = (float)(sumXY / count - cx * cy);
            float d = Mathf.Sqrt((xx - yy) * (xx - yy) + 4f * xy * xy);
            float major = 2f * Mathf.Sqrt(Mathf.Max(0f, (xx + yy + d) * 0.5f)) + 0.5f;
            float minor = 2f * Mathf.Sqrt(Mathf.Max(0f, (xx + yy - d) * 0.5f)) + 0.5f;
            if (minor < 2f || minor / major < 0.22f) continue;
            float angle = 0.5f * Mathf.Atan2(2f * xy, xx - yy);
            float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
            int darkSamples = 0;
            // Validate a rotated ellipse against the original 640px input, not the reduced mask.
            for (int i = 0; i < Samples; i++)
            {
                float dx = ca * major * cos[i] - sa * minor * sin[i];
                float dy = sa * major * cos[i] + ca * minor * sin[i];
                float inside = Luminance(rgb, size, (cx + dx * 0.65f) / Grid, (cy + dy * 0.65f) / Grid);
                float border = 1f;
                float tolerance = Mathf.Min(0.30f, 1.25f / minor);
                for (int sample = 0; sample < 8; sample++)
                {
                    float radius = 1f - tolerance + sample * (0.16f + 2f * tolerance) / 7f;
                    border = Mathf.Min(border, Luminance(rgb, size, (cx + dx * radius) / Grid, (cy + dy * radius) / Grid));
                }
                if (inside - border > 0.10f && border < inside * 0.72f) darkSamples++;
            }
            float score = darkSamples / (float)Samples;
            if (score < 0.72f || score <= confidence) continue;
            confidence = score;
            // Moment fit recovers sub-grid centre and perspective; return its axis-aligned extent.
            float rx = Mathf.Sqrt(major * major * ca * ca + minor * minor * sa * sa);
            float ry = Mathf.Sqrt(major * major * sa * sa + minor * minor * ca * ca);
            viewportRect = new Rect((cx - rx) / Grid, 1f - (cy + ry) / Grid, rx * 2f / Grid, ry * 2f / Grid);
        }
        if (confidence > 0f) previousMat = viewportRect;
        return confidence > 0f;
    }

    private void CheckBackgroundMotion()
    {
        float sum = 0f, sumSquared = 0f, difference = 0f;
        int count = 0, changed = 0, sample = 0;
        Rect excluded = previousMat;
        excluded.xMin -= 0.08f; excluded.xMax += 0.08f;
        excluded.yMin -= 0.08f; excluded.yMax += 0.08f;
        for (int y = 4; y < Grid; y += 8)
            for (int x = 4; x < Grid; x += 8)
            {
                float value = brightness[y * Grid + x], previous = backgroundSamples[sample];
                backgroundSamples[sample++] = value;
                if (excluded.Contains(new Vector2(x / (float)Grid, 1f - y / (float)Grid))) continue;
                float delta = Mathf.Abs(value - previous);
                sum += previous; sumSquared += previous * previous; difference += delta;
                if (delta > 0.08f) changed++;
                count++;
            }
        float variance = count > 0 ? sumSquared / count - (sum / count) * (sum / count) : 0f;
        // A featureless floor cannot prove that the camera stayed still. Fail closed.
        CanRetainPrevious = haveBackground && count >= 256 && variance > 0.0008f &&
            difference / count < 0.025f && changed / (float)count < 0.05f;
        haveBackground = true;
    }

    private float BrightReference()
    {
        int remaining = Mathf.Max(1, mask.Length / 200);
        for (int i = histogram.Length - 1; i >= 0; i--)
        {
            remaining -= histogram[i];
            if (remaining <= 0) return i / 63f;
        }
        return 0f;
    }

    private float Threshold()
    {
        double sum = 0, lowerSum = 0, best = -1;
        int lower = 0, selected = 0;
        for (int i = 0; i < histogram.Length; i++) sum += i * histogram[i];
        for (int i = 0; i < histogram.Length - 1; i++)
        {
            lower += histogram[i]; lowerSum += i * histogram[i];
            int upper = mask.Length - lower;
            if (lower == 0 || upper == 0) continue;
            double difference = lowerSum / lower - (sum - lowerSum) / upper;
            double variance = (double)lower * upper * difference * difference;
            if (variance > best) { best = variance; selected = i; }
        }
        return selected / 63f;
    }

    private void Enqueue(int index, ref int count)
    {
        if (mask[index] != 1) return;
        mask[index] = 2; queue[count++] = index;
    }

    private static float Pixel(float[] rgb, int plane, int index)
        => (rgb[index] + rgb[plane + index] + rgb[2 * plane + index]) / 3f;

    private static float Luminance(float[] rgb, int size, float u, float v)
    {
        int index = Mathf.Clamp((int)(v * size), 0, size - 1) * size + Mathf.Clamp((int)(u * size), 0, size - 1);
        return Pixel(rgb, size * size, index);
    }
}
