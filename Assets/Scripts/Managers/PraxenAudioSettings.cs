using UnityEngine;

// Separate persisted preferences; neither channel changes the other.
public static class PraxenAudioSettings
{
    private static bool loaded;
    private static float voice,music,effects;
    private static void Load()
    {
        if(loaded)return;loaded=true;
        voice=Mathf.Clamp01(PlayerPrefs.GetFloat("praxen.voice.volume",.85f));
        effects=Mathf.Clamp01(PlayerPrefs.GetFloat("praxen.effects.volume",.65f));
        music=Mathf.Clamp01(PlayerPrefs.GetFloat("praxen.music.volume",.32f));
    }
    public static float VoiceVolume { get {Load();return voice;} set {Load();if(Mathf.Approximately(voice,Mathf.Clamp01(value)))return;voice=Mathf.Clamp01(value);PlayerPrefs.SetFloat("praxen.voice.volume",voice);} }
    public static float MusicVolume { get {Load();return music;} set {Load();if(Mathf.Approximately(music,Mathf.Clamp01(value)))return;music=Mathf.Clamp01(value);PlayerPrefs.SetFloat("praxen.music.volume",music);} }
    public static float EffectsVolume { get {Load();return effects;} set {Load();if(Mathf.Approximately(effects,Mathf.Clamp01(value)))return;effects=Mathf.Clamp01(value);PlayerPrefs.SetFloat("praxen.effects.volume",effects);} }
    public static void Save() => PlayerPrefs.Save();
}

