using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhoneExerciseMenu : MonoBehaviour
{
    private enum MenuPage { Home, Individual, Circuit }
    private enum ExerciseChoice { Hands, Feet, Running }

    private static readonly string[] DifficultyNames =
    {
        "Inicial", "Basico", "Intermedio", "Avanzado", "Intensivo"
    };

    private MenuPage page;
    private ExerciseChoice exerciseChoice;
    private int difficulty = 2;
    private string individualSeconds = "60";
    private string repetitions = "2";
    private string handsSeconds = "60";
    private string feetSeconds = "60";
    private string runningSeconds = "60";
    private string restSeconds = "20";
    private Vector2 scrollPosition;
    private string validationMessage;

    private void Start()
    {
        if (Camera.main != null) return;

        GameObject cameraObject = new GameObject("Menu Camera");
        cameraObject.tag = "MainCamera";
        Camera menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
    }

    private void OnGUI()
    {
        float width = Mathf.Min(620f, Screen.width - 32f);
        float height = Screen.height - 32f;
        float left = (Screen.width - width) * 0.5f;
        GUILayout.BeginArea(new Rect(left, 16f, width, height), GUI.skin.box);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("ENTRENAMIENTO NEUROMUSCULAR");

        switch (page)
        {
            case MenuPage.Home:
                DrawHome();
                break;
            case MenuPage.Individual:
                DrawIndividual();
                break;
            case MenuPage.Circuit:
                DrawCircuit();
                break;
        }

        if (!string.IsNullOrEmpty(validationMessage))
            GUILayout.Label(validationMessage);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawHome()
    {
        GUILayout.Space(18f);
        GUILayout.Label("¿Qué quieres realizar?");
        if (GUILayout.Button("Un ejercicio", GUILayout.Height(58f)))
        {
            page = MenuPage.Individual;
            validationMessage = null;
        }
        if (GUILayout.Button("Circuito: manos, pies y correr", GUILayout.Height(58f)))
        {
            page = MenuPage.Circuit;
            validationMessage = null;
        }
    }

    private void DrawIndividual()
    {
        GUILayout.Space(10f);
        GUILayout.Label("Selecciona el ejercicio");
        exerciseChoice = (ExerciseChoice)GUILayout.SelectionGrid((int)exerciseChoice,
            new[] { "Manos", "Pies", "Correr" }, 3, GUILayout.Height(48f));

        DrawDifficultySelector();
        individualSeconds = DrawField("Duración (segundos)", individualSeconds);

        if (GUILayout.Button("Iniciar ejercicio", GUILayout.Height(52f)))
        {
            if (!TrySeconds(individualSeconds, out float duration))
            {
                validationMessage = "La duración debe ser un número de al menos 5 segundos.";
                return;
            }

            string scene = SceneForChoice(exerciseChoice);
            PhoneTrainingOptions.ConfigureIndividual(scene, difficulty, duration);
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        if (GUILayout.Button("Volver", GUILayout.Height(42f)))
        {
            page = MenuPage.Home;
            validationMessage = null;
        }
    }

    private void DrawCircuit()
    {
        GUILayout.Space(10f);
        GUILayout.Label("Configura el circuito");
        DrawDifficultySelector();
        repetitions = DrawField("Repeticiones del circuito", repetitions);
        handsSeconds = DrawField("Manos (segundos)", handsSeconds);
        feetSeconds = DrawField("Pies (segundos)", feetSeconds);
        runningSeconds = DrawField("Correr (segundos)", runningSeconds);
        restSeconds = DrawField("Descanso entre ejercicios (s)", restSeconds);

        if (GUILayout.Button("Iniciar circuito", GUILayout.Height(52f)))
        {
            if (!TryInt(repetitions, 1, out int cycles) ||
                !TrySeconds(handsSeconds, out float hands) ||
                !TrySeconds(feetSeconds, out float feet) ||
                !TrySeconds(runningSeconds, out float running) ||
                !TrySeconds(restSeconds, out float rest, allowZero: true))
            {
                validationMessage = "Revisa los valores: repeticiones desde 1, duraciones de 5 s o más y descanso desde 0.";
                return;
            }

            PhoneTrainingOptions.ConfigureCircuit(difficulty, cycles, hands, feet, running, rest);
            SceneManager.LoadScene("SesionCombinada", LoadSceneMode.Single);
        }

        if (GUILayout.Button("Volver", GUILayout.Height(42f)))
        {
            page = MenuPage.Home;
            validationMessage = null;
        }
    }

    private void DrawDifficultySelector()
    {
        GUILayout.Label("Dificultad inicial (se adapta según tus resultados)");
        difficulty = GUILayout.SelectionGrid(difficulty, DifficultyNames, 5, GUILayout.Height(48f));
    }

    private static string DrawField(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(260f));
        value = GUILayout.TextField(value, GUILayout.MinWidth(120f));
        GUILayout.EndHorizontal();
        return value;
    }

    private static string SceneForChoice(ExerciseChoice choice)
    {
        switch (choice)
        {
            case ExerciseChoice.Hands: return "TelefonoManos";
            case ExerciseChoice.Feet: return "TelefonoPies";
            default: return "TelefonoCorrer";
        }
    }

    private static bool TryInt(string text, int minimum, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= minimum;
    }

    private static bool TrySeconds(string text, out float value, bool allowZero = false)
    {
        bool parsed = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return parsed && value >= (allowZero ? 0f : 5f);
    }
}
