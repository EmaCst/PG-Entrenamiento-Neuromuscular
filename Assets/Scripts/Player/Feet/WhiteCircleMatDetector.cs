using UnityEngine;

/// <summary>Experimental white-disc/dark-outline detector. Input is the existing top-left RGB NCHW frame.</summary>
public sealed class WhiteCircleMatDetector
{
    private const int Grid = 320;
    private readonly byte[] mask = new byte[Grid * Grid];
    private readonly int[] queue = new int[Grid * Grid];

    public bool TryDetect(float[] rgb, int size, out Rect viewportRect, out float confidence)
    {
        viewportRect = default;
        confidence = 0f;
        int plane = size * size;
        if (rgb == null || rgb.Length < plane * 3 || size < Grid) return false;
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int index = Mathf.Min(size - 1, (int)((y + 0.5f) * size / Grid)) * size +
                    Mathf.Min(size - 1, (int)((x + 0.5f) * size / Grid));
                float r = rgb[index], g = rgb[plane + index], b = rgb[plane * 2 + index];
                float minimum = Mathf.Min(r, Mathf.Min(g, b));
                float maximum = Mathf.Max(r, Mathf.Max(g, b));
                mask[y * Grid + x] = (byte)((r + g + b) / 3f >= 0.55f && minimum >= 0.40f && maximum - minimum < 0.32f ? 1 : 0);
            }

        for (int seed = 0; seed < mask.Length; seed++)
        {
            if (mask[seed] != 1) continue;
            int read = 0, count = 1;
            queue[0] = seed;
            mask[seed] = 2;
            int xMin = Grid, yMin = Grid, xMax = 0, yMax = 0;
            while (read < count)
            {
                int index = queue[read++], x = index % Grid, y = index / Grid;
                xMin = Mathf.Min(xMin, x); xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
                if (x > 0) Enqueue(index - 1, ref count);
                if (x + 1 < Grid) Enqueue(index + 1, ref count);
                if (y > 0) Enqueue(index - Grid, ref count);
                if (y + 1 < Grid) Enqueue(index + Grid, ref count);
            }
            int width = xMax - xMin + 1, height = yMax - yMin + 1;
            if (count < 20 || count > Grid * Grid * 0.25f || width < 5 || height < 4 ||
                xMin == 0 || yMin == 0 || xMax == Grid - 1 || yMax == Grid - 1) continue;
            float fill = (float)count / (width * height);
            float aspect = (float)width / height;
            if (fill < 0.66f || fill > 0.88f || aspect < 0.25f || aspect > 4f) continue;
            float cx = (xMin + width * 0.5f) / Grid, cy = (yMin + height * 0.5f) / Grid;
            float rx = width * 0.5f / Grid, ry = height * 0.5f / Grid;
            int darkSamples = 0;
            float edgeTolerance = Mathf.Min(0.2f, 1.5f / Mathf.Min(width, height));
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                float dx = Mathf.Cos(angle) * rx, dy = Mathf.Sin(angle) * ry;
                float inside = Luminance(rgb, size, cx + dx * 0.75f, cy + dy * 0.75f);
                float border = 1f;
                for (int sample = 0; sample < 8; sample++)
                {
                    float radius = 0.98f - edgeTolerance + sample * (0.14f + 2f * edgeTolerance) / 7f;
                    border = Mathf.Min(border, Luminance(rgb, size, cx + dx * radius, cy + dy * radius));
                }
                if (border < 0.43f && inside - border > 0.18f) darkSamples++;
            }
            float score = darkSamples / 48f;
            if (score < 0.60f || score <= confidence) continue;
            confidence = score;
            viewportRect = new Rect(xMin / (float)Grid, 1f - (yMax + 1f) / Grid,
                width / (float)Grid, height / (float)Grid);
        }
        return confidence > 0f;
    }

    private void Enqueue(int index, ref int count)
    {
        if (mask[index] != 1) return;
        mask[index] = 2;
        queue[count++] = index;
    }

    private static float Luminance(float[] rgb, int size, float u, float v)
    {
        int plane = size * size;
        int index = Mathf.Clamp((int)(v * size), 0, size - 1) * size + Mathf.Clamp((int)(u * size), 0, size - 1);
        return (rgb[index] + rgb[plane + index] + rgb[plane * 2 + index]) / 3f;
    }
}
