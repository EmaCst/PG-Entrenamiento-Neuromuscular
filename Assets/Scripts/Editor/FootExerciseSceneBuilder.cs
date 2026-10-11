#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FootExerciseSceneBuilder
{
    [MenuItem("PG RA/Crear ejercicio de pies en escena")]
    public static void CreateInCurrentScene()
    {
        GameObject root = new GameObject("Ejercicio Pies");
        Undo.RegisterCreatedObjectUndo(root, "Crear ejercicio de pies");

        FootCameraSource cameraSource = root.AddComponent<FootCameraSource>();
        FootDetectorSentis detector = root.AddComponent<FootDetectorSentis>();
        FootExerciseStats stats = root.AddComponent<FootExerciseStats>();
        FootExerciseManager manager = root.AddComponent<FootExerciseManager>();

        FootTargetZone[] zones = new FootTargetZone[3];
        for (int i = 0; i < zones.Length; i++)
        {
            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = $"Objetivo pie {i + 1}";
            zone.transform.SetParent(root.transform);
            zone.transform.localPosition = new Vector3((i - 1) * 0.6f, -1.2f, 2f);
            zone.transform.localScale = new Vector3(0.45f, 0.025f, 0.65f);
            zones[i] = zone.AddComponent<FootTargetZone>();
            Object.DestroyImmediate(zone.GetComponent<Collider>());
        }

        SerializedObject detectorData = new SerializedObject(detector);
        detectorData.FindProperty("cameraSource").objectReferenceValue = cameraSource;
        ModelAssetReference(detectorData);
        detectorData.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("detector").objectReferenceValue = detector;
        managerData.FindProperty("stats").objectReferenceValue = stats;
        managerData.FindProperty("projectionCamera").objectReferenceValue = Camera.main;
        SerializedProperty targets = managerData.FindProperty("targets");
        targets.arraySize = zones.Length;
        for (int i = 0; i < zones.Length; i++) targets.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
        managerData.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("Ejercicio de pies creado con detector YOLO11 y tres objetivos.");
    }

    private static void ModelAssetReference(SerializedObject detectorData)
    {
        Object model = AssetDatabase.LoadAssetAtPath<Object>("Assets/Models/foot_detector_best.onnx");
        if (model != null) detectorData.FindProperty("modelAsset").objectReferenceValue = model;
        else Debug.LogWarning("Importa Sentis y espera a que Unity procese foot_detector_best.onnx.");
    }
}
#endif
