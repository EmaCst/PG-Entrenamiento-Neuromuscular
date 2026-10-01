using UnityEngine;
using UnityEngine.SceneManagement;

public class PhoneExerciseMenu : MonoBehaviour
{
    private void Awake()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Menu Camera");
        cameraObject.tag = "MainCamera";
        Camera menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
    }

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
