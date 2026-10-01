using UnityEngine;
using UnityEngine.SceneManagement;

public class PhoneExerciseMenu : MonoBehaviour
{
    private void OnGUI()
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        float left = (Screen.width - width) * 0.5f;
        GUILayout.BeginArea(new Rect(left, 30f, width, Screen.height - 60f), GUI.skin.box);
        GUILayout.Label("Entrenamiento neuromuscular");
        DrawSceneButton("Ejercicio de manos", "TelefonoManos");
        DrawSceneButton("Ejercicio de pies", "TelefonoPies");
        DrawSceneButton("Ejercicio de correr", "TelefonoCorrer");
        DrawSceneButton("Sesion combinada configurable", "SesionCombinada");
        GUILayout.EndArea();
    }

    private static void DrawSceneButton(string label, string scene)
    {
        if (GUILayout.Button(label, GUILayout.Height(55f)))
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
