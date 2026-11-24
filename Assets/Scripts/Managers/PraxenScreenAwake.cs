using UnityEngine;
using UnityEngine.SceneManagement;

// Keep the display awake only while training, including setup, countdown and rest.
// No wake lock/permission: leaving the app restores the system's normal policy.
public sealed class PraxenScreenAwake : MonoBehaviour
{
    private static PraxenScreenAwake instance;
    private bool paused;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if(instance==null)new GameObject("Praxen screen policy").AddComponent<PraxenScreenAwake>();
    }
    private void Awake()
    {
        if(instance!=null && instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged+=SceneChanged;Apply();
    }
    private void SceneChanged(Scene before,Scene after)=>Apply();
    private void OnApplicationPause(bool value){paused=value;Apply();}
    private void OnApplicationFocus(bool focused){if(focused && !paused)Apply();}
    private void Apply()
    {
        string scene=SceneManager.GetActiveScene().name;
        bool training=PraxenSceneRoutes.KeepsDisplayAwake(scene);
        bool keep=training && !paused;
        Screen.sleepTimeout=keep?SleepTimeout.NeverSleep:SleepTimeout.SystemSetting;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                activity.Call("runOnUiThread",new AndroidJavaRunnable(()=>SetAndroidWindow(keep)));
        }
        catch(System.Exception e){Debug.LogWarning("No se pudo reforzar el estado de pantalla: "+e.Message);}
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    private static void SetAndroidWindow(bool keep)
    {
        try
        {
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var window=activity.Call<AndroidJavaObject>("getWindow"))
                window.Call(keep?"addFlags":"clearFlags",128); // FLAG_KEEP_SCREEN_ON
        }
        catch(System.Exception e){Debug.LogWarning("No se pudo aplicar KEEP_SCREEN_ON: "+e.Message);}
    }
#endif
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged-=SceneChanged;
        if(instance==this){instance=null;Screen.sleepTimeout=SleepTimeout.SystemSetting;}
    }
}
