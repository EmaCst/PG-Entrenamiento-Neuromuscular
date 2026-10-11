using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Praxen's landscape menu. Graphics are cached and use no scene rebuild.</summary>
public class PhoneExerciseMenu : MonoBehaviour
{
    private enum MenuPage { Home, Individual, Circuit }
    private enum ExerciseChoice { Hands, Feet, Running }
    private static readonly string[] DifficultyNames = { "Inicial", "Básico", "Intermedio", "Avanzado", "Intensivo" };
    private static readonly string[] ExerciseNames = { "Manos", "Pies", "Correr" };
    private const float Width = 1600f;
    private const float Height = 720f;
    private static readonly Color Navy = new Color(0.012f, 0.10f, 0.23f);
    private static readonly Color Cyan = new Color(0.10f, 0.84f, 0.95f);
    private static readonly Color Orange = new Color(1f, 0.36f, 0.13f);
    private static readonly Color Muted = new Color(0.73f, 0.83f, 0.94f);

    private MenuPage page;
    private ExerciseChoice exerciseChoice;
    private int difficulty = 2;
    private string individualSeconds = "60";
    private string repetitions = "2";
    private string handsSeconds = "60";
    private string feetSeconds = "60";
    private string runningSeconds = "60";
    private string restSeconds = "20";
    private string validationMessage;
    private Texture2D logo, background, panelTexture, buttonTexture, hoverTexture, cyanTexture, orangeTexture;
    private readonly List<Texture2D> ownedTextures = new List<Texture2D>();
    private GUIStyle titleStyle, brandStyle, subtitleStyle, smallStyle, labelStyle, buttonStyle, selectedStyle;
    private GUIStyle orangeStyle, tileStyle, inputStyle, panelStyle;

