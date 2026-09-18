using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

public static class FootExerciseBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Object.FindFirstObjectByType<PoseLandmarkerRunner>() == null)
        {
            return;
        }

        var left = GameObject.Find("TargetLeft");
        var center = GameObject.Find("TargetCenter");
        var right = GameObject.Find("TargetRight");

        if (left == null || center == null || right == null)
        {
            Debug.LogWarning("FootExerciseBootstrap: no se encontraron los tres objetivos.");
            return;
        }

        var runtime = new GameObject("FootExerciseRuntime");
        var manager = runtime.AddComponent<FootExerciseManager>();

        var targetControllers = new[]
        {
            AddTargetController(left),
            AddTargetController(center),
            AddTargetController(right)
        };

        manager.Configure(targetControllers);

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("FootExerciseBootstrap: no se encontro la camara principal.");
            return;
        }

        var interaction = camera.gameObject.AddComponent<MediaPipeFootInteraction>();

        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if (rect.name == "Screen")
            {
                interaction.videoRect = rect;
                break;
            }
        }
    }

    private static FootTargetController AddTargetController(GameObject target)
    {
        var controller = target.GetComponent<FootTargetController>();
        return controller != null ? controller : target.AddComponent<FootTargetController>();
    }
}
