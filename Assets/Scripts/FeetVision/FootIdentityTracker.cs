using UnityEngine;

/// <summary>Two short-lived image tracks. Ambiguous/missing identities are not guessed.</summary>
public sealed class FootIdentityTracker
{
    private Rect left, right;
    private float leftAt = float.NegativeInfinity, rightAt = float.NegativeInfinity;
    public void Assign(Rect first, Rect second, int count, float time, out int leftIndex, out int rightIndex)
    {
        leftIndex = rightIndex = -1;
        if (count == 0) return;
        bool hasLeft = time - leftAt <= 0.6f, hasRight = time - rightAt <= 0.6f;
        if (!hasLeft || !hasRight)
        {
            // Require both feet to establish anatomical-side convention again.
            if (count < 2 || Mathf.Abs(first.center.x - second.center.x) < 0.03f) return;
            leftIndex = first.center.x < second.center.x ? 0 : 1;
            rightIndex = 1 - leftIndex;
        }
        else if (count >= 2)
        {
            float straight = Cost(left, first) + Cost(right, second);
            float crossed = Cost(left, second) + Cost(right, first);
            if (Mathf.Abs(straight - crossed) < 0.0004f) return;
            leftIndex = straight < crossed ? 0 : 1;
            rightIndex = 1 - leftIndex;
            if (!Near(left, leftIndex == 0 ? first : second) || !Near(right, rightIndex == 0 ? first : second))
            { leftIndex = rightIndex = -1; return; }
        }
        else
        {
            float l = Cost(left, first), r = Cost(right, first);
            if (Mathf.Abs(l - r) < 0.0004f) return;
            if (l < r && Near(left, first)) leftIndex = 0;
            else if (r < l && Near(right, first)) rightIndex = 0;
        }
        if (leftIndex >= 0) { left = leftIndex == 0 ? first : second; leftAt = time; }
        if (rightIndex >= 0) { right = rightIndex == 0 ? first : second; rightAt = time; }
    }
    private static float Cost(Rect a, Rect b) => (a.center - b.center).sqrMagnitude;
    private static bool Near(Rect a, Rect b) => Vector2.Distance(a.center, b.center) <= Mathf.Max(0.10f, a.size.magnitude * 0.8f);
}
