#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class RunningExerciseSceneBuilder
{
    [MenuItem("PG RA/Crear ejercicio de giros en escena")]
    public static void CreateInCurrentScene()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera"; camera = cameraObject.GetComponent<Camera>();
        }
        var rig = camera.GetComponent<PhoneStereoRig>();
        if (rig == null) rig = camera.gameObject.AddComponent<PhoneStereoRig>();
        var source = camera.GetComponent<FootCameraSource>();
        if (source == null) source = camera.gameObject.AddComponent<FootCameraSource>();
        var background = camera.GetComponent<FootStereoBackground>();
        if (background == null) background = camera.gameObject.AddComponent<FootStereoBackground>();
        var gyro = camera.GetComponent<RunningGyroSource>();
        if (gyro == null) gyro = camera.gameObject.AddComponent<RunningGyroSource>();
        Set(background, "cameraSource", source);
        var root = new GameObject("RunningExercise"); Undo.RegisterCreatedObjectUndo(root, "Crear ejercicio de giros");
        var stats = root.AddComponent<RunningExerciseStats>(); var manager = root.AddComponent<RunningExerciseManager>();
        var starter = root.AddComponent<StandaloneExerciseStarter>(); Set(starter,"exerciseController",manager);
        var canvasObject = new GameObject("RunningExerciseUI", typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var statusObject = new GameObject("RunningInstruction", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(canvasObject.transform, false);
        var label = statusObject.GetComponent<TextMeshProUGUI>(); label.text = "Preparando cámara y giroscopio…";
        label.font = TMP_Settings.defaultFontAsset; label.fontSize = 28;
        Set(manager,"gyroSource",gyro); Set(manager,"cameraSource",source); Set(manager,"stats",stats); Set(manager,"instructionText",label);
        Selection.activeGameObject = root;
    }
    private static void Set(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
