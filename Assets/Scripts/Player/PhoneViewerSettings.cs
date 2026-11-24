using UnityEngine;

/// <summary>Shared presentation settings; the exercise and detector coordinates stay unchanged.</summary>
public static class PhoneViewerSettings
{
    private const string Prefix = "Praxen.Viewer.v1.";
    public static bool Enabled { get; set; } = PlayerPrefs.GetInt(Prefix + "enabled", 1) != 0;
    public static float ImageScale { get; set; } = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "scale", 0.85f), 0.65f, 1f);
    public static float LensSpacing { get; set; } = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "spacing", 0f), -0.08f, 0.08f);
    public static float VerticalOffset { get; set; } = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "vertical", 0f), -0.1f, 0.1f);
    public static float Curvature { get; set; } = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "curvature", 0.10f), 0f, 0.35f);

    public static void Save()
    {
        PlayerPrefs.SetInt(Prefix + "enabled", Enabled ? 1 : 0);
        PlayerPrefs.SetFloat(Prefix + "scale", ImageScale);
        PlayerPrefs.SetFloat(Prefix + "spacing", LensSpacing);
        PlayerPrefs.SetFloat(Prefix + "vertical", VerticalOffset);
        PlayerPrefs.SetFloat(Prefix + "curvature", Curvature);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        Enabled = true;
        ImageScale = 0.85f;
        LensSpacing = VerticalOffset = 0f;
        Curvature = 0.10f;
        Save();
    }

    // This inverse display mapping is also used by the final shader. Input is bottom-left UV.
    public static bool TryMapToSource(Vector2 displayUv, out Vector2 sourceUv)
    {
        int eye = displayUv.x < 0.5f ? 0 : 1;
        Vector2 p = new Vector2((displayUv.x * 2f - eye) * 2f - 1f, displayUv.y * 2f - 1f);
        p.x -= eye == 0 ? -LensSpacing : LensSpacing;
        p.y -= VerticalOffset;
        Vector2 q = p / ImageScale;
        float radiusSquared = q.sqrMagnitude;
        q *= 1f + Curvature * radiusSquared;
        sourceUv = new Vector2((eye + (q.x + 1f) * 0.5f) * 0.5f, (q.y + 1f) * 0.5f);
        return displayUv.x >= 0f && displayUv.x <= 1f && displayUv.y >= 0f && displayUv.y <= 1f &&
            Mathf.Abs(q.x) <= 1f && Mathf.Abs(q.y) <= 1f;
    }
}
