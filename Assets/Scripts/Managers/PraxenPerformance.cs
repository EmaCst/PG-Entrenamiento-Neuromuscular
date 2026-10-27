using UnityEngine;
using UnityEngine.SceneManagement;

// Render budget is independent of Pose (15 Hz) / feet inference (10 Hz).
public static class PraxenPerformance
{
    public const int TargetFps=30;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if(Application.isMobilePlatform)
        {
            string[] levels=QualitySettings.names;
            for(int i=0;i<levels.Length;i++)if(levels[i]=="Mobile"){QualitySettings.SetQualityLevel(i,true);break;}
        }
        Apply();SceneManager.activeSceneChanged-=SceneChanged;SceneManager.activeSceneChanged+=SceneChanged;
    }
    private static void SceneChanged(Scene before,Scene after) => Apply();
    private static void Apply()
    {
        QualitySettings.vSyncCount=0;Application.targetFrameRate=TargetFps;
        if(Application.isMobilePlatform)
        {QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.antiAliasing=0;QualitySettings.realtimeReflectionProbes=false;}
    }
}
