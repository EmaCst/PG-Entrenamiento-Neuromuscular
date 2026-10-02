#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PhoneExerciseSuiteBuilder
{
    private const string ScenesFolder = "Assets/Scenes";

    [MenuItem("PG RA/Crear todas las escenas para telefono")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        IntegratedPhoneSceneBuilder.BuildIntegratedScene();
        ConfigureHandScene();
        BuildFeetScene();
        BuildRunningScene();
        BuildSimpleScene("MenuTelefono", typeof(PhoneExerciseMenu));
        BuildSimpleScene("SesionCombinada", typeof(CombinedExerciseSession));
        ConfigureBuildScenes();

        EditorSceneManager.OpenScene($"{ScenesFolder}/MenuTelefono.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Escenas para telefono",
            "Se crearon MenuTelefono, TelefonoManos, TelefonoPies, TelefonoCorrer y SesionCombinada.",
            "Aceptar"
        );
    }

    private static void ConfigureHandScene()
    {
        string path = $"{ScenesFolder}/TelefonoManos.unity";
        if (!System.IO.File.Exists(path)) return;
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        AimLabManager manager = UnityEngine.Object.FindFirstObjectByType<AimLabManager>();
        if (manager != null) AddStandaloneStarter(manager.gameObject, manager);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildFeetScene()
    {
        Scene scene = CreateEmptyScene("TelefonoPies");
        Camera camera = CreateStereoCamera(new Vector3(0f, 1f, -10f));
        FootCameraSource cameraSource = camera.gameObject.AddComponent<FootCameraSource>();
        FootDetectorSentis detector = camera.gameObject.AddComponent<FootDetectorSentis>();
        FootStereoBackground background = camera.gameObject.AddComponent<FootStereoBackground>();

        SetObject(detector, "cameraSource", cameraSource);
        SetObject(background, "cameraSource", cameraSource);
        AssignFootModel(detector);

        GameObject exercise = new GameObject("FootExercise");
        FootExerciseStats stats = exercise.AddComponent<FootExerciseStats>();
        FootExerciseManager manager = exercise.AddComponent<FootExerciseManager>();

        FootTargetZone[] zones = new FootTargetZone[3];
        for (int i = 0; i < zones.Length; i++)
        {
            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = $"FootTarget_{i + 1}";
            zone.transform.position = new Vector3((i - 1) * 2.2f, -1.8f, 0f);
            zone.transform.localScale = new Vector3(1.6f, 0.65f, 0.2f);
            zones[i] = zone.AddComponent<FootTargetZone>();
        }

        CreateExerciseTexts(out TMP_Text score, out TMP_Text instruction);
        SetObject(manager, "detector", detector);
        SetObjectArray(manager, "targets", zones.Cast<UnityEngine.Object>().ToArray());
        SetObject(manager, "stats", stats);
        SetObject(manager, "projectionCamera", camera);
        SetObject(manager, "scoreText", score);
        SetObject(manager, "instructionText", instruction);
        AddStandaloneStarter(exercise, manager);
        CreateLight();
        SaveScene(scene, "TelefonoPies");
    }

    private static void BuildRunningScene()
    {
        Scene scene = CreateEmptyScene("TelefonoCorrer");
        Camera camera = CreateArStereoCamera();

        GameObject exercise = new GameObject("RunningExercise");
        RunningAreaCalibrator calibrator = exercise.AddComponent<RunningAreaCalibrator>();
        RunningExerciseStats stats = exercise.AddComponent<RunningExerciseStats>();
        RunningExerciseManager manager = exercise.AddComponent<RunningExerciseManager>();
        RunningExerciseDemoSetup demoSetup = exercise.AddComponent<RunningExerciseDemoSetup>();
        RunningCalibrationInput calibrationInput = exercise.AddComponent<RunningCalibrationInput>();

        CreateExerciseTexts(out TMP_Text score, out TMP_Text instruction);
        SetObject(manager, "areaCalibrator", calibrator);
        SetObject(manager, "stats", stats);
        SetObject(manager, "trackedHead", camera.transform);
        SetObject(manager, "scoreText", score);
        SetObject(manager, "instructionText", instruction);
        SetObject(demoSetup, "calibrator", calibrator);
        SetObject(demoSetup, "trackedHead", camera.transform);
        SetObject(calibrationInput, "calibrator", calibrator);
        SetObject(calibrationInput, "statusText", instruction);
        SetObject(calibrationInput, "exerciseManager", manager);

        GameObject origin = GameObject.Find("XR Origin");
        if (origin != null)
        {
            AddComponentIfAvailable(origin, "UnityEngine.XR.ARFoundation.ARPlaneManager");
            AddComponentIfAvailable(origin, "UnityEngine.XR.ARFoundation.ARRaycastManager");
        }

        CreateLight();
        SaveScene(scene, "TelefonoCorrer");
    }

    private static void AddComponentIfAvailable(GameObject target, string typeName)
    {
        Type componentType = Type.GetType($"{typeName}, Unity.XR.ARFoundation");
        if (componentType != null && target.GetComponent(componentType) == null)
        {
            target.AddComponent(componentType);
        }
    }

    private static Camera CreateArStereoCamera()
    {
        Type arSessionType = Type.GetType("UnityEngine.XR.ARFoundation.ARSession, Unity.XR.ARFoundation");
        Type arInputType = Type.GetType("UnityEngine.XR.ARFoundation.ARInputManager, Unity.XR.ARFoundation");
        Type arCameraManagerType = Type.GetType("UnityEngine.XR.ARFoundation.ARCameraManager, Unity.XR.ARFoundation");
        Type arBackgroundType = Type.GetType("UnityEngine.XR.ARFoundation.ARCameraBackground, Unity.XR.ARFoundation");
        Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");

        if (arSessionType != null)
        {
            GameObject session = new GameObject("AR Session");
            session.AddComponent(arSessionType);
            if (arInputType != null) session.AddComponent(arInputType);
        }

        GameObject origin = new GameObject("XR Origin");
        Component xrOrigin = xrOriginType != null ? origin.AddComponent(xrOriginType) : null;
        Camera camera = CreateStereoCamera(new Vector3(0f, 1.6f, 0f));
        camera.transform.SetParent(origin.transform, true);
        if (arCameraManagerType != null) camera.gameObject.AddComponent(arCameraManagerType);
        if (arBackgroundType != null) camera.gameObject.AddComponent(arBackgroundType);
        TrackedPoseDriver trackedPoseDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
        trackedPoseDriver.positionInput = new InputActionProperty(
            new InputAction("Posicion XR", InputActionType.Value, "<XRHMD>/centerEyePosition")
        );
        trackedPoseDriver.rotationInput = new InputActionProperty(
            new InputAction("Rotacion XR", InputActionType.Value, "<XRHMD>/centerEyeRotation")
        );
        if (xrOrigin != null) SetObject(xrOrigin, "m_Camera", camera);
        return camera;
    }

    private static void AssignFootModel(FootDetectorSentis detector)
    {
        string[] guids = AssetDatabase.FindAssets("foot_detector_best t:ModelAsset");
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:ModelAsset");
        if (guids.Length == 0)
        {
            Debug.LogError("No se encontro el modelo ONNX de pies.");
            return;
        }

        UnityEngine.Object model = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
        SetObject(detector, "modelAsset", model);
    }

    private static Scene CreateEmptyScene(string name)
    {
        string path = $"{ScenesFolder}/{name}.unity";
        if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static Camera CreateStereoCamera(Vector3 position)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<PhoneStereoRig>();
        return camera;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateExerciseTexts(out TMP_Text score, out TMP_Text instruction)
    {
        GameObject canvasObject = new GameObject("ExerciseUI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        score = CreateText(canvas.transform, "Score", new Vector2(-260f, 190f));
        instruction = CreateText(canvas.transform, "Instruction", new Vector2(220f, 190f));
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 position)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = name;
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(430f, 70f);
        return text;
    }

    private static void AddStandaloneStarter(GameObject owner, MonoBehaviour controller)
    {
        StandaloneExerciseStarter starter = owner.GetComponent<StandaloneExerciseStarter>();
        if (starter == null) starter = owner.AddComponent<StandaloneExerciseStarter>();
        SetObject(starter, "exerciseController", controller);
    }

    private static void BuildSimpleScene(string name, Type componentType)
    {
        Scene scene = CreateEmptyScene(name);
        GameObject root = new GameObject(name);
        root.AddComponent(componentType);
        SaveScene(scene, name);
    }

    private static void SaveScene(Scene scene, string name)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{name}.unity");
    }

    private static void ConfigureBuildScenes()
    {
        string[] ordered =
        {
            "MenuTelefono", "TelefonoManos", "TelefonoPies",
            "TelefonoCorrer", "SesionCombinada"
        };
        EditorBuildSettings.scenes = ordered
            .Select(name => new EditorBuildSettingsScene($"{ScenesFolder}/{name}.unity", true))
            .ToArray();

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null)
        {
            Debug.LogError($"No se encontro {property} en {target.GetType().Name}.");
            return;
        }
        field.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray(UnityEngine.Object target, string property, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null || !field.isArray) return;
        field.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
