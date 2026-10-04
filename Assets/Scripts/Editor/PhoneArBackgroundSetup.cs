#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
        AssetDatabase.SaveAssets();
    }
}
#endif
