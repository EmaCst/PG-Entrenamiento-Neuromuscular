#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RunningExerciseSceneBuilder
{
    [MenuItem("PG RA/Crear ejercicio de correr en escena")]
    public static void CreateInCurrentScene()
    {
        GameObject root = new GameObject("Ejercicio Correr");
        Undo.RegisterCreatedObjectUndo(root, "Crear ejercicio de correr");

        RunningAreaCalibrator calibrator = root.AddComponent<RunningAreaCalibrator>();
        RunningExerciseStats stats = root.AddComponent<RunningExerciseStats>();
        RunningExerciseManager manager = root.AddComponent<RunningExerciseManager>();
        RunningCalibrationInput input = root.AddComponent<RunningCalibrationInput>();

        GameObject canvasObject = new GameObject("RunningExerciseUI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        GameObject statusObject = new GameObject("RunningInstruction");
        statusObject.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI statusText = statusObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 30f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.text = "Mueve el telefono para detectar el suelo.";
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -24f);
        statusRect.sizeDelta = new Vector2(900f, 100f);

        GameObject boundary = new GameObject("Limite del area");
        boundary.transform.SetParent(root.transform);
        LineRenderer line = boundary.AddComponent<LineRenderer>();
        line.widthMultiplier = 0.04f;
        line.loop = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.cyan;
        line.endColor = Color.cyan;

        Transform[] markers = new Transform[4];
        for (int i = 0; i < markers.Length; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Esquina {i + 1}";
            marker.transform.SetParent(root.transform);
            marker.transform.localScale = Vector3.one * 0.15f;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            markers[i] = marker.transform;
        }

        SerializedObject calibratorSerialized = new SerializedObject(calibrator);
        calibratorSerialized.FindProperty("calibrationCamera").objectReferenceValue = Camera.main;
        calibratorSerialized.FindProperty("boundaryLine").objectReferenceValue = line;
        SerializedProperty markerProperty = calibratorSerialized.FindProperty("cornerMarkers");
        markerProperty.arraySize = 4;
        for (int i = 0; i < 4; i++) markerProperty.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
        calibratorSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("areaCalibrator").objectReferenceValue = calibrator;
        managerSerialized.FindProperty("stats").objectReferenceValue = stats;
        managerSerialized.FindProperty("trackedHead").objectReferenceValue = Camera.main != null ? Camera.main.transform : null;
        managerSerialized.FindProperty("instructionText").objectReferenceValue = statusText;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject inputSerialized = new SerializedObject(input);
        inputSerialized.FindProperty("calibrator").objectReferenceValue = calibrator;
        inputSerialized.FindProperty("statusText").objectReferenceValue = statusText;
        inputSerialized.FindProperty("exerciseManager").objectReferenceValue = manager;
        inputSerialized.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("Ejercicio de correr creado. Asigna UI opcional y marca las cuatro esquinas sobre un suelo con Collider.");
    }
}
#endif