    private void Awake()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        logo = Resources.Load<Texture2D>("PraxenMenuLogo");
        if (logo == null) Debug.LogWarning("Praxen menu logo missing: Resources/PraxenMenuLogo.png");
    }

    private void Start()
    {
        if (Camera.main != null) return;
        GameObject cameraObject = new GameObject("Menu Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
    }

    private void OnGUI()
    {
        EnsureStyles();
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        Color previousBackground = GUI.backgroundColor;
        Color previousContent = GUI.contentColor;
        GUI.color = GUI.backgroundColor = GUI.contentColor = Color.white;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), background);
        if (!Application.isEditor && Screen.width < Screen.height)
        {
            GUI.Label(new Rect(20f, 20f, Screen.width - 40f, 80f), "Preparando la pantalla horizontal...", labelStyle);
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
            return;
        }

        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(safe.width / Width, safe.height / Height);
        if (scale <= 0f)
        {
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
            return;
        }
        float x = safe.x + (safe.width - Width * scale) * 0.5f;
        float y = Screen.height - safe.yMax + (safe.height - Height * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        try
        {
            DrawHeader();
            switch (page)
            {
                case MenuPage.Home: DrawHome(); break;
                case MenuPage.Individual: DrawIndividual(); break;
                case MenuPage.Circuit: DrawCircuit(); break;
            }
            if (!string.IsNullOrEmpty(validationMessage))
                GUI.Label(new Rect(65f, 651f, 1470f, 52f), validationMessage, smallStyle);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
        }
    }

    private void DrawHeader()
    {
        float left = page == MenuPage.Home ? 58f : 630f;
        if (logo != null) GUI.DrawTexture(new Rect(left, 8f, 135f, 112f), logo, ScaleMode.ScaleToFit, true);
        GUI.Label(new Rect(left + 130f, 20f, 310f, 60f), "Praxen", brandStyle);
        GUI.Label(new Rect(left + 133f, 79f, 430f, 30f), "Entrenamiento neuromuscular", smallStyle);
        if (page != MenuPage.Home && GUI.Button(new Rect(62f, 32f, 160f, 58f), "<  Volver", buttonStyle))
        {
            GUI.FocusControl(null);
            validationMessage = null;
            page = MenuPage.Home;
        }
    }

    private void DrawHome()
    {
        GUI.Label(new Rect(64f, 138f, 1450f, 65f), "¿Qué quieres realizar?", titleStyle);
        DrawHomeCard(new Rect(64f, 226f, 722f, 392f), false);
        DrawHomeCard(new Rect(814f, 226f, 722f, 392f), true);
    }

    private void DrawHomeCard(Rect rect, bool circuit)
    {
        GUI.Box(rect, GUIContent.none, panelStyle);
        Color accent = circuit ? Orange : Cyan;
        DrawExerciseIcon(new Rect(rect.x + 34f, rect.y + 55f, 196f, 190f), circuit ? ExerciseChoice.Running : ExerciseChoice.Hands, accent);
        GUI.Label(new Rect(rect.x + 246f, rect.y + 67f, 440f, 65f), circuit ? "Circuito" : "Un ejercicio", titleStyle);
        GUI.Label(new Rect(rect.x + 246f, rect.y + 142f, 435f, 96f),
            circuit ? "Combina manos, pies y correr" : "Elige manos, pies o correr", subtitleStyle);
        if (GUI.Button(new Rect(rect.x + 246f, rect.y + 260f, 430f, 82f),
            circuit ? "Configurar circuito  >" : "Elegir ejercicio  >", circuit ? orangeStyle : selectedStyle))
        {
            page = circuit ? MenuPage.Circuit : MenuPage.Individual;
            validationMessage = null;
        }
    }

    private void DrawIndividual()
    {
        GUI.Label(new Rect(64f, 141f, 1100f, 65f), "Configura tu ejercicio", titleStyle);
        GUI.Label(new Rect(66f, 227f, 650f, 40f), "Tipo de ejercicio", labelStyle);
        for (int i = 0; i < ExerciseNames.Length; i++)
        {
            Rect tile = new Rect(65f + i * 229f, 281f, 211f, 222f);
            bool selected = (int)exerciseChoice == i;
            if (GUI.Button(tile, GUIContent.none, selected ? selectedStyle : tileStyle)) exerciseChoice = (ExerciseChoice)i;
            DrawExerciseIcon(new Rect(tile.x + 66f, tile.y + 25f, 80f, 107f), (ExerciseChoice)i, selected ? Navy : Color.white);
            GUI.Label(new Rect(tile.x, tile.y + 151f, tile.width, 44f), ExerciseNames[i], selected ? DarkCenteredLabel() : centeredLabel);
        }
        GUI.Label(new Rect(808f, 225f, 690f, 42f), "Dificultad inicial", labelStyle);
        GUI.Label(new Rect(808f, 266f, 700f, 32f), "Se adapta según tus resultados", smallStyle);
        DrawDifficulty(808f, 316f, 728f);
        GUI.Label(new Rect(808f, 409f, 170f, 52f), "Duración", labelStyle);
        individualSeconds = DrawStepper(new Rect(983f, 407f, 364f, 64f), individualSeconds, 5f, 5f);
        GUI.Label(new Rect(1367f, 414f, 170f, 45f), "segundos", smallStyle);
        if (GUI.Button(new Rect(65f, 552f, 1471f, 82f), "Iniciar ejercicio  >", selectedStyle)) StartIndividual();
        if (string.IsNullOrEmpty(validationMessage))
            GUI.Label(new Rect(65f, 645f, 1470f, 38f), "Tendrás 5 segundos para prepararte.", smallStyle);
    }

    private void DrawCircuit()
    {
        GUI.Label(new Rect(65f, 129f, 1000f, 65f), "Configura tu circuito", titleStyle);
        GUI.Label(new Rect(65f, 210f, 300f, 42f), "Dificultad inicial", labelStyle);
        DrawDifficulty(400f, 202f, 1135f);
        GUI.Label(new Rect(65f, 309f, 320f, 40f), "Repeticiones", labelStyle);
        repetitions = DrawStepper(new Rect(386f, 297f, 330f, 62f), repetitions, 1f, 1f);
        GUI.Label(new Rect(810f, 309f, 365f, 40f), "Descanso (segundos)", labelStyle);
        restSeconds = DrawStepper(new Rect(1205f, 297f, 330f, 62f), restSeconds, 0f, 5f);
        handsSeconds = DrawCircuitDuration(65f, "Manos", handsSeconds);
        feetSeconds = DrawCircuitDuration(565f, "Pies", feetSeconds);
        runningSeconds = DrawCircuitDuration(1065f, "Correr", runningSeconds);
        if (GUI.Button(new Rect(65f, 552f, 1470f, 82f), "Iniciar circuito  >", orangeStyle)) StartCircuit();
        if (string.IsNullOrEmpty(validationMessage))
            GUI.Label(new Rect(65f, 645f, 1470f, 38f), "5 segundos de preparación antes de cada ejercicio.", smallStyle);
    }

    private string DrawCircuitDuration(float x, string exercise, string value)
    {
        GUI.Label(new Rect(x, 404f, 450f, 40f), exercise + " (segundos)", labelStyle);
        return DrawStepper(new Rect(x, 452f, 470f, 62f), value, 5f, 5f);
    }

    private void DrawDifficulty(float x, float y, float width)
    {
        float gap = 12f;
        float itemWidth = (width - gap * 4f) / 5f;
        for (int i = 0; i < DifficultyNames.Length; i++)
            if (GUI.Button(new Rect(x + i * (itemWidth + gap), y, itemWidth, 62f), DifficultyNames[i],
                difficulty == i ? selectedStyle : buttonStyle)) difficulty = i;
    }

    private string DrawStepper(Rect rect, string value, float minimum, float step)
    {
        const float buttonWidth = 64f;
        if (GUI.Button(new Rect(rect.x, rect.y, buttonWidth, rect.height), "−", buttonStyle))
            value = ChangeValue(value, -step, minimum);
        value = GUI.TextField(new Rect(rect.x + buttonWidth + 8f, rect.y, rect.width - 2f * buttonWidth - 16f, rect.height), value, 8, inputStyle);
        if (GUI.Button(new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height), "+", buttonStyle))
            value = ChangeValue(value, step, minimum);
        return value;
    }

    private static string ChangeValue(string text, float step, float minimum)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value)) value = minimum;
        return Mathf.Max(minimum, value + step).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void StartIndividual()
    {
        if (!TrySeconds(individualSeconds, out float duration))
        {
            validationMessage = "La duración debe ser un número de al menos 5 segundos.";
            return;
        }
        PhoneTrainingOptions.ConfigureIndividual(SceneForChoice(exerciseChoice), difficulty, duration);
        SceneManager.LoadScene(SceneForChoice(exerciseChoice), LoadSceneMode.Single);
    }

    private void StartCircuit()
    {
        if (!int.TryParse(repetitions, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cycles) || cycles < 1 ||
            !TrySeconds(handsSeconds, out float hands) || !TrySeconds(feetSeconds, out float feet) ||
            !TrySeconds(runningSeconds, out float running) || !TrySeconds(restSeconds, out float rest, true))
        {
            validationMessage = "Revisa los valores: repeticiones desde 1, duraciones desde 5 s y descanso desde 0.";
            return;
        }
        PhoneTrainingOptions.ConfigureCircuit(difficulty, cycles, hands, feet, running, rest);
        SceneManager.LoadScene("SesionCombinada", LoadSceneMode.Single);
    }

    private static string SceneForChoice(ExerciseChoice choice)
    {
        return choice == ExerciseChoice.Hands ? "TelefonoManos" : choice == ExerciseChoice.Feet ? "TelefonoPies" : "TelefonoCorrer";
    }

    private static bool TrySeconds(string text, out float value, bool allowZero = false)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= (allowZero ? 0f : 5f);
    }

    private GUIStyle darkCenteredLabel, centeredLabel;
    private GUIStyle DarkCenteredLabel()
    {
        if (darkCenteredLabel == null)
        {
            darkCenteredLabel = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
            darkCenteredLabel.normal.textColor = Navy;
        }
        return darkCenteredLabel;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        background = MakeBackground();
        panelTexture = MakeRounded(new Color(0.027f, 0.18f, 0.36f));
        buttonTexture = MakeRounded(new Color(0.055f, 0.24f, 0.43f));
        hoverTexture = MakeRounded(new Color(0.08f, 0.34f, 0.53f));
        cyanTexture = MakeRounded(Cyan);
        orangeTexture = MakeRounded(Orange);
        titleStyle = TextStyle(46, Color.white, FontStyle.Bold);
        brandStyle = TextStyle(58, Color.white, FontStyle.BoldAndItalic);
        subtitleStyle = TextStyle(30, Muted);
        smallStyle = TextStyle(25, Muted);
        labelStyle = TextStyle(30, Color.white, FontStyle.Bold);
        centeredLabel = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
        buttonStyle = ButtonStyle(buttonTexture, Color.white, 25);
        selectedStyle = ButtonStyle(cyanTexture, Navy, 27);
        orangeStyle = ButtonStyle(orangeTexture, Navy, 27);
        tileStyle = ButtonStyle(panelTexture, Color.white, 27);
        panelStyle = ButtonStyle(panelTexture, Color.white, 25);
        inputStyle = ButtonStyle(buttonTexture, Color.white, 34);
        inputStyle.fontStyle = FontStyle.Bold;
    }

    private GUIStyle TextStyle(int size, Color color, FontStyle fontStyle = FontStyle.Normal)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, wordWrap = true };
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle ButtonStyle(Texture2D texture, Color textColor, int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = size, alignment = TextAnchor.MiddleCenter, wordWrap = true,
            border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(8, 8, 6, 6)
        };
        style.normal.background = style.active.background = style.focused.background = texture;
        style.hover.background = texture == buttonTexture || texture == panelTexture ? hoverTexture : texture;
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = textColor;
        return style;
    }

    private Texture2D MakeRounded(Color color)
    {
        const int size = 64;
        const float radius = 12f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius));
                float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius));
                float distance = new Vector2(Mathf.Max(0f, dx), Mathf.Max(0f, dy)).magnitude;
                Color pixel = color;
                pixel.a = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = pixel;
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        ownedTextures.Add(texture);
        return texture;
    }

    private Texture2D MakeBackground()
    {
        Texture2D texture = new Texture2D(1, 64, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++) texture.SetPixel(0, y, Color.Lerp(Navy, new Color(0.015f, 0.18f, 0.37f), y / 63f));
        texture.Apply(false, true);
        ownedTextures.Add(texture);
        return texture;
    }

    private static void DrawExerciseIcon(Rect area, ExerciseChoice exercise, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        float unit = Mathf.Min(area.width, area.height) / 100f;
        Vector2 origin = new Vector2(area.x + (area.width - 100f * unit) * 0.5f, area.y + (area.height - 100f * unit) * 0.5f);
        if (exercise == ExerciseChoice.Hands)
        {
            GUI.DrawTexture(new Rect(origin.x + 27f * unit, origin.y + 45f * unit, 50f * unit, 45f * unit), Texture2D.whiteTexture);
            for (int i = 0; i < 4; i++)
                GUI.DrawTexture(new Rect(origin.x + (28f + i * 13f) * unit, origin.y + (i == 1 ? 10f : 18f) * unit, 10f * unit, 42f * unit), Texture2D.whiteTexture);
            IconLine(origin + new Vector2(29f, 67f) * unit, origin + new Vector2(13f, 43f) * unit, 10f * unit);
        }
        else if (exercise == ExerciseChoice.Feet)
        {
            IconLine(origin + new Vector2(17f, 76f) * unit, origin + new Vector2(87f, 76f) * unit, 9f * unit);
            IconLine(origin + new Vector2(20f, 66f) * unit, origin + new Vector2(66f, 29f) * unit, 18f * unit);
            IconLine(origin + new Vector2(66f, 29f) * unit, origin + new Vector2(84f, 60f) * unit, 18f * unit);
            IconLine(origin + new Vector2(84f, 60f) * unit, origin + new Vector2(20f, 66f) * unit, 18f * unit);
        }
        else
        {
            GUI.DrawTexture(new Rect(origin.x + 58f * unit, origin.y + 7f * unit, 15f * unit, 15f * unit), Texture2D.whiteTexture);
            IconLine(origin + new Vector2(59f, 34f) * unit, origin + new Vector2(44f, 58f) * unit, 10f * unit);
            IconLine(origin + new Vector2(59f, 34f) * unit, origin + new Vector2(78f, 47f) * unit, 8f * unit);
            IconLine(origin + new Vector2(58f, 34f) * unit, origin + new Vector2(31f, 35f) * unit, 8f * unit);
            IconLine(origin + new Vector2(44f, 58f) * unit, origin + new Vector2(20f, 86f) * unit, 9f * unit);
            IconLine(origin + new Vector2(44f, 58f) * unit, origin + new Vector2(68f, 69f) * unit, 9f * unit);
            IconLine(origin + new Vector2(68f, 69f) * unit, origin + new Vector2(58f, 94f) * unit, 9f * unit);
        }
        GUI.color = previous;
    }

    private static void IconLine(Vector2 start, Vector2 end, float thickness)
    {
        Matrix4x4 previous = GUI.matrix;
        Vector2 delta = end - start;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }

    private void OnDestroy()
    {
        foreach (Texture2D texture in ownedTextures) if (texture != null) Destroy(texture);
    }
}
