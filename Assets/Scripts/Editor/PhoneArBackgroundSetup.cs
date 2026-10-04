#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Configure existing scenes as well as newly generated ones before building.
[InitializeOnLoad]
public class PhoneArBackgroundSetup : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    static PhoneArBackgroundSetup()
    {
        EditorApplication.delayCall += Configure;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        Configure();
    }

    [MenuItem("Tools/Phone Training/Repair AR Camera Background")]
    public static void Configure()
    {
        Type featureType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            featureType = assembly.GetType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature");
            if (featureType != null) break;
        }
        if (featureType == null || !typeof(ScriptableRendererFeature).IsAssignableFrom(featureType))
        {
            throw new BuildFailedException("ARBackgroundRendererFeature no esta disponible. Comprueba AR Foundation y URP antes de compilar.");
        }

        foreach (string path in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (renderer == null)
                throw new BuildFailedException("No se encontro el renderer: " + path);

            ScriptableRendererFeature background = null;
            foreach (var feature in renderer.rendererFeatures)
                if (feature != null && featureType.IsInstanceOfType(feature)) background = feature;

            if (background == null)
            {
                background = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
                background.name = "AR Camera Background";
                AssetDatabase.AddObjectToAsset(background, renderer);
                renderer.rendererFeatures.Add(background);
            }
            background.SetActive(true);
            EditorUtility.SetDirty(background);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
        }
        ConfigureAndroidLoader();
        AssetDatabase.SaveAssets();
    }
    private static void ConfigureAndroidLoader()
    {
        XRGeneralSettingsPerBuildTarget settings;
        if (!EditorBuildSettings.TryGetConfigObject("com.unity.xr.management.loader_settings", out settings) || settings == null)
        {
            var existing = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (existing.Length > 0)
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(existing[0]));
            else
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            }
            EditorBuildSettings.AddConfigObject("com.unity.xr.management.loader_settings", settings, true);
        }
        if (!settings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var android = settings.SettingsForBuildTarget(BuildTargetGroup.Android);
        android.InitManagerOnStart = true;
        android.Manager.automaticLoading = true;
        android.Manager.automaticRunning = true;
        bool hasARCore = false;
        foreach (var loader in android.Manager.activeLoaders)
            if (loader != null && loader.GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader") hasARCore = true;
        if (!hasARCore && !XRPackageMetadataStore.AssignLoader(android.Manager,
            "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android))
            throw new BuildFailedException("No se pudo activar ARCore para Android. Revisa XR Plug-in Management.");
        EditorUtility.SetDirty(android.Manager);
        EditorUtility.SetDirty(android);
        EditorUtility.SetDirty(settings);
    }

}
#endif
