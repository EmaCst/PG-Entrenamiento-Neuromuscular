using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// One non-spatial voice channel shared by both eyes and retained across scenes.
public sealed class PraxenVoice : MonoBehaviour
{
    private static PraxenVoice instance;
    private AudioSource source;
    private AudioSource music;
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
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false; music.spatialBlend = 0f; music.loop = true; music.volume = 0f;
        music.clip = Resources.Load<AudioClip>("PraxenVoices/training_music");
        SceneManager.activeSceneChanged += SceneChanged;
        UpdateMusicScene(SceneManager.GetActiveScene());
    }
    private void SceneChanged(Scene before, Scene after)
    {
        Stop();
        UpdateMusicScene(after);
    }
    private void UpdateMusicScene(Scene scene)
    {
        bool training = scene.name == "TelefonoManos" || scene.name == "TelefonoPies" || scene.name == "TelefonoCorrer";
        if (training && music.clip != null)
        { if (!music.isPlaying) music.Play(); }
        else { music.Stop(); music.volume = 0f; }
    }
    private void Update()
    {
        if (music == null || !music.isPlaying) return;
        float target = source.isPlaying ? 0.025f : TrainingTransitionScreen.IsVisible ? 0.07f : 0.18f;
        if (CombinedExerciseSession.Instance != null && !CombinedExerciseSession.Instance.IsRunning)
            target = 0f;
        music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * (source.isPlaying ? 1.5f : 0.3f));
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
