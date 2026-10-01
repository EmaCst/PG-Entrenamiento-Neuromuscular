using UnityEngine;

[DisallowMultipleComponent]
public class PhoneStereoOverlay : MonoBehaviour
{
    [SerializeField] private bool showGuide = true;
    [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField, Range(1f, 8f)] private float lineThickness = 2f;
    [SerializeField, Range(4f, 40f)] private float crosshairSize = 14f;

    private Texture2D pixel;

    public bool ShowGuide
    {
        get => showGuide;
        set => showGuide = value;
    }

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.name = "PhoneStereoGuidePixel";
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void OnGUI()
    {
        if (!showGuide || pixel == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = guideColor;

        float centerX = Screen.width * 0.5f;
        DrawRect(new Rect(centerX - lineThickness * 0.5f, 0f, lineThickness, Screen.height));

        DrawCrosshair(Screen.width * 0.25f, Screen.height * 0.5f);
        DrawCrosshair(Screen.width * 0.75f, Screen.height * 0.5f);

        GUI.color = previousColor;
    }

    private void DrawCrosshair(float x, float y)
    {
        DrawRect(new Rect(x - crosshairSize, y - lineThickness * 0.5f, crosshairSize * 2f, lineThickness));
        DrawRect(new Rect(x - lineThickness * 0.5f, y - crosshairSize, lineThickness, crosshairSize * 2f));
    }

    private void DrawRect(Rect rect)
    {
        GUI.DrawTexture(rect, pixel);
    }

    private void OnDestroy()
    {
        if (pixel != null)
        {
            Destroy(pixel);
        }
    }
}
