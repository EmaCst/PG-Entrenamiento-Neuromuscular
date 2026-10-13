using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

// Prevent a camera-only APK from silently shipping without AR plane tracking.
public sealed class ValidatePraxenAndroidXR : IPreprocessBuildWithReport
{
    public int callbackOrder => -10000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var targets) || targets == null)
        {
            targets = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            if (targets != null) EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targets, true);
        }
        var android = targets != null ? targets.SettingsForBuildTarget(BuildTargetGroup.Android) : null;
        bool hasARCore = false;
        if (android != null && android.Manager != null)
            foreach (var loader in android.Manager.activeLoaders)
                if (loader != null && loader.GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader") hasARCore = true;
        if (android == null || !android.InitManagerOnStart || !hasARCore)
            throw new BuildFailedException("Praxen requiere ARCore e Initialize XR on Startup en XR Plug-in Management > Android. No se generará una APK sin detección de suelo.");
        Debug.Log("Praxen: proveedor ARCore Android e inicio XR verificados.");
    }
}
