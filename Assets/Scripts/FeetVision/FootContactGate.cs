using UnityEngine;

/// <summary>Exit/re-enter and dwell measured on distinct camera observations.</summary>
public sealed class FootContactGate
{
    private float last = float.NegativeInfinity, enteredAt;
    private int clearFrames, insideFrames;
    private bool armed, completed;
    public void Reset()
    {
        last = float.NegativeInfinity;
        clearFrames = insideFrames = 0;
        armed = completed = false;
    }

    public bool Observe(float capturedAt, bool usable, bool inside, bool clearlyOutside)
    {
        if (capturedAt <= last || completed) return false;
        if (!usable || capturedAt - last > 0.4f)
        {
            clearFrames = insideFrames = 0;
            armed = false;
        }
        last = capturedAt;
        if (!usable) return false;
        if (clearlyOutside)
        {
            insideFrames = 0;
            if (++clearFrames >= 2) armed = true;
            return false;
        }
        clearFrames = 0;
        if (!inside || !armed) { insideFrames = 0; return false; }
        if (insideFrames++ == 0) enteredAt = capturedAt;
        if (insideFrames >= 2 && capturedAt - enteredAt >= 0.06f)
        {
            completed = true;
            return true;
        }
        return false;
    }

    public static bool Overlaps(Rect mat, Rect foot, float ellipseScale = 1f)
    {
        if (mat.width <= 0f || mat.height <= 0f || foot.width <= 0f || foot.height <= 0f) return false;
        // Trim the uncertain edges of the YOLO box, but allow the whole foot, not one landmark.
        float insetX = foot.width * 0.15f, insetY = foot.height * 0.15f;
        Vector2 center = mat.center;
        float x = Mathf.Clamp(center.x, foot.xMin + insetX, foot.xMax - insetX);
        float y = Mathf.Clamp(center.y, foot.yMin + insetY, foot.yMax - insetY);
        float nx = (x - center.x) / (mat.width * 0.5f * ellipseScale);
        float ny = (y - center.y) / (mat.height * 0.5f * ellipseScale);
        return nx * nx + ny * ny <= 1f;
    }
}
