using System;
using UnityEngine;

[Serializable]
public struct FootDetection
{
    public Rect viewportRect;
    public float confidence;

    public Vector2 ContactPoint => new Vector2(viewportRect.center.x, viewportRect.yMin);
}

public enum FootSide
{
    Left,
    Right
}
