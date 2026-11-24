using UnityEngine;
using UnityEngine.SceneManagement;

// Independent music component and Resources folder. Voices keep their own channel.
public sealed class PraxenMusic : MonoBehaviour
{
    private static PraxenMusic instance;
    private AudioSource source;
    private bool trainingMode;
    private float previewUntil;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { if(instance==null)new GameObject("Praxen music").AddComponent<PraxenMusic>(); }
    private void Awake()
    {
        if(instance!=null && instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=0;
        source.clip=Resources.Load<AudioClip>("PraxenMusic/training_music");
        SceneManager.activeSceneChanged+=SceneChanged;ApplyScene(SceneManager.GetActiveScene());
    }
    private void SceneChanged(Scene before,Scene after) => ApplyScene(after);
    private void ApplyScene(Scene scene)
    {
        previewUntil=0;
        trainingMode=PraxenSceneRoutes.IsExercise(scene.name);
        if(trainingMode && source.clip!=null){if(!source.isPlaying)source.Play();}
        else {source.Stop();source.volume=0;}
    }
    public static void Preview()
    {
        Bootstrap();instance.previewUntil=Time.unscaledTime+10;
        if(instance.source.clip!=null && !instance.source.isPlaying)instance.source.Play();
    }
    public static void StopPreview()
    {
        if(instance==null)return;instance.previewUntil=0;
        if(!instance.trainingMode){instance.source.Stop();instance.source.volume=0;}
    }
    private void Update()
    {
        if(source==null || !source.isPlaying)return;
        if(!trainingMode && Time.unscaledTime>=previewUntil){source.Stop();source.volume=0;return;}
        float factor=PraxenVoice.IsAnySpeaking ? .14f : TrainingTransitionScreen.IsVisible ? .4f : 1;
        float target=PraxenAudioSettings.MusicVolume*factor;
        if(CombinedExerciseSession.Instance!=null && !CombinedExerciseSession.Instance.IsRunning)target=0;
        if(PraxenVoice.IsAnySpeaking)source.volume=Mathf.Min(source.volume,target);
        else source.volume=Mathf.MoveTowards(source.volume,target,Time.unscaledDeltaTime*(target<source.volume?2:.4f));
    }
    private void OnApplicationPause(bool paused)
    {
        PraxenAudioSettings.Save();
        if(paused)source.Pause();else if(source.clip!=null)ApplyScene(SceneManager.GetActiveScene());
    }
    private void OnApplicationQuit() => PraxenAudioSettings.Save();
    private void OnDestroy()
    { SceneManager.activeSceneChanged-=SceneChanged;if(instance==this)instance=null; }
}

