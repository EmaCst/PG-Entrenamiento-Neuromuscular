#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class IntegratedPhoneSceneBuilder
{
    private const string ExerciseScenePath = "Assets/Scenes/Tests.unity";
    private const string MediaPipeScenePath =
        "Assets/MediaPipeUnity/Samples/Scenes/Hand Landmark Detection/Hand Landmark Detection.unity";
    private const string OutputScenePath = "Assets/Scenes/TelefonoMediaPipe.unity";

    [MenuItem("PG RA/Crear escena integrada MediaPipe + telefono")]
    public static void BuildIntegratedScene()
    {
        if (!System.IO.File.Exists(ExerciseScenePath) ||
            !System.IO.File.Exists(MediaPipeScenePath))
        {
            EditorUtility.DisplayDialog(
                "Escena integrada",
                "No se encontro Tests.unity o la escena de Hand Landmark Detection.",
                "Aceptar"
            );
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (System.IO.File.Exists(OutputScenePath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Recrear escena integrada",
                "TelefonoMediaPipe.unity ya existe. Se reemplazara con una version nueva.",
                "Reemplazar",
                "Cancelar"
            );

            if (!overwrite)
            {
                return;
            }

            AssetDatabase.DeleteAsset(OutputScenePath);
        }

        if (!AssetDatabase.CopyAsset(MediaPipeScenePath, OutputScenePath))
        {
            EditorUtility.DisplayDialog(
                "Escena integrada",
                "No fue posible copiar la escena base de MediaPipe.",
                "Aceptar"
            );
            return;
        }

        AssetDatabase.Refresh();

        Scene integratedScene = EditorSceneManager.OpenScene(
            OutputScenePath,
            OpenSceneMode.Single
        );

        RemoveDuplicateRoots(integratedScene);
        HideMediaPipeSampleCanvas(integratedScene);

        Scene exerciseScene = EditorSceneManager.OpenScene(
            ExerciseScenePath,
            OpenSceneMode.Additive
        );

        EditorSceneManager.MergeScenes(exerciseScene, integratedScene);
        SceneManager.SetActiveScene(integratedScene);

        Camera mainCamera = FindMainCamera(integratedScene);
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog(
                "Escena integrada",
                "La escena Tests no contiene una camara principal.",
                "Aceptar"
            );
            return;
        }

        ConfigureCamera(mainCamera);
        AddSceneToBuildProfiles(OutputScenePath);

        EditorSceneManager.MarkSceneDirty(integratedScene);
        EditorSceneManager.SaveScene(integratedScene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = mainCamera.gameObject;
        EditorUtility.DisplayDialog(
            "Escena integrada creada",
            "Se creo Assets/Scenes/TelefonoMediaPipe.unity con MediaPipe, camara, ejercicio de manos y vista estereoscopica.",
            "Aceptar"
        );
    }

    private static void RemoveDuplicateRoots(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Camera" ||
                root.name == "Directional Light" ||
                root.GetComponent<EventSystem>() != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void HideMediaPipeSampleCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.enabled = false;
            }
        }
    }

    private static Camera FindMainCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera.CompareTag("MainCamera"))
                {
                    return camera;
                }
            }
        }

        return null;
    }

    private static void ConfigureCamera(Camera mainCamera)
    {
        MouseInteraction mouse = mainCamera.GetComponent<MouseInteraction>();
        if (mouse != null)
        {
            mouse.enabled = false;
        }

        if (mainCamera.GetComponent<MediaPipeHandInteraction>() == null)
        {
            mainCamera.gameObject.AddComponent<MediaPipeHandInteraction>();
        }

        if (mainCamera.GetComponent<MediaPipeStereoBackground>() == null)
        {
            mainCamera.gameObject.AddComponent<MediaPipeStereoBackground>();
        }

        if (mainCamera.GetComponent<PhoneStereoRig>() == null)
        {
            mainCamera.gameObject.AddComponent<PhoneStereoRig>();
        }
    }

    private static void AddSceneToBuildProfiles(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.All(scene => scene.path != scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
