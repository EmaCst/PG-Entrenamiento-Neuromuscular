#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConfigureMobileARBackground
{
    private const string RendererDataPath = "Assets/Settings/Mobile_Renderer.asset";

    static ConfigureMobileARBackground()
    {
        EditorApplication.delayCall += EnsureFeature;
    }

    [MenuItem("PG RA/Configurar fondo de cámara AR en Android")]
    private static void EnsureFeature()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        ScriptableObject rendererData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererDataPath);
        if (rendererData == null)
        {
            Debug.LogError($"No se encontró {RendererDataPath}.");
            return;
        }

        PropertyInfo featuresProperty = rendererData.GetType().GetProperty(
            "rendererFeatures", BindingFlags.Instance | BindingFlags.Public
        );
        IList features = featuresProperty?.GetValue(rendererData) as IList;
        if (features == null)
        {
            Debug.LogError("No se pudo acceder a las funciones del renderer móvil.", rendererData);
            return;
        }

        bool changed = false;
        changed |= EnsureFeature(rendererData, features,
            "UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature");
        // ARCore uses command buffers for its camera background when Vulkan is
        // enabled. Keep this feature present even if Android later falls back to GLES.
        changed |= EnsureFeature(rendererData, features,
            "UnityEngine.XR.ARFoundation.ARCommandBufferSupportRendererFeature");
        if (!changed) return;

        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        Debug.Log("Funciones ARBackgroundRendererFeature y ARCommandBufferSupportRendererFeature agregadas a Mobile_Renderer.", rendererData);
    }

    private static bool EnsureFeature(ScriptableObject rendererData, IList features, string fullTypeName)
    {
        Type featureType = FindType(fullTypeName);
        if (featureType == null)
        {
            Debug.LogError($"AR Foundation no contiene {fullTypeName}. Revisa el paquete XR AR Foundation.", rendererData);
            return false;
        }

        foreach (object feature in features)
        {
            if (feature != null && featureType.IsInstanceOfType(feature)) return false;
        }

        ScriptableObject featureAsset = ScriptableObject.CreateInstance(featureType);
        featureAsset.name = featureType.Name;
        MethodInfo setActive = featureType.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public);
        setActive?.Invoke(featureAsset, new object[] { true });
        AssetDatabase.AddObjectToAsset(featureAsset, rendererData);
        features.Add(featureAsset);
        EditorUtility.SetDirty(featureAsset);
        return true;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);
            if (type != null) return type;
        }
        return null;
    }
}
#endif
