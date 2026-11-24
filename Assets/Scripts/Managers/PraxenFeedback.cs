using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prebuilt procedural cues on their own source: never stop or queue speech/music.
public sealed class PraxenFeedback : MonoBehaviour
{
    private static PraxenFeedback instance;
    private AudioSource channel;
    private AudioClip hit,miss;
    private bool lastHit;
    private float shownUntil;
    private GUIStyle badge;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if(instance==null) new GameObject("Praxen feedback").AddComponent<PraxenFeedback>();
    }
    private void Awake()
    {
        if(instance!=null && instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);
        channel=gameObject.AddComponent<AudioSource>();channel.playOnAwake=false;
        channel.spatialBlend=0;channel.priority=80;
        hit=Tone("Praxen hit",.14f,true);miss=Tone("Praxen miss",.20f,false);
        SceneManager.activeSceneChanged+=SceneChanged;
    }
    private void SceneChanged(Scene before,Scene after){channel.Stop();shownUntil=0;}
    private static AudioClip Tone(string name,float seconds,bool success)
    {
        const int rate=22050;
        var data=new float[(int)(rate*seconds)];double phase=0;
        for(int i=0;i<data.Length;i++)
        {
            float t=(float)i/data.Length;
            double frequency=success?(t<.45f?660:990):330-150*t;
            phase+=2*Math.PI*frequency/rate;
            float envelope=Mathf.Min(1,t/.04f)*Mathf.Pow(1-t,1.6f);
            data[i]=(float)Math.Sin(phase)*envelope*.35f;
        }
        var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
    }
    public static void Play(bool success)
    {
        if(instance==null)Initialize();
        instance.lastHit=success;instance.shownUntil=Time.unscaledTime+.55f;
        float volume=PraxenAudioSettings.EffectsVolume;
        if(volume>0)instance.channel.PlayOneShot(success?instance.hit:instance.miss,volume);
    }
    private void OnGUI()
    {
        // Stereo HUDs remain responsible for their own presentation.
        if(Time.unscaledTime>=shownUntil || PraxenTrainingMode.UseGlasses || TrainingTransitionScreen.IsVisible)return;
        if(badge==null)badge=new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
        Rect safe=NatacionTrainingHud.SafeRect;
        badge.fontSize=Mathf.Clamp((int)(safe.width/25),18,32);
        var old=GUI.color;GUI.color=lastHit?new Color(.35f,1,.8f):new Color(1,.65f,.5f);
        float w=Mathf.Min(safe.width*.64f,480);
        GUI.Box(new Rect(safe.center.x-w/2,safe.yMax-150,w,62),lastHit?"¡Acierto!":"Se pasó · sigue intentando",badge);GUI.color=old;
    }
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged-=SceneChanged;
        if(instance==this)instance=null;
        if(hit!=null)Destroy(hit);if(miss!=null)Destroy(miss);
    }
}
