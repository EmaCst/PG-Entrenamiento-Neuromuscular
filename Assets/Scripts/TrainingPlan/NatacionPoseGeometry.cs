using System;

public static class NatacionPoseGeometry
{
    // Scalars keep the training-plan assembly independent of UnityEngine.
    public static bool SegmentTouchesCircle(float ax, float ay, float bx, float by,
        float cx, float cy, float radius)
    {
        float dx = bx - ax, dy = by - ay;
        float lengthSquared = dx * dx + dy * dy;
        float t = lengthSquared > 0.000001f
            ? Math.Max(0f, Math.Min(1f, ((cx - ax) * dx + (cy - ay) * dy) / lengthSquared)) : 0f;
        float x = ax + dx * t - cx, y = ay + dy * t - cy;
        return x * x + y * y <= radius * radius;
    }
}
