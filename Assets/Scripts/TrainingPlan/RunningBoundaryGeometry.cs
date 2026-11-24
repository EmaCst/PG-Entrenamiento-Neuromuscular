using System;
using System.Collections.Generic;

// Geometry in metres, independent of Unity and AR tracking.
public static class RunningBoundaryGeometry
{
    public struct Point
    {
        public float X, Z;
        public Point(float x, float z) { X = x; Z = z; }
    }
    public static float Area(IReadOnlyList<Point> points)
    {
        double sum = 0;
        for (int i = 0; i < points.Count; i++)
        { var a = points[i]; var b = points[(i + 1) % points.Count]; sum += a.X * b.Z - b.X * a.Z; }
        return (float)Math.Abs(sum * 0.5);
    }
    public static float DistanceToSegment(Point p, Point a, Point b)
    {
        float x = b.X - a.X, z = b.Z - a.Z, d = x * x + z * z;
        float t = d < 0.000001f ? 0f : Math.Max(0f, Math.Min(1f, ((p.X - a.X) * x + (p.Z - a.Z) * z) / d));
        x = p.X - a.X - t * x; z = p.Z - a.Z - t * z;
        return (float)Math.Sqrt(x * x + z * z);
    }
    private static float Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
    private static bool Intersects(Point a, Point b, Point c, Point d)
    {
        float ac = Cross(a, b, c), ad = Cross(a, b, d), ca = Cross(c, d, a), cb = Cross(c, d, b);
        if ((ac > 0 && ad < 0 || ac < 0 && ad > 0) && (ca > 0 && cb < 0 || ca < 0 && cb > 0)) return true;
        return Math.Abs(ac) < 0.00001f && DistanceToSegment(c, a, b) < 0.00001f ||
            Math.Abs(ad) < 0.00001f && DistanceToSegment(d, a, b) < 0.00001f ||
            Math.Abs(ca) < 0.00001f && DistanceToSegment(a, c, d) < 0.00001f ||
            Math.Abs(cb) < 0.00001f && DistanceToSegment(b, c, d) < 0.00001f;
    }
    public static bool IsSimple(IReadOnlyList<Point> points)
    {
        if (points.Count < 3 || Area(points) < 0.001f) return false;
        for (int i = 0; i < points.Count; i++)
        for (int j = i + 1; j < points.Count; j++)
        {
            if (j == i + 1 || i == 0 && j == points.Count - 1) continue;
            if (Intersects(points[i], points[(i + 1) % points.Count], points[j], points[(j + 1) % points.Count])) return false;
        }
        return true;
    }
    public static bool Contains(IReadOnlyList<Point> points, Point p, float margin = 0f)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            var a = points[i]; var b = points[j];
            if (DistanceToSegment(p, a, b) < Math.Max(margin, 0.00001f)) return false;
            if ((a.Z > p.Z) != (b.Z > p.Z) && p.X < (b.X - a.X) * (p.Z - a.Z) / (b.Z - a.Z) + a.X) inside = !inside;
        }
        return inside;
    }
    public static void Bounds(IReadOnlyList<Point> points, out Point min, out Point max)
    {
        min = max = points[0];
        foreach (var p in points)
        { min.X = Math.Min(min.X, p.X); min.Z = Math.Min(min.Z, p.Z); max.X = Math.Max(max.X, p.X); max.Z = Math.Max(max.Z, p.Z); }
    }
    public static bool TryPoint(IReadOnlyList<Point> points, float u, float v, float margin, out Point result)
    {
        result = default;
        if (points.Count < 3) return false;
        Bounds(points, out var min, out var max);
        result = new Point(min.X + (max.X - min.X) * u, min.Z + (max.Z - min.Z) * v);
        if (Contains(points, result, margin)) return true;
        // Deterministic fallback remains inside concave polygons and keeps the margin.
        for (int x = 1; x < 32; x++) for (int z = 1; z < 32; z++)
        {
            result = new Point(min.X + (max.X - min.X) * x / 32f, min.Z + (max.Z - min.Z) * z / 32f);
            if (Contains(points, result, margin)) return true;
        }
        return false;
    }
}
