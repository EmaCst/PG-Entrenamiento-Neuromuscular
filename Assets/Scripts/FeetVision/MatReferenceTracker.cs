using UnityEngine;

/// <summary>Tracks observations from capture timestamps, never render frames.</summary>
public sealed class MatReferenceTracker
{
    public Rect Rect { get; private set; }
    public float LastSeen { get; private set; } = float.NegativeInfinity;
    public int Generation { get; private set; }
    private int stable;
    public bool IsStable(float capturedAt) => stable >= 3 && capturedAt - LastSeen <= 0.35f;

    public void Observe(bool visible, Rect rect, float capturedAt, bool allowOcclusion = false)
    {
        if (!visible) { if (!allowOcclusion || capturedAt - LastSeen > 0.35f) stable = 0; return; }
        float scale = Mathf.Max(0.025f, Rect.size.magnitude);
        bool consistent = capturedAt - LastSeen <= 0.35f &&
            Vector2.Distance(rect.center, Rect.center) < scale * 0.55f &&
            rect.width / Mathf.Max(0.001f, Rect.width) > 0.60f &&
            rect.width / Mathf.Max(0.001f, Rect.width) < 1.65f &&
            rect.height / Mathf.Max(0.001f, Rect.height) > 0.60f &&
            rect.height / Mathf.Max(0.001f, Rect.height) < 1.65f;
        if (!consistent) { stable = 0; Generation++; }
        stable++;
        // Only damp sub-pixel jitter. Large movements use the new measurement immediately.
        if (consistent && Vector2.Distance(rect.center, Rect.center) < 0.006f)
            rect = new Rect(Vector2.Lerp(Rect.position, rect.position, 0.7f), Vector2.Lerp(Rect.size, rect.size, 0.7f));
        Rect = rect;
        LastSeen = capturedAt;
    }
}
