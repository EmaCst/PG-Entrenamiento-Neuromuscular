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

        Type featureType = FindType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature");
        if (featureType == null)
        {
            Debug.LogError("AR Foundation no contiene ARBackgroundRendererFeature. Revisa que el paquete XR AR Foundation esté instalado.");
            return;
        }

        foreach (object feature in features)
        {
            if (feature != null && featureType.IsInstanceOfType(feature)) return;
        }

        ScriptableObject featureAsset = ScriptableObject.CreateInstance(featureType);
        featureAsset.name = "ARBackgroundRendererFeature";
        MethodInfo setActive = featureType.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public);
        setActive?.Invoke(featureAsset, new object[] { true });
        AssetDatabase.AddObjectToAsset(featureAsset, rendererData);
        features.Add(featureAsset);
        EditorUtility.SetDirty(rendererData);
        EditorUtility.SetDirty(featureAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("ARBackgroundRendererFeature agregado a Mobile_Renderer para que AR Foundation renderice la cámara en Android.", rendererData);
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
