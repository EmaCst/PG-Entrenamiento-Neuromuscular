using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// PlayerSettings uses UIOrientation (Portrait=0), not ScreenOrientation (Portrait=1).
// Apply before the Android launch activity/splash is generated, not after startup.
public sealed class PraxenStartupOrientation : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform!=BuildTarget.Android) return;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait=true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.allowedAutorotateToLandscapeLeft=false;
        PlayerSettings.allowedAutorotateToLandscapeRight=false;
    }
}
