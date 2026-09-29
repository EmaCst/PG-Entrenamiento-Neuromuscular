using UnityEngine;

public class FootTargetZone : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);

    public int Index { get; private set; }
    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void Configure(int index)
    {
        Index = index;
        SetState(false, Color.white);
    }

    public void SetState(bool active, Color activeColor)
    {
        IsActive = active;
        if (targetRenderer != null)
        {
            targetRenderer.material.color = active ? activeColor : inactiveColor;
        }
    }

    public bool ContainsViewportPoint(Camera camera, Vector2 point)
    {
        if (camera == null || targetRenderer == null) return false;
        Bounds bounds = targetRenderer.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z)
        };

        float xMin = 1f, yMin = 1f, xMax = 0f, yMax = 0f;
        foreach (Vector3 corner in corners)
        {
            Vector3 viewport = camera.WorldToViewportPoint(corner);
            if (viewport.z <= 0f) continue;
            xMin = Mathf.Min(xMin, viewport.x);
            yMin = Mathf.Min(yMin, viewport.y);
            xMax = Mathf.Max(xMax, viewport.x);
            yMax = Mathf.Max(yMax, viewport.y);
        }

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin).Contains(point);
    }
}
