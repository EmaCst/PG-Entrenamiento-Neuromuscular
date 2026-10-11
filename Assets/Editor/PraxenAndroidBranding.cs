using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Applies Praxen's adaptive, round, and legacy launcher icons to Android builds.</summary>
public sealed class PraxenAndroidBranding : IPreprocessBuildWithReport
{
    private const string IconPath = "Assets/Branding/PraxenIcon.png";
    private const string ForegroundPath = "Assets/Branding/PraxenAdaptiveForeground.png";
    private const string BackgroundPath = "Assets/Branding/PraxenAdaptiveBackground.png";

    public int callbackOrder { get { return 0; } }

    [MenuItem("Tools/Praxen/Apply Android Branding")]
    private static void ApplyFromMenu()
    {
        ApplyAndroidIcons();
        AssetDatabase.SaveAssets();
        Debug.Log("Praxen Android launcher icons configured.");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            ApplyAndroidIcons();
        }
    }

    private static void ApplyAndroidIcons()
    {
        Texture2D legacy = LoadTexture(IconPath);
        Texture2D foreground = LoadTexture(ForegroundPath);
        Texture2D background = LoadTexture(BackgroundPath);

        SetIconKind(AndroidPlatformIconKind.Adaptive, GetAdaptiveLayers(foreground, background));
        SetIconKind(AndroidPlatformIconKind.Round, legacy);
        SetIconKind(AndroidPlatformIconKind.Legacy, legacy);
    }


    // Unity's platform module defines the layer order. Use its labels instead of
    // assuming that a foreground/background array has the same order in every version.
    private static Texture2D[] GetAdaptiveLayers(Texture2D foreground, Texture2D background)
    {
        PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
        PropertyInfo labelsProperty = typeof(PlatformIconKind).GetProperty(
            "customLayerLabels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        string[] labels = labelsProperty == null ? null : labelsProperty.GetValue(kind, null) as string[];
        if (labels == null || labels.Length != 2)
        {
            throw new BuildFailedException("Cannot identify Unity's adaptive icon layers. Check Android Player Settings > Icon.");
        }

        Texture2D[] layers = new Texture2D[2];
        bool foundForeground = false;
        bool foundBackground = false;
        for (int i = 0; i < labels.Length; i++)
        {
            if (string.Equals(labels[i], "Foreground", StringComparison.OrdinalIgnoreCase))
            {
                layers[i] = foreground;
                foundForeground = true;
            }
            else if (string.Equals(labels[i], "Background", StringComparison.OrdinalIgnoreCase))
            {
                layers[i] = background;
                foundBackground = true;
            }
        }
        if (!foundForeground || !foundBackground)
        {
            throw new BuildFailedException("Unrecognized adaptive icon layer labels: " + string.Join(", ", labels));
        }
        Debug.Log("Praxen adaptive icon layers: " + string.Join(", ", labels));
        return layers;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            throw new BuildFailedException("Praxen icon texture is missing or failed to import: " + path);
        }
        return texture;
    }

    private static void SetIconKind(PlatformIconKind kind, params Texture2D[] layers)
    {
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        if (icons == null || icons.Length == 0)
        {
            throw new BuildFailedException("Unity did not provide Android icon slots for: " + kind);
        }

        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i].maxLayerCount != layers.Length)
            {
                throw new BuildFailedException(
                    "Unexpected layer count for Android icon " + kind + ": " + icons[i].maxLayerCount);
            }
            icons[i].SetTextures(layers);
        }

        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
    }
}
