using UnityEngine;
using TMPro;
using System.Text;

[DisallowMultipleComponent]
public class PhoneStereoOverlay : MonoBehaviour
{
    [SerializeField] private bool showGuide = true;
    [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField, Range(1f, 8f)] private float lineThickness = 2f;
    [SerializeField, Range(4f, 40f)] private float crosshairSize = 14f;
    [SerializeField] private bool showExerciseHud = true;

    private Texture2D pixel;
    private TMP_Text[] hudTexts;
    private GUIStyle hudStyle;

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

        var collectedTexts = new System.Collections.Generic.List<TMP_Text>();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text.enabled && !collectedTexts.Contains(text)) collectedTexts.Add(text);
            }
        }

        hudTexts = collectedTexts.ToArray();

        // Los componentes siguen recibiendo actualizaciones de los managers;
        // ocultamos el render centrado para dibujar una copia dentro de cada ojo.
        foreach (var text in hudTexts) text.enabled = false;
    }

    private void OnGUI()
    {
        if (pixel == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        if (showGuide)
        {
            GUI.color = guideColor;
            float centerX = Screen.width * 0.5f;
            DrawRect(new Rect(centerX - lineThickness * 0.5f, 0f, lineThickness, Screen.height));
            DrawCrosshair(Screen.width * 0.25f, Screen.height * 0.5f);
            DrawCrosshair(Screen.width * 0.75f, Screen.height * 0.5f);
        }

        if (showExerciseHud)
        {
            DrawExerciseHud(0);
            DrawExerciseHud(1);
        }

        GUI.color = previousColor;
    }

    private void DrawExerciseHud(int eye)
    {
        if (hudTexts == null || hudTexts.Length == 0)
        {
            return;
        }

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 13, 22),
                normal = { textColor = Color.white }
            };
        }

        var lines = new StringBuilder();
        foreach (var text in hudTexts) AppendHudLine(lines, text);

        float halfWidth = Screen.width * 0.5f;
        float left = eye * halfWidth + halfWidth * 0.04f;
        var rect = new Rect(left, Screen.height * 0.17f, halfWidth * 0.92f, Screen.height * 0.15f);
        GUI.color = new Color(0f, 0f, 0f, 0.48f);
        GUI.DrawTexture(rect, pixel);
        GUI.color = Color.white;
        GUI.Label(rect, lines.ToString(), hudStyle);
    }

    private static void AppendHudLine(StringBuilder builder, TMP_Text source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.text)) return;
        if (builder.Length > 0) builder.Append('\n');
        builder.Append(source.text.Replace("\n", " "));
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
