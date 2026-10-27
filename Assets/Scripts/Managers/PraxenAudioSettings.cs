using UnityEngine;

// Separate persisted preferences; neither channel changes the other.
public static class PraxenAudioSettings
{
    private static bool loaded;
    private static float voice,music;
    private static void Load()
    {
        if(loaded)return;loaded=true;
        voice=Mathf.Clamp01(PlayerPrefs.GetFloat("praxen.voice.volume",.85f));
        music=Mathf.Clamp01(PlayerPrefs.GetFloat("praxen.music.volume",.32f));
    }
    public static float VoiceVolume { get {Load();return voice;} set {Load();if(Mathf.Approximately(voice,Mathf.Clamp01(value)))return;voice=Mathf.Clamp01(value);PlayerPrefs.SetFloat("praxen.voice.volume",voice);} }
    public static float MusicVolume { get {Load();return music;} set {Load();if(Mathf.Approximately(music,Mathf.Clamp01(value)))return;music=Mathf.Clamp01(value);PlayerPrefs.SetFloat("praxen.music.volume",music);} }
    public static void Save() => PlayerPrefs.Save();
}
