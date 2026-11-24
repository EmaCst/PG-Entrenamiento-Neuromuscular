using UnityEngine;
using UnityEngine.SceneManagement;

public static class PraxenTrainingMode
{
    public static bool UseGlasses
    {
        get => PlayerPrefs.GetInt("praxen.training.glasses",0)==1;
        set {PlayerPrefs.SetInt("praxen.training.glasses",value?1:0);PlayerPrefs.Save();}
    }
    public static string Label => UseGlasses?"Gafas · Cámara trasera":"Cámara frontal · Sin gafas";
    public static string Resolve(string scene) => PraxenSceneRoutes.Resolve(scene,UseGlasses);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.activeSceneChanged-=SceneChanged;
        SceneManager.activeSceneChanged+=SceneChanged;
    }
    private static void SceneChanged(Scene before,Scene after) => ConfigureScene(after.name);
    public static void ConfigureScene(string scene)
    {
        bool glasses=PraxenSceneRoutes.IsGlasses(scene);
        Mediapipe.Unity.WebCamSource.PreferRearCamera=glasses;
        if(!glasses) {NatacionPortrait.Configure();return;}
        var webcam=Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource as Mediapipe.Unity.WebCamSource;
        if(webcam!=null)webcam.RequireFrontFacing=false;
        Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
        Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=false;
        Screen.orientation=ScreenOrientation.LandscapeLeft;
    }
}

