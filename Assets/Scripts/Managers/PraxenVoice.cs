using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// One non-spatial voice channel shared by both eyes and retained across scenes.
public sealed class PraxenVoice : MonoBehaviour
{
    private static PraxenVoice instance;
    private AudioSource source;
    private int playback;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    public static PraxenVoice Instance
    {
        get
        {
            if (instance == null) new GameObject("Praxen voice").AddComponent<PraxenVoice>();
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0f; source.volume = 0.85f;
        SceneManager.activeSceneChanged += SceneChanged;
    }
    private void SceneChanged(Scene before, Scene after) => Stop();
    private void Update()
    {
        source.volume = PraxenAudioSettings.VoiceVolume;
        if(source.volume<=0 && source.isPlaying) Stop();
    }
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        if (instance == this) instance = null;
    }
    private AudioClip Clip(string key)
    {
        if (!clips.TryGetValue(key, out var clip))
        { clip = Resources.Load<AudioClip>("PraxenVoices/" + key); clips[key] = clip; }
        return clip;
    }
    public static bool IsAnySpeaking => instance != null && instance.IsSpeaking;
    public bool IsSpeaking => source != null && source.isPlaying;
    public void WarmUp(string key) => Clip(key);
    public bool TryPlay(string key)
    {
        if(IsSpeaking) return false;
        return PlayIfAvailable(key);
    }
    public bool PlayIfAvailable(string key)
    {
        if (Clip(key) == null) return false;
        Play(key); return true;
    }
    public void Stop() { playback++; source.Stop(); }
    // Timed cues never queue: late speech must not leak into the next phase.
    public void Play(string key)
    {
        var clip = Clip(key);
        playback++; source.Stop(); source.clip = clip;
        source.volume = PraxenAudioSettings.VoiceVolume;
        if (clip != null && source.volume > 0) source.Play();
    }
    public IEnumerator Say(MonoBehaviour owner, string key)
    {
        Play(key);
        int ticket = playback;
        try
        {
            while (ticket == playback && source.isPlaying && owner != null && owner.isActiveAndEnabled) yield return null;
        }
        finally { if (ticket == playback) Stop(); }
    }
    public static string ExerciseKey(string scene)
        => scene.Contains("Manos") ? "hands" : scene.Contains("Pies") ? "feet" : "turn_intro";
}

