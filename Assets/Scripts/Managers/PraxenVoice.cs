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
    public void Stop() { playback++; source.Stop(); }
    // Timed cues never queue: late speech must not leak into the next phase.
    public void Play(string key)
    {
        var clip = Clip(key);
        playback++; source.Stop(); source.clip = clip;
        if (clip != null) source.Play();
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
        => scene.Contains("Manos") ? "hands" : scene.Contains("Pies") ? "feet" : "running";
}
